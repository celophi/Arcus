using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Infrastructure.Network
{
	public interface IReceiveHandler
	{
		/// <summary>
		/// Implementation for handling data received.
		/// </summary>
		/// <param name="buffer">Byte array containing received data.</param>
		/// <returns>Byte array of data to send to the client.</returns>
		byte[] Handle(byte[] buffer);
	}
}
