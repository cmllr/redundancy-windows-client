using System.Xml;

namespace RedundancyClient
{
    class AppConfig
    {
        public string ApiUri { get; set; }
        public string SyncPath { get; set; }

        public static bool SaveConfig(string path, AppConfig config)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode root, node;

            root = doc.CreateElement("config");
            doc.AppendChild(root);

            node = doc.CreateElement("apiUri");
            node.InnerText = config.ApiUri;
            root.AppendChild(node);

            try
            {
                doc.Save(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static AppConfig LoadConfig(string path)
        {
            try
            {
                AppConfig config = new AppConfig();
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlNode child = doc.SelectSingleNode("/config/apiUri");
                config.ApiUri = child.InnerText;
                child = doc.SelectSingleNode("/config/syncPath");
                config.SyncPath = child.InnerText;
                return config;
            }
            catch
            {
                return null;
            }
        }
    }
}
