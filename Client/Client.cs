using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using HttpPostRequestLib.Net;
using System.Diagnostics;

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
            getFileHeadsAsXML,
            getProperties,
            getPropertiesAsXML,
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
        public string SyncPath { get; private set; }
        public bool Log { get; set; }

        public Client(String apiKey, String url, string userAgent, string syncPath)
        {
            this.ApiKey = apiKey;
            this.ApiUri = new Uri(url);
            this.UserAgent = userAgent;
            this.SyncPath = syncPath;
            this.Log = false;
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

        public bool checkApiKey()
        {
            if (Log) Console.Write("Check API Key...");
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("keyOnly", "true");
            request.UserAgent = UserAgent;
            TransactionCount++;
            string content = request.Submit();
            bool result = content == "true";
            if (Log) Console.WriteLine(result);
            return result;
        }

        public string getVersion()
        {
            if (Log) Console.Write("Get API version...");
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.getVersion.ToString());
            request.UserAgent = UserAgent;
            TransactionCount++;
            string content = request.Submit();
            if (Log) Console.WriteLine(content);
            return content;
        }

        ////key := FileID, value := Displayname
        //public Dictionary<string, string> getFileIDsWithDisplaynames(string dir)
        //{
        //    HTTPPostRequest request = new HTTPPostRequest(this.apiUri.ToString());
        //    request.Post.Add("dir", dir);
        //    request.Post.Add("key", apiKey);
        //    request.Post.Add("method", Action.getFileIDsWithDisplaynames.ToString());
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
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.getFileIDsWithDisplaynamesAndFilenamesAndUploadDate.ToString());
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
                DateTime creationDate = DateTime.Parse(values[i + 3]);
                entries.Add(new Entry(id, displayName, fileName, creationDate));
            }
            return entries;
        }

        public List<Entry> getFileHeads(string dir)
        {
            if (Log) Console.Write("Get file heads of files in {0}...", dir);
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("dir", dir);
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.getFileHeadsAsXML.ToString());
            request.UserAgent = UserAgent;
            TransactionCount++;
            string content = request.Submit();

            List<Entry> entries = new List<Entry>();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            foreach (XmlNode child in doc.DocumentElement)
            {
                int id = int.Parse(child.Attributes["id"].Value);
                string displayName = child.Attributes["displayName"].Value;
                string fileName = child.Attributes["fileName"].Value;
                DateTime creationDate = DateTime.Parse(child.Attributes["creationDate"].Value);
                Entry entry = new Entry(id, displayName, fileName, creationDate);
                entries.Add(entry);
            }
            if (Log) Console.WriteLine("done");
            return entries;
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
                DateTime creationDate = DateTime.Parse(csvValues[i + 3]);
                list.Add(new Entry(id, displayName, fileName, creationDate));
            }
            return list;
        }

        public string getDirPath(string dir)
        {
            if (dir == "/")
                return SyncPath;
            else
                return Path.Combine(SyncPath, dir);
        }

        /// <summary>
        /// Verändert Redundancy Ordnerpfade zu Windowspfaden, um Path.Combine(...) verwenden zu können
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string preparePath(string path)
        {
            path = path.Replace('/', '\\');
            return path.Substring(1);
        }

        public void create(int id)
        {
            Entry entry = getProperties(id);
            string path;
            if (entry.IsFolder())
            {
                path = Path.Combine(SyncPath, preparePath(entry.DisplayName));
                if (!Directory.Exists(path))
                {
                    if (Log) Console.Write("Create dir {0}...", path);
                    Directory.CreateDirectory(path);
                    if (Log) Console.WriteLine("done");
                }
            }
            else
            {
                path = Path.Combine(SyncPath, preparePath(entry.Directory));
                path = Path.Combine(path, entry.DisplayName);
                entry.Content = getContent(id);
                if (Log) Console.WriteLine("Create file {0}...", path);
                File.WriteAllBytes(path, entry.Content);
                if (Log) Console.WriteLine("done");
            }
        }

        public void Sync()
        {
            Sync("/");
        }

        public void Sync(string dir)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (Log) Console.WriteLine("Start Synchronization of {0}", dir);
            foreach (Entry entry in getNewestFiles(dir))
                create(entry.ID);
            stopwatch.Stop();
            if (Log)
            {
                Console.WriteLine("Synchronization finished");
                Console.WriteLine("Needed transactions: {0}", TransactionCount);
                Console.WriteLine("Needed time: {0}s", stopwatch.Elapsed.TotalSeconds);
            }
        }

        public List<Entry> getNewestFiles(String dir)
        {
            getFileHeads(dir);
            List<Entry> serverEntries = getFileIDsWithDisplaynamesAndFilenames(dir);
            List<Entry> newEntries = new List<Entry>();
            //List<Entry> localEntries = new List<Entry>();
            if (!Directory.Exists(getDirPath(dir)))
            {
                newEntries.AddRange(serverEntries);
                foreach (Entry entry in serverEntries)
                    if (entry.IsFolder())
                        newEntries.AddRange(getNewestFiles(entry.DisplayName));
            }
            else
            {
                foreach (Entry entry in serverEntries)
                {
                    string path = Path.Combine(SyncPath, entry.DisplayName);
                    if (entry.IsFolder())
                    {
                        newEntries.AddRange(getNewestFiles(entry.DisplayName));
                    }
                    else if (File.Exists(path))
                    {
                        //Überprüfe, ob lokale Datei aktuell ist
                        FileInfo fileInfo = new FileInfo(path);
                        if (fileInfo.CreationTime < entry.CreationDate)
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
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.getName.ToString());
            request.UserAgent = UserAgent;
            string content = request.Submit();
            TransactionCount++;
            return content;
        }

        public Entry getProperties(int id)
        {
            if (Log) Console.Write("Get properties of {0}...", id);
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("id", id.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.getPropertiesAsXML.ToString());
            request.UserAgent = UserAgent;
            request.Encoding = Encoding.UTF8;
            TransactionCount++;
            string content = request.Submit();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            XmlNode root = doc.DocumentElement;

            string fileName = root.Attributes["fileName"].Value;
            string displayName = root.Attributes["displayName"].Value;
            DateTime creationDate = DateTime.Parse(root.Attributes["creationDate"].Value);
            string hash = root.Attributes["hash"].Value;
            int sizeInByte = int.Parse(root.Attributes["sizeInByte"].Value);
            string userAgent = root.Attributes["userAgent"].Value;
            string directory = root.Attributes["directory"].Value;

            if (Log) Console.WriteLine("done");
            return new Entry(id, displayName, fileName, creationDate)
            {
                Hash = hash,
                SizeInByte = sizeInByte,
                UserAgent = userAgent,
                Directory = directory
            };
        }

        //public Entry getProperties(int id)
        //{
        //    HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
        //    request.Post.Add("id", id.ToString());
        //    request.Post.Add("key", ApiKey);
        //    request.Post.Add("method", Action.getProperties.ToString());
        //    request.UserAgent = UserAgent;
        //    request.Encoding = Encoding.UTF8;
        //    TransactionCount++;
        //    string content = request.Submit();

        //    List<string> splitted = parseCSV(content);
        //    string displayName = splitted[1];
        //    string fileName = splitted[0];
        //    DateTime creationDate = DateTime.Parse(splitted[4]);
        //    return new Entry(id, displayName, fileName, creationDate)
        //    {
        //        Hash = splitted[3],
        //        SizeInByte = int.Parse(splitted[5]),
        //        UserAgent = splitted[6],
        //        Directory = splitted[8],
        //        LocalHash = Common.getMD5(splitted[1])
        //    };
        //}

        //public Entry getEntryByID(string sid, bool download)
        //{
        //    if (download == true)
        //        Console.WriteLine("Starting Download of " + sid);
        //    HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
        //    request.Post.Add("id", sid);
        //    request.Post.Add("key", ApiKey);
        //    request.Post.Add("method", Action.getProperty.ToString());
        //    request.UserAgent = UserAgent;
        //    request.Encoding = Encoding.UTF8;
        //    TransactionCount++;
        //    string content = request.Submit();
        //    string[] entries = content.Split(';');
        //    int id = int.Parse(entries[0]);
        //    string displayName = entries[2];
        //    string fileName = entries[1];
        //    Entry entry = new Entry(id, displayName, fileName)
        //    {
        //        hash = entries[3],
        //        Uploaded = DateTime.Parse(entries[4]),
        //        SizeInByte = int.Parse(entries[5]),
        //        UsedClient = entries[6],
        //        Directory = entries[8],
        //        localHash = Common.getMD5(entries[1])
        //    };
        //    if (entry.DisplayName == entry.FileName)
        //    {
        //        if (download == true)
        //        {
        //            entry.FileName = Common.correctString(entry.FileName);
        //            entry.Directory = Common.correctString(entry.Directory);
        //            entry.DisplayName = Common.correctString(entry.DisplayName);
        //            if (System.IO.Directory.Exists("./Sync/" + entry.DisplayName) == false)
        //            {
        //                System.IO.Directory.CreateDirectory("./Sync/" + entry.DisplayName);
        //                Console.WriteLine("Got new dir: " + entry.DisplayName);
        //                new DirectoryInfo("./Sync/" + entry.DisplayName).CreationTime = entry.Uploaded;
        //            }
        //            else
        //            {
        //                Console.WriteLine("Dir already here: " + entry.DisplayName);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (download == true)
        //        {						
        //            entry.DisplayName = Common.correctString(entry.DisplayName);
        //            string syncPath = Environment.CurrentDirectory + "\\Sync" + entry.Directory.Replace("/", "\\") + entry.DisplayName;
        //            if (File.Exists(syncPath) == false)
        //            {
        //                entry.Content = getContent(entry.ID);
        //                System.IO.File.WriteAllBytes(syncPath, entry.Content);
        //                Console.WriteLine("Got new file: " + entry.DisplayName);
        //                try
        //                {
        //                    new FileInfo(syncPath).CreationTime = entry.Uploaded;
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine(ex.Message + ex.StkeyTrace);
        //                }
        //            }
        //            else
        //            {
        //                if (entry.SizeInByte != new System.IO.FileInfo(syncPath).Length)
        //                {
        //                    Console.WriteLine("file would be resynced" + entry.DisplayName);
        //                }
        //                else
        //                {
        //                    Console.WriteLine("File already synced: " + entry.DisplayName);
        //                }
        //            }
        //        }
        //    }
        //    return entry;
        //}

        Byte[] getContent(int id)
        {
            if (Log) Console.Write("get content of {0}...", id);
            WebClient client = new WebClient();
            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            client.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);
            byte[] result = client.UploadData(this.ApiUri.ToString(), "POST", System.Text.Encoding.UTF8.GetBytes("id=" + id.ToString() + "&key=" + ApiKey + "&method=" + Action.getContent.ToString()));
            TransactionCount++;
            if (Log) Console.WriteLine("done");
            return result;
        }

        public bool uploadFile(string Path, string currentdir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.uploadFile.ToString());
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
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.renameFile.ToString());
            request.Post.Add("file", hash);
            request.Post.Add("newname", newName);
            request.UserAgent = UserAgent;
            request.Encoding = System.Text.Encoding.UTF8;
            request.Post.Add("currentdir", currentdir);
            string result = request.Submit();
            return result == "false";
        }

        public bool renameFolder(string source, string newName, string old_root, string currentdir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.renameFolder.ToString());
            request.Post.Add("newname", newName);
            request.Post.Add("source", source);
            request.Post.Add("old_root", old_root);
            request.Post.Add("currentdir", currentdir);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            return result == "false";
        }

        public bool copyFile(string target, string hash)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.copy.ToString());
            request.Post.Add("file", hash);
            request.UserAgent = UserAgent;
            request.Post.Add("dir", target);
            string result = request.Submit();
            return result == "false";
        }

        public bool copyFolder(string source, string target, string old_root)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.copy.ToString());
            request.Post.Add("source", source);
            request.Post.Add("target", target);
            request.Post.Add("old_root", old_root);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            return result == "false";
        }

        public bool moveFile(string target, string hash)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.move.ToString());
            request.Post.Add("file", hash);
            request.UserAgent = UserAgent;
            request.Post.Add("dir", target);
            string result = request.Submit();
            return result == "false";
        }

        public bool moveFolder(string source, string target, string old_root)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.move.ToString());
            request.Post.Add("source", source);
            request.Post.Add("target", target);
            request.Post.Add("old_root", old_root);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            return result == "false";
        }

        public bool exists(string entry, string dir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.exists.ToString());
            request.Post.Add("entry", entry);
            request.Post.Add("dir", dir);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            return result == "false";
        }

        public bool createDir(string entry, string dir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.createDir.ToString());
            request.Post.Add("entry", entry);
            request.Post.Add("dir", dir);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            return result == "false";
        }

        public string getHash(string file, string dir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.getHash.ToString());
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
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.deleteFile.ToString());
            request.Post.Add("s", "true");
            request.Post.Add("file", hash);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            return result == "false";
        }

        public bool deleteFolder(string dir)
        {
            HTTPPostRequest request = new HTTPPostRequest(this.ApiUri.ToString());
            request.Post.Add("key", ApiKey);
            request.Post.Add("method", Action.deleteFolder.ToString());
            request.Post.Add("s", "true");
            request.Post.Add("dir", dir);
            request.UserAgent = UserAgent;
            string result = request.Submit();
            return result == "false";
        }
    }
}