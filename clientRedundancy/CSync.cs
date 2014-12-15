using libRedundancy.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Authentication;

namespace RedundancyForWindows
{
	class CSync
	{
		public CSync(libRedundancy.libRedundancy i_api, CConfigurationManager i_conf)
		{
			conf = i_conf;
			api = i_api;
		}

		public void startSync()
		{
			Enabled = true;
			Sync();
		}

		public void stopSync()
		{
			Enabled = false;
		}

		private void Sync()
		{
			while (Enabled)
			{
				conf.AuthToken = api.Request<string>("Kernel.UserKernel", "LogIn", new string[] { conf.Username, conf.Password, "true" });
				if (conf.AuthToken != null) {
					Syncing = true;
					GetFiles("/");
					Syncing = false;
					System.Threading.Thread.Sleep(10000); //Think about something better 
				} else {
					Enabled = false;
					throw new AuthenticationException("Wrong Username or Password");
				}
			}
		}

		private void GetFiles(string path) {
			List<FileSystemItem> fsItems = api.Request<List<FileSystemItem>>("Kernel.FileSystemKernel", "GetContent", new string[] { path + @"/", conf.AuthToken });

			foreach (FileSystemItem fsItem in fsItems)
			{
				if (fsItem.FilePath == null) //is folder
				{
					if (!Directory.Exists(conf.LocalPath + path + fsItem.DisplayName))
					{
						Directory.CreateDirectory(conf.LocalPath + path.Replace(@"/", @"\\") + fsItem.DisplayName);
					}
					GetFiles(path + fsItem.DisplayName);
				}
				else
				{
					string remotePath = conf.Hostname + @"/../../Storage/" + fsItem.FilePath;
					string localPath = conf.LocalPath + path.Replace(@"/", @"\\") + @"\\" + fsItem.DisplayName;

					if (!File.Exists(localPath)) //Only download if not existant. Later version will compare hashes.
					{
						WebClient wc = new WebClient();
						wc.DownloadFile(new Uri(remotePath), localPath);
					}
				}
			}
		}

		public volatile bool Syncing { get; private set; }
		public bool Enabled { get; private set; }
		private CConfigurationManager conf { get; set; }
		private libRedundancy.libRedundancy api { get; set; }
	}
}
