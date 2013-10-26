using System.Xml;
using System.Security;

namespace RedundancyClient
{
    class UserConfig
    {
        public string UserName { get; set; }
        public SecureString Password { get; set; }

        public static bool SaveConfig(string path, UserConfig config)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode root, node;

            root = doc.CreateElement("config");
            doc.AppendChild(root);

            node = doc.CreateElement("userName");
            node.InnerText = config.UserName;
            root.AppendChild(node);

            node = doc.CreateElement("password");
            node.InnerText = StringCryptography.EncryptString(config.Password);
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

        public static UserConfig LoadConfig(string path)
        {
            try
            {
                UserConfig config = new UserConfig();
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlNode child = doc.SelectSingleNode("/config/userName");
                config.UserName = child.InnerText;
                child = doc.SelectSingleNode("/config/password");
                config.Password = StringCryptography.DecryptString(child.InnerText);
                return config;
            }
            catch
            {
                return null;
            }
        }
    }
}