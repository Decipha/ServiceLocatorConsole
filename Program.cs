using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLocatorConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			// saves a file with all visible windows services
			Core.Network.ServiceMapper.Map();

		}
	}
}
