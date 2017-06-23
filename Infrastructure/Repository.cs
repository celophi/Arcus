using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure
{
	public class Repository : IRepository
	{
		public IUnitOfWork UnitOfWork { get; private set; }

		public Repository(IUnitOfWork uow)
		{
			this.UnitOfWork = uow;
		}
	}
}
