/*
 * Erstellt mit SharpDevelop.
 * Benutzer: christoph
 * Datum: 28.09.2013
 * Zeit: 12:30
 * 
 * Sie können diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader ändern.
 */
using System;
using HttpPostRequestLib.Net;
using System.Web;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
namespace Client
{
	/// <summary>
	/// Description of ClientClass.
	/// </summary>
	public class ClientClass
	{
		public enum Status{
			Syncing,
			Error,
			Idle			
		}
		public enum Action{
			getFiles,		
			getProperty,
			getContent,	
			getName,
			getVersion,
			uploadFile,
			renameFile,
			renameFolder,
			copy,
			move,
			getHash,
			exists,
			createDir,
			deleteFile,
			deleteFolder,
		}
		public String Token {get; private set;}
		public Uri Instance {get;private set;}	
		public String UserAgent  {get;private set;}
		public int TransactionCount {get; set;}
		public bool isBusy {get; set;}
		public ClientClass(String token, String url)
		{
			this.Token = token;
			this.Instance = new Uri(url);
			UserAgent = "Client";
		}
		public bool acknoledge()
		{			
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());
			request.Post.Add("ACK",Token);
			request.Post.Add("ACKONLY","true");
			request.UserAgent = UserAgent;
			TransactionCount++;
			string content = request.Submit();
			isBusy = false;
			if (bool.Parse(content.Replace("ACK:","")) == true)
				return true;
			else
				return false;
		}
		public string getVersion()
		{
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());			
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.getVersion.ToString());
			request.UserAgent = UserAgent;		
			TransactionCount++;		
			string result =  request.Submit();	
			isBusy = false;			
			return	result;
		}
		public List<Entry> getFiles(String dir,bool download)
		{
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());
			request.Post.Add("dir",dir);
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.getFiles.ToString());
			request.UserAgent = UserAgent;
			TransactionCount++;
			string content = request.Submit();				
			try{
				content = content.Replace("LIST:","");		
				if (content != ""){
					content = content.Substring(0,content.Length -1);			
					string[] entries =  content.Split(';');
					List<Entry> items = new List<Entry>();
					foreach (string element in entries) {
						if (File.Exists("./Sync/"+ getNameByID(element)) == false){
						Entry newEntry = getEntryByID(element,download);
						if (newEntry != null)
							try{
								if (newEntry.DisplayName == newEntry.FileName)
									items.AddRange(getFiles(newEntry.DisplayName,download));
								else							
									items.Add(newEntry);
							}catch{
								Console.WriteLine("Entry failed: " +element);
							}
						}
						else
							Console.WriteLine("Skipping " + element);	
					}
					isBusy = !true;
					return items;
				}
			}
			catch (Exception ex){
				Console.WriteLine(ex.Message + ex.StackTrace);
			}	
			isBusy = !true;			
			return new List<Entry>();
		}
		public string getNameByID(string id){
			isBusy = true;			
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());
			request.Post.Add("id",id);
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.getName.ToString());
			request.UserAgent = UserAgent;
			string content = request.Submit();
			TransactionCount++;
			isBusy = false;
			return content;
		}
		public Entry getEntryByID(string id,bool download)
		{
			isBusy = true;
			if (download == true)
				Console.WriteLine("Starting Download of " + id);
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());
			request.Post.Add("id",id);
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.getProperty.ToString());
			request.UserAgent = UserAgent;
			request.Encoding  = Encoding.UTF8;
			TransactionCount++;
			string content = request.Submit();
			content = content.Replace("LIST:","");
			//content = content.Substring(0,content.Length -1);
			string[] entries =  content.Split(';');
			Entry toreturn = new Entry();
			toreturn.ID = int.Parse(entries[0]);
			toreturn.DisplayName = entries[2];
			toreturn.FileName = entries[1];
			toreturn.hash = entries[4];			
			
			string regex_pattern = @"\w{3}.(?<month>\w{3}).(?<date>\d{1,2}).(?<time>\d{1,2}:\d{1,2}:\d{1,2}).*(?<year>\d{4})";
			System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(regex_pattern);
			
			string date = r.Match(entries[5]).Groups["date"].Value + "." + r.Match(entries[5]).Groups["month"].Value + " " + r.Match(entries[5]).Groups["year"].Value + " " + r.Match(entries[5]).Groups["time"].Value;
			toreturn.Uploaded =  DateTime.Parse(date);
			toreturn.Size = int.Parse(entries[6]);
			toreturn.UsedClient = entries[7];	
			toreturn.Directory = entries[9];
			toreturn.localHash = Common.getMD5(toreturn.DisplayName);
			if (toreturn.FileName == toreturn.DisplayName)
				toreturn.isFolder = true;
			
			
			if (toreturn.DisplayName == toreturn.FileName){
				if (download == true){
					toreturn.FileName = Common.correctstring(toreturn.FileName);						
					//toreturn.ShortDisplayName = Common.correctstring(toreturn.ShortDisplayName);
					toreturn.Directory = Common.correctstring(toreturn.Directory);						
					toreturn.DisplayName = Common.correctstring(toreturn.DisplayName);
					if (System.IO.Directory.Exists("./Sync/" + toreturn.DisplayName) == false){							
						System.IO.Directory.CreateDirectory("./Sync/" + toreturn.DisplayName);			
						Console.WriteLine("Got new dir: " + toreturn.DisplayName);
						new DirectoryInfo("./Sync/" + toreturn.DisplayName).CreationTime = toreturn.Uploaded;
					}
					else{						
						Console.WriteLine("Dir already here: " + toreturn.DisplayName);
					}
				}
			}else{	
				if (download == true){
					//toreturn.Directory = Common.correctstring(toreturn.Directory);						
					toreturn.DisplayName = Common.correctstring(toreturn.DisplayName);
					string syncPath = Environment.CurrentDirectory + "\\Sync" + toreturn.Directory.Replace("/","\\") + toreturn.DisplayName;				
					if (File.Exists(syncPath) == false){
						toreturn.Content = getContent(toreturn.ID);					
						System.IO.File.WriteAllBytes(syncPath,toreturn.Content);
						Console.WriteLine("Got new file: " + toreturn.DisplayName);
						try{
							new FileInfo(syncPath).CreationTime = toreturn.Uploaded;	
						}
						catch (Exception ex){
							Console.WriteLine(ex.Message + ex.StackTrace);
						}
					}
					else{
						if (toreturn.Size != new System.IO.FileInfo(syncPath).Length){
							Console.WriteLine("file would be resynced" + toreturn.DisplayName);
						}	
						else{
							Console.WriteLine("File already synced: " + toreturn.DisplayName);
						}
					}
				}
			}
			isBusy = false;
			return toreturn;
		}
		Byte[] getContent(int id)
		{		
			isBusy = true;
			WebClient client = new WebClient();
			client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
			client.Headers.Add(HttpRequestHeader.UserAgent,UserAgent);
			byte []result=client.UploadData(this.Instance.ToString(), "POST",System.Text.Encoding.UTF8.GetBytes("id="+id.ToString()+"&ACK="+Token+"&Method="+Action.getContent.ToString()));
			TransactionCount++;
			isBusy = false;
			return result;
		}
		
		public bool uploadFile(string Path, string currentdir){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.uploadFile.ToString());
			request.Post.Add("currentdir",currentdir);
			request.Files.Add("userfile[]",Path);			
			request.UserAgent = UserAgent;
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}
		public bool renameFile(string hash, string newName,string currentdir){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.renameFile.ToString());
			request.Post.Add("file",hash);
			request.Post.Add("newname",newName);			
			request.UserAgent = UserAgent;
			request.Encoding = System.Text.Encoding.UTF8;
			request.Post.Add("currentdir",currentdir);
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}
		public bool renameFolder(string source, string newName,string old_root,string currentdir){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.renameFolder.ToString());
			request.Post.Add("newname",newName);			
			request.Post.Add("source",source);			
			request.Post.Add("old_root",old_root);
			request.Post.Add("currentdir",currentdir);
			request.UserAgent = UserAgent;
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}
		public bool copyFile(string target, string hash){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.copy.ToString());
			request.Post.Add("file",hash);			
			request.UserAgent = UserAgent;
			request.Post.Add("dir",target);
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}
		public bool copyFolder(string source, string target,string old_root){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.copy.ToString());
			request.Post.Add("source",source);				
			request.Post.Add("target",target);
			request.Post.Add("old_root",old_root);
			request.UserAgent = UserAgent;
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}
		public bool moveFile(string target, string hash){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.move.ToString());
			request.Post.Add("file",hash);			
			request.UserAgent = UserAgent;
			request.Post.Add("dir",target);
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}
		public bool moveFolder(string source, string target,string old_root){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.move.ToString());
			request.Post.Add("source",source);				
			request.Post.Add("target",target);
			request.Post.Add("old_root",old_root);
			request.UserAgent = UserAgent;
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}
		public bool exists(string entry, string dir){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.exists.ToString());
			request.Post.Add("entry",entry);				
			request.Post.Add("dir",dir);
			request.UserAgent = UserAgent;
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}
		public bool createDir(string entry, string dir){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.createDir.ToString());
			request.Post.Add("entry",entry);				
			request.Post.Add("dir",dir);
			request.UserAgent = UserAgent;
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}		
		public string getHash(string file, string dir){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.getHash.ToString());
			request.Post.Add("file",file);
			request.Post.Add("dir",dir);			
			request.UserAgent = UserAgent;
			//request.Encoding = Encoding.UTF8;
			string result = request.Submit();
			isBusy = false;
			return result;
		}
		public bool deleteFile(string hash){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.deleteFile.ToString());
			request.Post.Add("s","true");				
			request.Post.Add("file",hash);
			request.UserAgent = UserAgent;
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}
		public bool deleteFolder(string dir){
			isBusy = true;
			HTTPPostRequest request = new HTTPPostRequest(this.Instance.ToString());		
			request.Post.Add("ACK",Token);
			request.Post.Add("Method",Action.deleteFolder.ToString());
			request.Post.Add("s","true");				
			request.Post.Add("dir",dir);
			request.UserAgent = UserAgent;
			string result = request.Submit();
			isBusy = false;
			if (result == "false")
				return false;
			else	
				return true;
		}
	}
}
