using System;
using System.Text;
using System.Collections.Generic;

namespace RedundancyClient
{
    public class Entry
    {
        public String DisplayName { get; set; }
        public String ShortDisplayName { get; set; }
        public String FileName { get; set; }
        public DateTime LastWriteTime { get; set; }
        public String UserAgent { get; set; }
        public int SizeInByte { get; set; }
        public string Hash { get; set; }
        public string Directory { get; set; }
        public int ID { get; set; }
        public Byte[] Content { get; set; }
        public Dictionary<string, Entry> Entries { get; set; }
        public bool FromServer { get; set; }

        public Entry(bool fromServer)
        {
            this.FromServer = fromServer;
        }

        public Entry(bool fromServer, int id, string displayName, string fileName, DateTime creationTime)
        {
            this.FromServer = fromServer;
            this.ID = id;
            this.DisplayName = displayName;
            this.FileName = fileName;
            this.LastWriteTime = creationTime;
        }

        public bool IsFolder()
        {
            return DisplayName == FileName;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(ID);
            builder.Append("; ");
            builder.Append(DisplayName);
            builder.Append("; ");
            builder.Append(FileName);
            builder.Append("; ");
            builder.Append(Directory);
            return builder.ToString();
        }
    }
}