using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure
{
	public class UnitOfWork : IUnitOfWork
	{
		public IDbTransaction Transaction { get; private set; }
		public IDbConnection Connection { get; private set; }
		private bool _disposed = false;

		public UnitOfWork(IDbConnection connection)
		{
			if (connection.State != ConnectionState.Closed)
				throw new InvalidOperationException("Error. The IDBConnection object must be supplied in a closed state.");

			this.Connection = connection;
			this.Connection.Open();
			this.Transaction = this.Connection.BeginTransaction();
		}

		public void Commit()
		{
			try
			{
				this.Transaction.Commit();
			}
			catch (Exception e)
			{
				this.Transaction.Rollback();
				throw e;
			}
			finally
			{
				this.Transaction.Dispose();
			}
		}

		// Refer to this explanation on how to implement disposal properly
		// https://stackoverflow.com/a/151244/1343005
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{
					if (this.Transaction != null)
					{
						this.Transaction.Dispose();
						this.Transaction = null;
					}

					if (this.Connection != null)
					{
						this.Connection.Dispose();
						this.Connection = null;
					}
				}
			}

			this._disposed = true;
		}

		~UnitOfWork()
		{
			this.Dispose(false);
		}
	}
}
