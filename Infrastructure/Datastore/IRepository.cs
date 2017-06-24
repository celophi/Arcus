using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Datastore
{
	public interface IRepository<TEntity> where TEntity : class, new()
	{
		IUnitOfWork UnitOfWork { get; }
		long Insert(TEntity entity);
		long Insert(IEnumerable<TEntity> entities);
		bool Update(TEntity entity);
		bool Update(IEnumerable<TEntity> entities);
		bool Delete(TEntity entity);
		bool Delete(IEnumerable<TEntity> entities);
		TEntity Get(object primarykey);
		IEnumerable<TEntity> GetAll();
	}
}
