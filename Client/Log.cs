using System;

namespace RedundancyClient
{
    public class Log
    {
        #region properties
        public ConsoleColor exceptionColor { get; set; }
        public ConsoleColor defaultColor { get; set; }
        public bool DoLog { get; set; }
        #endregion

        #region constructors
        public Log()
        {
            exceptionColor = ConsoleColor.Red;
            defaultColor = ConsoleColor.Gray;
        }
        #endregion

        #region methods
        public void Write(string text)
        {
            if(DoLog)
                Console.Write(text);
        }

        public void WriteLine(string text)
        {
            if(DoLog)
                Console.WriteLine(text);
        }

        public void WriteException(string text)
        {
            if (DoLog)
            {
                Console.ForegroundColor = exceptionColor;
                Console.Write(text);
                Console.ForegroundColor = defaultColor;
            }
        }

        public void WriteLineException(string text)
        {
            if (DoLog)
            {
                Console.ForegroundColor = exceptionColor;
                Console.WriteLine(text);
                Console.ForegroundColor = defaultColor;
            }
        }
        #endregion
    }
}