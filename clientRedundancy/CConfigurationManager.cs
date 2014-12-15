using System;
using System.IO;
using System.Xml.Linq;

namespace RedundancyForWindows
{
	class CConfigurationManager
	{
		private string cfgName = "config.xml";

		public CConfigurationManager() {
			if (File.Exists(cfgName)){
				readConfigFile();
			} else {
				generateConfigDummy();
			}
		}

		public string Hostname { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }
		public string LocalPath { get; private set; }

		public string AuthToken { get; set; }


		private void generateConfigDummy(){
			XDocument config = new XDocument(
				new XElement("redundancy",
					new XElement("hostname", "http://server/Includes/api.inc.php"),
					new XElement("username", "admin"),
					new XElement("password", "secretpass"),
					new XElement("localpath", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
				)
			);
			config.Save(cfgName);
			throw new ApplicationException("Change values in config.xml and restart Redundancy for Windows!");
		}

		private void readConfigFile() {
			XDocument config = XDocument.Load("config.xml");
			XElement root = config.Element("redundancy");
			Hostname = root.Element("hostname").Value;
			Username = root.Element("username").Value;
			Password = root.Element("password").Value;
			LocalPath = root.Element("localpath").Value;
		}
	}
}
