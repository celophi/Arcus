using System;
using System.Collections.Generic;
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
		/// Prevents the socket from accepting more than the maximum set connections.
		/// </summary>
		private Semaphore _maxConnectionsControl;

		/// <summary>
		/// Parent socket used for listening to incoming requests.
		/// </summary>
		private Socket _listener;

		public SocketListener(SocketListenerSettings settings)
		{
			this._settings = settings;

			this._acceptPool = new SocketAsyncEventArgsPool(settings.MaxSimultaneousAcceptOps);
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
			if (!this._listener.AcceptAsync(acceptArgs))
			{
				this.ProcessAccept(acceptArgs);
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

		private void ProcessAccept(SocketAsyncEventArgs acceptArgs)
		{
			// temporary
			try
			{
				acceptArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
			}
			catch (ObjectDisposedException e)
			{
				
			}
			finally
			{
				this._maxConnectionsControl.Release();
			}
		}
	}
}
