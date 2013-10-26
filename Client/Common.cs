using System;
using System.Text;

namespace RedundancyClient
{
	public class Common
	{
		public static string getMD5(string input)
		{
		    //Umwandlung des Eingabestrings in den MD5 Hash
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

        /// <summary>
        /// korrigiert die Zeichenkodierung
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
		public static string correctString(string input){
			System.Text.Encoding iso = System.Text.Encoding.GetEncoding("ISO-8859-1");			
			System.Text.Encoding utf8 = System.Text.Encoding.UTF8;			
			byte[] utfBytes = utf8.GetBytes(input);			
			byte[] isoBytes = System.Text.Encoding.Convert(utf8, iso, utfBytes);			
			System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();			
			return encoding.GetString(isoBytes);
		}

        public static string hiddenReadLine()
        {
            string input = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    input += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                    {
                        input = input.Substring(0, (input.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            }
            // Stops Receving Keys Once Enter is Pressed
            while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return input;
        }
	}
}
