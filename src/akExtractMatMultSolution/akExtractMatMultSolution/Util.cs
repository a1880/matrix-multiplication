using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace akExtractMatMultSolution
{
    /// <summary>
    /// Boiler-plate collection of commonly used routines
    /// </summary>
    public class Util
    {
        public static StreamWriter fOut = null;

        public static void Assert(bool condition)
        {
            Check(condition, "assertion");
        }

#pragma warning disable IDE1006 // Naming Styles
        public static float atof(string s)
#pragma warning restore IDE1006 // Naming Styles
        {
            CheckNull(s);
            if (!float.TryParse(s, out float result))
            {
                throw new ArgumentException($"Cannot read '{s}' as float");
            }

            return result;
        }

#pragma warning disable IDE1006 // Naming Styles
        public static int atoi(string s)
#pragma warning restore IDE1006 // Naming Styles
        {
            CheckNull(s);
            if (!int.TryParse(s, out int i))
            {
                throw new ArgumentException($"Cannot read '{s}' as integer");
            }

            return i;
        }

        public static string BuildDate()
        {
            string s = Properties.Resources.BuildDate.Substring(0, 10);

            int dd = ExtractInt(s, 0, 2);
            int mm = ExtractInt(s, 3, 2);
            int yyyy = ExtractInt(s, 6, 4);

            DateTime dt = new(yyyy, mm, dd);
                    
            s = dt.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);

            return s;
        }

        public static void Check(bool cond, string format, params object[] args)
        {
            if (!cond)
            {
                o("Condition violated:");
                o(string.Format(format, args));
                Finish(1);
            }
        }

        public static void CheckFile(string fileName, string purpose)
        {
            Check(!string.IsNullOrEmpty(fileName), $"Empty filename for {purpose} file");

            if (!File.Exists(fileName))
            {
                Fatal($"{purpose} file '{fileName}' not found");
            }
        }

        public static void CheckNull(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("Null argument not allowed!");
            }
        }

        public static int ExtractInt(string s, int offset, int length)
        {
            CheckNull(s);
            if (offset + length > s.Length)
            {
                throw new ArgumentException($"ExtactInt: access to '{s}' out of bounds!");
            }
            return atoi(s.Substring(offset, length));
        }

        public static void Finish(int code)
        {
            o("");
            o("ciao!");
            o("");

            if (fOut != null)
            {
                fOut.Flush();
                fOut.Close();
            }
            else
            {
                Console.Out.Flush(); ;
            }

            Environment.Exit(code);
        }

        public static void Fatal(string msg)
        {
            CheckNull(msg);
            o("Fatal error:");
            o(msg);
            Finish(1);
        }

        public static bool IsNumeric(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            foreach (char c in s)
            {
                if ((c < '0') || (c > '9'))
                {
                    return false;
                }
            }

            return true;
        }

#pragma warning disable IDE1006 // Naming Styles
        public static void o(string s = "")
#pragma warning restore IDE1006 // Naming Styles
        {
            CheckNull(s);
            if (fOut != null)
            {
                fOut.WriteLine(s);
            }
            else
            {
                Console.WriteLine(s);
            }
            Debug.WriteLine(s);
        }

        public static StreamReader OpenReader(string fileName, string purpose)
        {
            CheckFile(fileName, purpose);
            o($"Reading {purpose} file '{fileName}'");

            StreamReader sr = new(fileName);

            return sr;
        }
        public static StreamWriter OpenWriter(string fileName, string purpose)
        {
            if ((fileName == "") || (fileName == "<stdout>"))
            {
                o($"Writing {purpose} to standard output");
                return (StreamWriter)Console.Out;
            }

            o($"Writing {purpose} file '{fileName}'");

            StreamWriter sw = new(fileName);

            return sw;
        }

        /// <summary>
        /// Return number n with thousands separator(s)
        /// </summary>
        public static string PrettyNum(long n)
        {
            if (n < 0)
            {
                return $"-{PrettyNum(-n)}";
            }

            if (n < 1000)
            {
                return $"{n}";
            }

            return $"{PrettyNum(n / 1000)},{(n % 1000).ToString().PadLeft(3, '0')}";
        }

        /// <summary>
        /// Return int array [start, start+1, ... start+count-1]
        /// </summary>
        public static int[] Range(int start, int count)
        {
            return [.. Enumerable.Range(start, count)];
        }

        /// <summary>
        /// Return current date and time as string
        /// </summary>
        public static string Timestamp()
        {
            return DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
        }

        private static readonly char[] SpaceSeparator = [' '];

        public static string[] Tokenize(string s, char[] separators = null)
        {
            if (s == null)
            {
                throw new ArgumentNullException("string s");
            }

            return s.Split(separators ?? SpaceSeparator, 
                           StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
