using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure
{
	public interface IUnitOfWork : IDisposable
	{
		/// <summary>
		/// Database transaction object.
		/// </summary>
		IDbTransaction Transaction { get; }

		/// <summary>
		/// Database connection object.
		/// </summary>
		IDbConnection Connection { get; }

		/// <summary>
		/// Called to persist all recorded transactions to the database.
		/// </summary>
		void Commit();
	}
}
