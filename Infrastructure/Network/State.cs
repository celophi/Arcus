using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Network
{
	/// <summary>
	/// Represents a client state.
	/// </summary>
	public class State
	{
		/// <summary>
		/// Unique identifier for the client.
		/// </summary>
		public Guid Id { get; private set; }

		public State()
		{
			this.Id = Guid.NewGuid();
		}
	}
}
