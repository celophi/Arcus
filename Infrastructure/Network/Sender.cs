using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Network
{
	/// <summary>
	/// Represents a client state.
	/// </summary>
	public class Sender
	{
		/// <summary>
		/// Client socket.
		/// </summary>
		private Socket _socket;

		/// <summary>
		/// Pool of SAEA for sending.
		/// </summary>
		private Pool _pool;

		/// <summary>
		/// Event called when SendAsync completes.
		/// </summary>
		private EventHandler<SocketAsyncEventArgs> _onSendCompleted;

		/// <summary>
		/// Unique identifier for the client.
		/// </summary>
		public Guid Id { get; private set; }

		/// <summary>
		/// Creates a sender implementation to communicate with a client.
		/// </summary>
		/// <param name="socket">Client socket to connect to.</param>
		/// <param name="pool">Pool of SAEA instances to draw from.</param>
		public Sender(Socket socket, Pool pool)
		{
			this.Id = Guid.NewGuid();
			this._socket = socket;
			this._pool = pool;

			this._onSendCompleted = ((sender, e) =>
			{
				if ((e.LastOperation == SocketAsyncOperation.Send) && (e.SocketError != SocketError.Success))
					this._socket.Shutdown(SocketShutdown.Send);

				this._pool.Push(e);
				e.Completed -= this._onSendCompleted;
			});
		}

		/// <summary>
		/// Prepares a byte array to be sent and sends.
		/// </summary>
		/// <param name="buffer"></param>
		public void Send(byte[] buffer)
		{
			if (Listener.IsShuttingDown)
			{
				this._socket.Shutdown(SocketShutdown.Send);
				return;
			}

			this.Process(buffer);
		}

		/// <summary>
		/// Receives data from the client for processing.
		/// </summary>
		/// <param name="buffer"></param>
		public void Handle(byte[] buffer)
		{
			// Implementation
			this.Send(buffer);
		}

		/// <summary>
		/// Sends a buffer to the client asynchronously.
		/// </summary>
		/// <param name="buffer">Byte array to send to the client.</param>
		private void Process(byte[] buffer)
		{
			if (this._socket == null || buffer == null || buffer.Length == 0)
				return;

			var saea = this._pool.Pop();
			saea.SetBuffer(buffer, 0, buffer.Length);
			saea.UserToken = this;
			saea.Completed += this._onSendCompleted;

			this._socket.SendAsync(saea);
		}
	}
}
