using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Network
{
	public class SocketListener
	{
		/// <summary>
		/// Used for configuring the listening socket.
		/// </summary>
		private SocketListenerSettings _settings;

		/// <summary>
		/// Pool of SocketAsyncEventArgs used for accepting connections.
		/// </summary>
		private SocketAsyncEventArgsPool _acceptPool;

		/// <summary>
		/// Pool of SocketAsyncEventArgs used for sending/receiving.
		/// </summary>
		private SocketAsyncEventArgsPool _sendRecvPool;

		private SocketAsyncEventArgsPool _sendPool;

		/// <summary>
		/// Prevents the socket from accepting more than the maximum set connections.
		/// </summary>
		private Semaphore _maxConnectionsControl;

		/// <summary>
		/// Parent socket used for listening to incoming requests.
		/// </summary>
		private Socket _listener;

		/// <summary>
		/// Implementation for handling received data.
		/// </summary>
		private IReceiveHandler _recvHandler;

		/// <summary>
		/// Used to track the currently connected clients.
		/// </summary>
		private int _currentConnections = 0;

		/// <summary>
		/// Flag used to tell the server to shutdown all connections.
		/// </summary>
		private bool _shutdown = false;

		/// <summary>
		/// Constructs a SocketListener.
		/// </summary>
		/// <param name="settings">Object used to set server related properties.</param>
		public SocketListener(SocketListenerSettings settings, IReceiveHandler recvHandler)
		{
			this._settings = settings;
			this._maxConnectionsControl = new Semaphore(settings.MaxConnections, settings.MaxConnections);
			this._acceptPool = new SocketAsyncEventArgsPool(settings.MaxSimultaneousAcceptOps);
			this._sendRecvPool = new SocketAsyncEventArgsPool(settings.MaxConnections);
			this._recvHandler = recvHandler;

			// Pre-allocate resources for accept SAEA.
			for (int i = 0; i < settings.MaxSimultaneousAcceptOps; i++)
			{
				this._acceptPool.Push(this.NewAcceptSAEA(this._acceptPool));
			}

			// Pre-allocate resources for send/receive SAEA.
			for (int i = 0; i < settings.MaxConnections; i++)
			{
				var sendRecvArgs = new SocketAsyncEventArgs();
				var sendArgs = new SocketAsyncEventArgs();

				// Necessary event for processing immediately after connection has completed.
				sendRecvArgs.Completed += ((sender, saea) =>
				{
					switch (saea.LastOperation)
					{
						case SocketAsyncOperation.Receive:
							this.ProcessReceive(saea);
							break;
						case SocketAsyncOperation.Send:
							this.ProcessSend(saea);
							break;
						default:
							throw new ArgumentException("Error. The last operation was not a 'send' or 'receive'.");
					}
				});

				// Completed event for sending.
				sendArgs.Completed += ((sender, saea) =>
				{
					if ((saea.LastOperation == SocketAsyncOperation.Send) && 
					(saea.SocketError != SocketError.Success))
					{
						this.CloseClientSocket(saea);
					}
				});

				sendRecvArgs.SetBuffer(new byte[settings.BufferSize], 0, settings.BufferSize);

				this._sendRecvPool.Push(sendRecvArgs);
				this._sendPool.Push(sendArgs);
			}
		}

		/// <summary>
		/// Instructs the server to open a socket and start.
		/// </summary>
		public void Start()
		{
			this._listener = new Socket(this._settings.Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			this._listener.Bind(this._settings.Endpoint);
			this._listener.Listen(this._settings.Backlog);

			this.Accept();
		}

		/// <summary>
		/// Gets a SocketAsyncEventArgs instance and uses it to accept connections.
		/// </summary>
		public void Accept()
		{
			// Get an available SocketAsyncEventArgs from the pool, or create one if needed.
			var acceptArgs = this._acceptPool.Pop();
			if (acceptArgs == null)
				acceptArgs = this.NewAcceptSAEA(this._acceptPool);

			// If the maximum allowed connections has not been reached, accept one connection.
			// If the operation completes synchronously, then move to processing; otherwise,
			// the event attached in NewAcceptSAEA will handle it later.
			this._maxConnectionsControl.WaitOne();

			try
			{
				if (!this._listener.AcceptAsync(acceptArgs))
					this.ProcessAccept(acceptArgs);
			}
			catch (ObjectDisposedException e)
			{
				if (this._shutdown)
					acceptArgs.Dispose();
				else
					throw;
			}
		}

		/// <summary>
		/// Creates a new SocketAsyncEventArgs instance for accept operations.
		/// </summary>
		/// <param name="pool"></param>
		/// <returns>SAEA for accept.</returns>
		/// <remarks>Creating this is possible since Accept operations do not use a buffer.</remarks>
		private SocketAsyncEventArgs NewAcceptSAEA(SocketAsyncEventArgsPool pool)
		{
			var acceptArgs = new SocketAsyncEventArgs();

			// Necessary event for processing immediately after connection has completed.
			acceptArgs.Completed += ((sender, saea) =>
			{
				this.ProcessAccept(saea);
			});

			return acceptArgs;
		}

		/// <summary>
		/// Processes a SAEA once the 'accept' operation has completed.
		/// </summary>
		/// <param name="acceptArgs">SAEA instance in the 'accept' completed state.</param>
		private void ProcessAccept(SocketAsyncEventArgs acceptArgs)
		{
			this.Accept();

			// Dispose the socket, queue the SAEA for reuse, start another accept call.
			if (acceptArgs.SocketError != SocketError.Success)
			{
				acceptArgs.AcceptSocket.Close();
				acceptArgs.AcceptSocket = null;
				this._acceptPool.Push(acceptArgs);
				return;
			}

			// Transfer the new socket to the sending/receiving SAEA.
			this._currentConnections++;
			var sendRecvArgs = this._sendRecvPool.Pop();
			var sendArgs = this._sendPool.Pop();

			sendRecvArgs.AcceptSocket = acceptArgs.AcceptSocket;
			sendArgs.AcceptSocket = acceptArgs.AcceptSocket;
			acceptArgs.AcceptSocket = null;
			this._acceptPool.Push(acceptArgs);

			sendRecvArgs.UserToken = new State();
			
			this.Receive(sendRecvArgs);
		}

		/// <summary>
		/// Wraps the ReceiveAsync method to handle sync/async return values.
		/// </summary>
		/// <param name="sendRecvArgs">SAEA object used for receiving data.</param>
		private void Receive(SocketAsyncEventArgs sendRecvArgs)
		{
			if (this._shutdown)
				sendRecvArgs.AcceptSocket.Shutdown(SocketShutdown.Send);

			// prepare the buffer
			var buffer = new byte[this._settings.BufferSize];
			sendRecvArgs.SetBuffer(buffer, 0, buffer.Length);

			var a = ((State)sendRecvArgs.UserToken).Id;

			if (!sendRecvArgs.AcceptSocket.ReceiveAsync(sendRecvArgs))
				this.ProcessReceive(sendRecvArgs);
		}

		/// <summary>
		/// Processes the received data from the socket.
		/// </summary>
		/// <param name="sendRecvArgs">SAEA object used for receiving data.</param>
		private void ProcessReceive(SocketAsyncEventArgs sendRecvArgs)
		{
			// Close connection when the client is done sending data.
			if ((sendRecvArgs.SocketError != SocketError.Success) || (sendRecvArgs.BytesTransferred == 0))
			{
				this.CloseClientSocket(sendRecvArgs);
				return;
			}

			var data = new byte[sendRecvArgs.BytesTransferred];
			Buffer.BlockCopy(sendRecvArgs.Buffer, 0, data, 0, sendRecvArgs.BytesTransferred);

			var response = this._recvHandler.Handle(data);
			if (response.Length > this._settings.BufferSize)
				throw new OverflowException(
					string.Format("Error. The message size of {0} bytes is larger than the allocated buffer of size of {1}",
					response.Length, this._settings.BufferSize));

			sendRecvArgs.SetBuffer(response, 0, response.Length);

			this.Send(sendRecvArgs);
		}

		/// <summary>
		/// Wraps the asynchronous send call for SAEA.
		/// </summary>
		/// <param name="sendRecvArgs">SAEA instance used for sending.</param>
		private void Send(SocketAsyncEventArgs sendRecvArgs)
		{
			if (this._shutdown)
				sendRecvArgs.AcceptSocket.Shutdown(SocketShutdown.Send);

			if (!sendRecvArgs.AcceptSocket.SendAsync(sendRecvArgs))
				this.ProcessSend(sendRecvArgs);
		}

		/// <summary>
		/// Sends data to a client using a SAEA instance.
		/// </summary>
		/// <param name="sendRecvArgs">SAEA instance used for sending.</param>
		private void ProcessSend(SocketAsyncEventArgs sendRecvArgs)
		{
			if (sendRecvArgs.SocketError != SocketError.Success)
			{
				this.CloseClientSocket(sendRecvArgs);
				return;
			}

			var buffer = new byte[this._settings.BufferSize];
			sendRecvArgs.SetBuffer(buffer, 0, buffer.Length);

			this.Receive(sendRecvArgs);
		}

		/// <summary>
		/// Shuts down the socket handling a client connection and recycles the SAEA.
		/// </summary>
		/// <param name="sendRecvArgs">SAEA used for the client connection.</param>
		private void CloseClientSocket(SocketAsyncEventArgs sendRecvArgs)
		{
			this._currentConnections--;

			try
			{
				sendRecvArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
			}
			catch (Exception e)
			{
				// if the socket was already closed.
				// this should only be object disposed I think.
			}
			finally
			{
				sendRecvArgs.AcceptSocket.Close();
			}

			// Shutdown check
			if (this._shutdown)
			{
				if (sendRecvArgs != null)
					sendRecvArgs.Dispose();
				return;
			}

			// Recycle the SAEA
			this._sendRecvPool.Push(sendRecvArgs);
			this._maxConnectionsControl.Release();
		}

		/// <summary>
		/// Disposes of the socket listener and related resources.
		/// </summary>
		/// <param name="timeout">TimeSpan to wait for connection draining before forcing the server offline.</param>
		public void Shutdown(TimeSpan timeout)
		{
			this._listener.Close();
			this._shutdown = true;

			if (timeout != null)
			{
				var sw = new Stopwatch();
				while (sw.Elapsed.Ticks < timeout.Ticks)
				{
					if (this._currentConnections == 0)
						break;
				}
			}

			this._acceptPool.DisposeAll();
			this._sendRecvPool.DisposeAll();
		}
	}
}
