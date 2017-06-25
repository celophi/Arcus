using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Network
{
	public class SocketAsyncEventArgsPool
	{
		/// <summary>
		/// The internal managed pool.
		/// </summary>
		private Stack<SocketAsyncEventArgs> _pool;

		/// <summary>
		/// Returns the number of objects in the pool.
		/// </summary>
		public int Count { get { return this._pool.Count; } }

		/// <summary>
		/// Creates a pool of SocketAsyncEventArgs.
		/// </summary>
		/// <param name="size">Maximum size of the pool.</param>
		public SocketAsyncEventArgsPool(int size)
		{
			this._pool = new Stack<SocketAsyncEventArgs>(size);
		}

		/// <summary>
		/// Returns an available instance from the pool.
		/// </summary>
		/// <returns></returns>
		/// <remarks>Returns null if no instances from the pool are available.</remarks>
		public SocketAsyncEventArgs Pop()
		{
			lock (this._pool)
			{
				if (this.Count > 0)
					return this._pool.Pop();
				else
					return null;
			}
		}

		/// <summary>
		/// Adds an instance back to the pool.
		/// </summary>
		/// <param name="instance">A SocketAsyncEventArgs object.</param>
		public void Push(SocketAsyncEventArgs instance)
		{
			if (instance == null)
				throw new ArgumentNullException("Error. The SocketAsyncEventArgs object must not be null.");

			lock (this._pool)
				this._pool.Push(instance);
		}
	}
}
