using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Network
{
	public class Server
	{
		/// <summary>
		/// Used for binding new sockets.
		/// </summary>
		private IPEndPoint _endpoint;

		/// <summary>
		/// Used for listening for connections.
		/// </summary>
		private Socket _listener;

		/// <summary>
		/// Byte array size allocated for receiving messages from clients.
		/// </summary>
		private int _recvBufferSize = 8192;

		/// <summary>
		/// Number of maximum pending connections queued to be accepted.
		/// </summary>
		public int backlog { get; set; } = 100;
		
		/// <summary>
		/// Constructs a server for sending and receiving data.
		/// </summary>
		/// <param name="address">An IPAddress as a string used for listening.</param>
		/// <param name="port">A port used for listening.</param>
		public Server(string address, int port)
		{
			IPAddress ipAddr;
			if (!IPAddress.TryParse(address, out ipAddr))
				throw new ArgumentException("Error. The 'address' parameter was invalid.");
			this._endpoint = new IPEndPoint(ipAddr, port);
		}

		/// <summary>
		/// Constructs a server for sending and receiving data.
		/// </summary>
		/// <param name="address">An IPAddress used for listening.</param>
		/// <param name="port">A port used for listening.</param>
		public Server(IPAddress address, int port)
		{
			this._endpoint = new IPEndPoint(address, port);
		}

		/// <summary>
		/// Constructs a server for sending and receiving data.
		/// </summary>
		/// <param name="endpoint">A network endpoint used for listening.</param>
		public Server(IPEndPoint endpoint)
		{
			this._endpoint = endpoint;
		}

		/// <summary>
		/// Instructs the server to open a socket and start.
		/// </summary>
		public void Start()
		{
			this._listener = new Socket(this._endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			this._listener.Bind(this._endpoint);
			this._listener.Listen(this.backlog);

			this.AcceptConnections();
		}

		/// <summary>
		/// Begins a loop to accept incoming connections.
		/// </summary>
		private async void AcceptConnections()
		{
			while (true)
			{
				var child = await this.AcceptAsync(this._listener);
				this.ReceiveConnectionData(child);
			}
		}

		/// <summary>
		/// Begins a loop to receive data for a single connection.
		/// </summary>
		/// <param name="child"></param>
		private async void ReceiveConnectionData(Socket child)
		{
			while (true)
			{
				var buffer = new byte[this._recvBufferSize];
				var close = await ReceiveAsync(child, buffer, 0, buffer.Length, SocketFlags.None);
				if (close)
				{
					// Close this connection
					break;
				}
			}
		}

		/// <summary>
		/// Accepts a pending connection on a listening socket.
		/// </summary>
		/// <param name="socket"></param>
		/// <returns>Task yielding a child socket for receiving.</returns>
		public Task<Socket> AcceptAsync(Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException("Error. The socket was null.");

			var tcs = new TaskCompletionSource<Socket>();

			var cb = new AsyncCallback((result) =>
			{
				try
				{
					var state = (Socket)result.AsyncState;
					var client = state.EndAccept(result);
					
					tcs.SetResult(client);
				}
				catch (Exception e)
				{
					tcs.SetException(e);
				}
			});

			socket.BeginAccept(cb, socket);
			return tcs.Task;
		}

		/// <summary>
		/// Performs an asynchronous task to receive data on a socket.
		/// </summary>
		/// <param name="socket">Socket to receive data on.</param>
		/// <param name="buffer">Byte array to place received data.</param>
		/// <param name="offset">Offset into the buffer to start writing data.</param>
		/// <param name="size">Number of bytes to receive.</param>
		/// <param name="socketFlags">Bitwise combination of SocketFlags.</param>
		/// <returns>Task yielding a boolean where 'true' indicates that zero bytes were received.</returns>
		public Task<bool> ReceiveAsync(Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
		{
			var tcs = new TaskCompletionSource<bool>(socket);

			var cb = new AsyncCallback((result) =>
			{
				try
				{
					var state = (TaskCompletionSource<bool>)result.AsyncState;
					var socketState = (Socket)state.Task.AsyncState;

					var bytesReceived = socketState.EndReceive(result);
					var close = (bytesReceived == 0);

					tcs.SetResult(close);
				}
				catch (Exception e)
				{
					tcs.SetException(e);
				}
			});

			socket.BeginReceive(buffer, offset, size, socketFlags, cb, tcs);
			return tcs.Task;
		}
	}
}
