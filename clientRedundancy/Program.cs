using System;
using System.Threading;

namespace RedundancyForWindows
{
	class Program
	{
		static void Main(string[] args)
		{
			CConfigurationManager conf = new CConfigurationManager();
			libRedundancy.libRedundancy api = new libRedundancy.libRedundancy(new Uri(conf.Hostname));
			
			CSync sync = new CSync(api, conf);
			Thread syncThread = new Thread(sync.startSync);
			syncThread.Start();

			CUserInterface ui = new CUserInterface();
		}
	}
}

