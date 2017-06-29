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
		public SocketListener(SocketListenerSettings settings)
		{
			this._settings = settings;
			this._maxConnectionsControl = new Semaphore(settings.MaxConnections, settings.MaxConnections);

			this._acceptPool = new SocketAsyncEventArgsPool(settings.MaxSimultaneousAcceptOps);
			this._sendRecvPool = new SocketAsyncEventArgsPool(settings.MaxConnections);
			this._sendPool = new SocketAsyncEventArgsPool(settings.MaxConnections * settings.SendersPerConnection);


			// Pre-allocate resources for send/receive SAEA.
			//for (int i = 0; i < settings.MaxConnections; i++)
			//{
			//	var sendRecvArgs = new SocketAsyncEventArgs();
			//	var sendArgs = new SocketAsyncEventArgs();

			//	// Necessary event for processing immediately after connection has completed.
			//	sendRecvArgs.Completed += ((sender, saea) =>
			//	{
			//		switch (saea.LastOperation)
			//		{
			//			case SocketAsyncOperation.Receive:
			//				this.ProcessReceive(saea);
			//				break;
			//			default:
			//				throw new ArgumentException("Error. The last operation was not a 'send' or 'receive'.");
			//		}
			//	});

			//	sendRecvArgs.SetBuffer(new byte[settings.BufferSize], 0, settings.BufferSize);

			//	this._sendRecvPool.Push(sendRecvArgs);
			//}
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
			var saea = this._acceptPool.Pop();
			saea.Completed += ((sender, e) =>
			{
				this.ProcessAccept(e);
			});

			// If the maximum allowed connections has not been reached, accept one connection.
			// If the operation completes synchronously, then move to processing; otherwise,
			// the event attached in NewAcceptSAEA will handle it later.
			this._maxConnectionsControl.WaitOne();

			try
			{
				if (!this._listener.AcceptAsync(saea))
					this.ProcessAccept(saea);
			}
			catch (ObjectDisposedException)
			{
				if (this._shutdown)
					saea.Dispose();
				else
					throw;
			}
		}

		/// <summary>
		/// Processes a SAEA once the 'accept' operation has completed.
		/// </summary>
		/// <param name="acceptor">SAEA instance in the 'accept' completed state.</param>
		private void ProcessAccept(SocketAsyncEventArgs acceptor)
		{
			// Dispose the socket, queue the SAEA for reuse, start another accept call.
			if (acceptor.SocketError != SocketError.Success)
			{
				acceptor.AcceptSocket.Close();
				this._acceptPool.Push(acceptor);
				this.Accept();
				return;
			}
			
			this._currentConnections++;

			// Obtain a receiver SAEA
			var receiver = this._sendRecvPool.Pop();
			receiver.AcceptSocket = acceptor.AcceptSocket;

			// Recycle the acceptor
			this._acceptPool.Push(acceptor);

			// Generate a sender
			receiver.UserToken = new Sender(receiver.AcceptSocket, this._sendPool);

			// Prepare a buffer
			var buffer = new byte[this._settings.BufferSize];
			receiver.SetBuffer(buffer, 0, buffer.Length);

			// Assign the event handler
			receiver.Completed += ((sender, e) =>
			{
				if ((e.LastOperation == SocketAsyncOperation.Receive) && (e.SocketError != SocketError.Success))
				{
					// close the connection
				}

				this.ProcessReceive(e);
			});

			// Start receiving
			if (!receiver.AcceptSocket.ReceiveAsync(receiver))
				this.ProcessReceive(receiver);

			this.Accept();
		}

		/// <summary>
		/// Processes the received data from the socket.
		/// </summary>
		/// <param name="receiver">SAEA object used for receiving data.</param>
		private void ProcessReceive(SocketAsyncEventArgs receiver)
		{
			// Close connection when the client is done sending data.
			if ((receiver.SocketError != SocketError.Success) || (receiver.BytesTransferred == 0))
			{
				// close client
				return;
			}

			// Retrieve data
			var data = new byte[receiver.BytesTransferred];
			Buffer.BlockCopy(receiver.Buffer, 0, data, 0, receiver.BytesTransferred);

			// Handle
			var sender = (Sender)receiver.UserToken;
			sender.Handle(data);

			// Start receiving
			if (!receiver.AcceptSocket.ReceiveAsync(receiver))
				this.ProcessReceive(receiver);
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
