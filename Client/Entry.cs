using System;

namespace RedundancyClient
{
	public class Entry
	{
		public String DisplayName {get;set;}
		public String ShortDisplayName {get;set;}
		public String FileName {get;set;}
		public DateTime Uploaded {get;set;}
		public String UsedClient {get;set;}
		public int SizeInByte {get;set;}
		public string hash {get;set;}
		public string localHash;
		public string Directory {get;set;}
		public int ID {get;set;}
		public Byte[] Content {get;set;}

        public Entry(int id, string displayName, string fileName)
        {
            ID = id;
            DisplayName = displayName;
            FileName = fileName;
        }

        public bool IsFolder()
        {
            return DisplayName == FileName;
        }
	}
}