using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure
{
	public interface IRepository
	{
		IUnitOfWork UnitOfWork { get; }
	}
}
