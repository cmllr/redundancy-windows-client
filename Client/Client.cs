using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HttpPostRequestLib.Net;

namespace RedundancyClient
{
    public class Client
    {
        public enum Status
        {
            Syncing,
            Error,
            Idle
        }

        public enum Action
        {
            getFiles,
            getFileIDsWithDisplaynames,
            getFileIDsWithDisplaynamesAndFilenamesAndUploadDate,
            getProperty,
            getContent,
            getName,
            getVersion,
            getLatestFiles,
            uploadFile,
            renameFile,
            renameFolder,
            copy,
            move,
            getHash,
            exists,
            createDir,
            deleteFile,
            deleteFolder
        }

        public String ApiKey { get; private set; }
        public Uri ApiUri { get; private set; }
        public String UserAgent { get; private set; }
        public int TransactionCount { get; set; }
        public string syncPath { get; private set; }

        public Client(String apiKey, String url, string userAgent, string syncPath)
        {
            this.ApiKey = apiKey;
            this.ApiUri = new Uri(url);
            this.UserAgent = userAgent;
            this.syncPath = syncPath;
        }

        /// <summary>
        /// Zergliedert CSV Werte, die folgendermaßen separiert sind: "wert1";"wert2" oder "wert1""wert2" oder beides
        /// </summary>
        /// <param name="csv"></param>
        /// <returns></returns>
        private List<string> parseCSV(string csv)
        {
            List<string> splittedValues = new List<string>();
            foreach (Match m in Regex.Matches(csv, "\"([^\"]*)\""))
                splittedValues.Add(m.Groups[1].ToString());
            return splittedValues;
        }

        public bool acknowledge()
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("ACKONLY", "true");
            request.UserAgent = UserAgent;
            TransactionCount++;
            string content = request.Submit();
            return bool.Parse(content);
        }

        public string getVersion()
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.getVersion.ToString());
            request.UserAgent = UserAgent;
            TransactionCount++;
            string content = request.Submit();
            return content;
        }

        ////key := FileID, value := Displayname
        //public Dictionary<string, string> getFileIDsWithDisplaynames(string dir)
        //{
        //    HTTPPostRequest request = new HTTPPostRequest(this.apiUri.ToString());
        //    request.Post.Add("dir", dir);
        //    request.Post.Add("ACK", apiKey);
        //    request.Post.Add("Method", Action.getFileIDsWithDisplaynames.ToString());
        //    request.UserAgent = UserAgent;
        //    TransactionCount++;
        //    string content = request.Submit();
        //    string[] values = content.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        //    Dictionary<string, string> files = new Dictionary<string, string>();
        //    for (int i = 0; i < values.Length; i += 2)
        //        files.Add(values[i], values[i + 1]);
        //    return files;
        //}

        public List<Entry> getFileIDsWithDisplaynamesAndFilenames(string dir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("dir", dir);
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.getFileIDsWithDisplaynamesAndFilenamesAndUploadDate.ToString());
            request.UserAgent = UserAgent;
            TransactionCount++;
            string content = request.Submit();

            List<string> values = parseCSV(content);
            List<Entry> entries = new List<Entry>();
            for (int i = 0; i < values.Count; i += 4)
            {
                int id = int.Parse(values[i]);
                string displayName = values[i + 1];
                string fileName = values[i + 2];
                DateTime dateTime = DateTime.Parse(values[i + 3]);
                entries.Add(new Entry(id, displayName, fileName)
                {
                    Uploaded = dateTime
                });
            }
            return entries;
        }

        private string prepareEntriesForGetLatestFiles(List<Entry> entries)
        {
            StringBuilder csv = new StringBuilder();
            foreach(Entry entry in entries)
            {
                csv.Append("\"");
                csv.Append(entry.ID.ToString());
                csv.Append("\"");
                csv.Append("\"");
                csv.Append(entry.Uploaded.ToUniversalTime());
                csv.Append("\"");
            }
            return csv.ToString();
        }

        private List<Entry> convertCSVFromGetLatestFilesToList(string csv)
        {
            List<Entry> list = new List<Entry>();
            List<string> csvValues = parseCSV(csv);
            for (int i = 0; i < csvValues.Count; i += 4)
            {
                int id = int.Parse(csvValues[i]);
                string displayName = csvValues[i + 1];
                string fileName = csvValues[i + 2];
                DateTime dateTime = DateTime.Parse(csvValues[i + 3]);
                list.Add(new Entry(id, displayName, fileName)
                {
                    Uploaded = dateTime
                });
            }
            return list;
        }

        //public List<Entry> getNewerFiles(List<Entry> localEntries, List<Entry> serverEntries)
        //{
        //    if (localEntries.Count > 0)
        //    {
        //        if(
        //    }
        //    return new List<Entry>();
        //}

        public string getDirPath(string dir)
        {
            if (dir == "/")
                return syncPath;
            else
                return Path.Combine(syncPath, dir);
        }

        public List<Entry> getFiles(String dir)
        {
            List<Entry> serverEntries = getFileIDsWithDisplaynamesAndFilenames(dir);
            List<Entry> newEntries = new List<Entry>();
            List<Entry> localEntries = new List<Entry>();
            if (!Directory.Exists(getDirPath(dir)))
            {
                newEntries.AddRange(serverEntries);
                foreach(Entry entry in serverEntries)
                    if(entry.IsFolder())
                        newEntries.AddRange(getFiles(entry.DisplayName));
            }
            else
            {
                foreach(Entry entry in serverEntries)
                {
                    string path = Path.Combine(syncPath, entry.DisplayName);
                    if(entry.IsFolder())
                        newEntries.AddRange(getFiles(entry.DisplayName));
                    else if (File.Exists(path))
                    {
                        //Überprüfe, ob lokale Datei aktuell ist
                        FileInfo file = new FileInfo(path);
                        if (file.CreationTime >= entry.Uploaded)
                            localEntries.Add(entry);
                        else
                            newEntries.Add(entry);
                    }
                }
            }
            return newEntries;
        }

        public string getNameByID(string id)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("id", id);
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.getName.ToString());
            request.UserAgent = UserAgent;
            string content = request.Submit();
            TransactionCount++;
            return content;
        }

        public Entry getEntryByID(string sid, bool download)
        {
            if (download == true)
                Console.WriteLine("Starting Download of " + sid);
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("id", sid);
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.getProperty.ToString());
            request.UserAgent = UserAgent;
            request.Encoding = Encoding.UTF8;
            TransactionCount++;
            string content = request.Submit();
            string[] entries = content.Split(';');
            int id = int.Parse(entries[0]);
            string displayName = entries[2];
            string fileName = entries[1];
            Entry entry = new Entry(id, displayName, fileName)
            {
                hash = entries[4],
                Uploaded = DateTime.Parse(entries[5]),
                SizeInByte = int.Parse(entries[6]),
                UsedClient = entries[7],
                Directory = entries[9],
                localHash = Common.getMD5(entries[2])
            };
            if (entry.DisplayName == entry.FileName)
            {
                if (download == true)
                {
                    entry.FileName = Common.correctString(entry.FileName);
                    entry.Directory = Common.correctString(entry.Directory);
                    entry.DisplayName = Common.correctString(entry.DisplayName);
                    if (System.IO.Directory.Exists("./Sync/" + entry.DisplayName) == false)
                    {
                        System.IO.Directory.CreateDirectory("./Sync/" + entry.DisplayName);
                        Console.WriteLine("Got new dir: " + entry.DisplayName);
                        new DirectoryInfo("./Sync/" + entry.DisplayName).CreationTime = entry.Uploaded;
                    }
                    else
                    {
                        Console.WriteLine("Dir already here: " + entry.DisplayName);
                    }
                }
            }
            else
            {
                if (download == true)
                {						
                    entry.DisplayName = Common.correctString(entry.DisplayName);
                    string syncPath = Environment.CurrentDirectory + "\\Sync" + entry.Directory.Replace("/", "\\") + entry.DisplayName;
                    if (File.Exists(syncPath) == false)
                    {
                        entry.Content = getContent(entry.ID);
                        System.IO.File.WriteAllBytes(syncPath, entry.Content);
                        Console.WriteLine("Got new file: " + entry.DisplayName);
                        try
                        {
                            new FileInfo(syncPath).CreationTime = entry.Uploaded;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message + ex.StackTrace);
                        }
                    }
                    else
                    {
                        if (entry.SizeInByte != new System.IO.FileInfo(syncPath).Length)
                        {
                            Console.WriteLine("file would be resynced" + entry.DisplayName);
                        }
                        else
                        {
                            Console.WriteLine("File already synced: " + entry.DisplayName);
                        }
                    }
                }
            }
            return entry;
        }

        Byte[] getContent(int id)
        {
            WebClient client = new WebClient();
            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            client.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);
            byte[] result = client.UploadData(this.ApiUri.ToString(), "POST", System.Text.Encoding.UTF8.GetBytes("id=" + id.ToString() + "&ACK=" + ApiKey + "&Method=" + Action.getContent.ToString()));
            TransactionCount++;
            return result;
        }

        public bool uploadFile(string Path, string currentdir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.uploadFile.ToString());
            request.Post.Add("currentdir", currentdir);
            request.Files.Add("userfile[]", Path);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
        public bool renameFile(string hash, string newName, string currentdir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.renameFile.ToString());
            request.Post.Add("file", hash);
            request.Post.Add("newname", newName);
            request.UserAgent = UserAgent;
            request.Encoding = System.Text.Encoding.UTF8;
            request.Post.Add("currentdir", currentdir);
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
        public bool renameFolder(string source, string newName, string old_root, string currentdir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.renameFolder.ToString());
            request.Post.Add("newname", newName);
            request.Post.Add("source", source);
            request.Post.Add("old_root", old_root);
            request.Post.Add("currentdir", currentdir);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
        public bool copyFile(string target, string hash)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.copy.ToString());
            request.Post.Add("file", hash);
            request.UserAgent = UserAgent;
            request.Post.Add("dir", target);
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
        public bool copyFolder(string source, string target, string old_root)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.copy.ToString());
            request.Post.Add("source", source);
            request.Post.Add("target", target);
            request.Post.Add("old_root", old_root);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
        public bool moveFile(string target, string hash)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.move.ToString());
            request.Post.Add("file", hash);
            request.UserAgent = UserAgent;
            request.Post.Add("dir", target);
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
        public bool moveFolder(string source, string target, string old_root)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.move.ToString());
            request.Post.Add("source", source);
            request.Post.Add("target", target);
            request.Post.Add("old_root", old_root);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
        public bool exists(string entry, string dir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.exists.ToString());
            request.Post.Add("entry", entry);
            request.Post.Add("dir", dir);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
        public bool createDir(string entry, string dir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.createDir.ToString());
            request.Post.Add("entry", entry);
            request.Post.Add("dir", dir);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
        public string getHash(string file, string dir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.getHash.ToString());
            request.Post.Add("file", file);
            request.Post.Add("dir", dir);
            request.UserAgent = UserAgent;
            //request.Encoding = Encoding.UTF8;
            string result = request.Submit();
            return result;
        }
        public bool deleteFile(string hash)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.deleteFile.ToString());
            request.Post.Add("s", "true");
            request.Post.Add("file", hash);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
        public bool deleteFolder(string dir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("ACK", ApiKey);
            request.Post.Add("Method", Action.deleteFolder.ToString());
            request.Post.Add("s", "true");
            request.Post.Add("dir", dir);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            if (result == "false")
                return false;
            else
                return true;
        }
    }
}