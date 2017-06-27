using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Network
{
	public class ReceiveHandler : IReceiveHandler
	{
		public byte[] Handle(byte[] buffer)
		{
			//throw new NotImplementedException();
			return buffer;
		}
	}
}
