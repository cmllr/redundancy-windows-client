using System;
using System.Text;

namespace RedundancyClient
{
    public class Entry
    {
        public String DisplayName { get; set; }
        public String ShortDisplayName { get; set; }
        public String FileName { get; set; }
        public DateTime CreationDate { get; set; }
        public String UserAgent { get; set; }
        public int SizeInByte { get; set; }
        public string Hash { get; set; }
        //public string LocalHash { get; set; }
        public string Directory { get; set; }
        public int ID { get; set; }
        public Byte[] Content { get; set; }

        public Entry(int id, string displayName, string fileName, DateTime creationDate)
        {
            this.ID = id;
            this.DisplayName = displayName;
            this.FileName = fileName;
            this.CreationDate = creationDate;
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