using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Network
{
	public class Settings
	{
		/// <summary>
		/// Used for binding new sockets.
		/// </summary>
		public IPEndPoint Endpoint { get; private set; }

		/// <summary>
		/// Number of maximum pending connections queued to be accepted.
		/// </summary>
		public int Backlog { get; private set; } = 100;

		/// <summary>
		/// Determines the pool size of SocketAsyncEventArgs allocated for accept operations.
		/// </summary>
		public int MaxSimultaneousAcceptOps { get; private set; } = 10;

		/// <summary>
		/// The maximum number of connections total.
		/// </summary>
		/// <remarks>The operating system may limit this number.</remarks>
		public int MaxConnections { get; private set; } = 1000;

		/// <summary>
		/// Represents a multiple of SAEA needed per connection.
		/// </summary>
		public int SendersPerConnection { get; private set; } = 3;

		/// <summary>
		/// The buffer size used for send and receive operations.
		/// </summary>
		public int BufferSize { get; private set; } = (1024 * 64);

		public Settings(IPEndPoint endpoint)
		{
			this.Endpoint = endpoint;
		}
	}
}
