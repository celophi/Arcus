using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Network
{
	public class SocketListenerSettings
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
	}
}
