using Arcus.Infrastructure;
using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Datastore
{
	public class Repository<TEntity> : IRepository<TEntity> where TEntity : class, new()
	{
		public IUnitOfWork UnitOfWork { get; private set; }

		public Repository(IUnitOfWork uow)
		{
			this.UnitOfWork = uow;
		}

		public virtual bool Delete(IEnumerable<TEntity> entities)
		{
			return this.UnitOfWork.Connection.Delete(entities, transaction: this.UnitOfWork.Transaction);
		}

		public virtual bool Delete(TEntity entity)
		{
			return this.UnitOfWork.Connection.Delete(entity, transaction: this.UnitOfWork.Transaction);
		}

		public virtual TEntity Get(object primaryKey)
		{
			return this.UnitOfWork.Connection.Get<TEntity>(primaryKey, transaction: this.UnitOfWork.Transaction);
		}

		public virtual IEnumerable<TEntity> GetAll()
		{
			return this.UnitOfWork.Connection.GetAll<TEntity>(transaction: this.UnitOfWork.Transaction);
		}

		public virtual long Insert(IEnumerable<TEntity> entities)
		{

			return this.UnitOfWork.Connection.Insert(entities, transaction: this.UnitOfWork.Transaction);
		}

		public virtual long Insert(TEntity entity)
		{
			return this.UnitOfWork.Connection.Insert(entity, transaction: this.UnitOfWork.Transaction);
		}

		public virtual bool Update(IEnumerable<TEntity> entities)
		{
			return this.UnitOfWork.Connection.Update(entities, transaction: this.UnitOfWork.Transaction);
		}

		public virtual bool Update(TEntity entity)
		{
			return this.UnitOfWork.Connection.Update(entity, transaction: this.UnitOfWork.Transaction);
		}
	}
}
