using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Network
{
	public class Pool
	{
		/// <summary>
		/// The internal managed pool.
		/// </summary>
		private Stack<SocketAsyncEventArgs> _pool;

		/// <summary>
		/// Represents the initial capacity the pool was configured with.
		/// </summary>
		private int _capacity;

		/// <summary>
		/// Creates a pool of SocketAsyncEventArgs.
		/// </summary>
		/// <param name="size">Maximum size of the pool.</param>
		public Pool(int capacity)
		{
			this._capacity = capacity;
			this._pool = new Stack<SocketAsyncEventArgs>(capacity);
		}

		/// <summary>
		/// Returns an available instance from the pool.
		/// </summary>
		/// <returns>SAEA instance</returns>
		/// <remarks>Creates a new instance when the pool has none available.</remarks>
		public SocketAsyncEventArgs Pop()
		{
			lock (this._pool)
			{
				// return a temporary new instance if none are available.
				if (this._pool.Count > 0)
					return this._pool.Pop();
				else
					return new SocketAsyncEventArgs();
			}
		}

		/// <summary>
		/// Adds an instance back to the pool.
		/// </summary>
		/// <param name="instance">A SocketAsyncEventArgs object.</param>
		/// <remarks>Disposes additional instances when the pool has sufficient capacity.</remarks>
		public void Push(SocketAsyncEventArgs instance)
		{
			if (instance == null)
				throw new ArgumentNullException("Error. The SocketAsyncEventArgs object must not be null.");

			lock (this._pool)
			{
				instance.UserToken = null;
				instance.AcceptSocket = null;

				// Dispose the instance if it goes over the initial capacity.
				if (this._pool.Count < this._capacity)
					this._pool.Push(instance);
				else
					instance.Dispose();
			}
		}

		/// <summary>
		/// Disposes all of the SAEA instances.
		/// </summary>
		public void DisposeAll()
		{
			lock (this._pool)
			{
				while (this._pool.Count > 0)
				{
					var saea = this._pool.Pop();
					saea.Dispose();
				}
			}
		}
	}
}
