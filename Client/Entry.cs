/*
 * Erstellt mit SharpDevelop.
 * Benutzer: christoph
 * Datum: 09.05.2013
 * Zeit: 15:48
 * 
 * Sie können diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader ändern.
 */
using System;

namespace Client
{
	/// <summary>
	/// Description of Entry.
	/// </summary>
	public class Entry
	{
		public String DisplayName {get;set;}
		public String ShortDisplayName {get;set;}
		public String FileName {get;set;}
		public DateTime Uploaded {get;set;}
		public String UsedClient {get;set;}
		public int Size {get;set;}
		public bool isFolder {get;set;}
		public string hash {get;set;}
		public string localHash;
		public string Directory {get;set;}
		public int ID {get;set;}
		public Byte[] Content {get;set;}
	}
}
