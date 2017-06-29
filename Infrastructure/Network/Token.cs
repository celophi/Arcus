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
	public class Token
	{
		private Socket _socket;
		private SocketAsyncEventArgsPool _pool;

		/// <summary>
		/// Unique identifier for the client.
		/// </summary>
		public Guid Id { get; private set; }

		public Token(Socket socket, SocketAsyncEventArgsPool pool)
		{
			this.Id = Guid.NewGuid();
			this._socket = socket;
			this._pool = pool;
		}

		/// <summary>
		/// Sends a buffer to the client asynchronously.
		/// </summary>
		/// <param name="buffer">Byte array to send to the client.</param>
		public void Send(byte[] buffer)
		{
			if (this._socket == null || !this._socket.Connected)
				return;

			if (buffer == null || buffer.Length == 0)
				return;

			var saea = this._pool.Pop();
			saea.SetBuffer(buffer, 0, buffer.Length);
			saea.UserToken = this;
			saea.Completed += ((sender, e) =>
			{
				if ((e.LastOperation == SocketAsyncOperation.Send) && (e.SocketError != SocketError.Success))
				{
					// close the connection
				}

				this._pool.Push(e);
			});

			this._socket.SendAsync(saea);
		}
	}
}
