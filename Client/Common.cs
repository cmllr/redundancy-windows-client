/*
 * Erstellt mit SharpDevelop.
 * Benutzer: christoph
 * Datum: 05.10.2013
 * Zeit: 13:01
 * 
 * Sie können diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader ändern.
 */
using System;
using System.Text;
namespace Client
{
	/// <summary>
	/// Description of Common.
	/// </summary>
	public class Common
	{
		/// <summary>
		/// Liefert den MD5 Hash 
		/// </summary>
		/// <param name="input">Eingabestring</param>
		/// <returns>MD5 Hash der Eingabestrings</returns>
		public static string getMD5(string input)
		{
		    //Umwandlung des Eingastring in den MD5 Hash
		    System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
		    byte[] textToHash = Encoding.Default.GetBytes(input);
		    byte[] result = md5.ComputeHash(textToHash);
		 
		    //MD5 Hash in String konvertieren
		    System.Text.StringBuilder s = new System.Text.StringBuilder();
		    foreach (byte b in result)
		    {
		        s.Append(b.ToString("x2").ToLower());
		    }
		 
		    return s.ToString();
		}
		public static string correctstring(string input){
			System.Text.Encoding iso = System.Text.Encoding.GetEncoding("ISO-8859-1");			
			System.Text.Encoding utf8 = System.Text.Encoding.UTF8;			
			byte[] utfBytes = utf8.GetBytes(input);			
			byte[] isoBytes = System.Text.Encoding.Convert(utf8, iso, utfBytes);			
			System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();			
			return encoding.GetString(isoBytes);
		}
	}
}
