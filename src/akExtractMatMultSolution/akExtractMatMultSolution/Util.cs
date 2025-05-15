using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace akExtractMatMultSolution
{
    public class Util
    {
        public static StreamWriter fOut = null;

        public static bool ArraysEqual(string[] arr1, string[] arr2)
        {
            return
            ReferenceEquals(arr1, arr2) || (
                arr1 != null && arr2 != null &&
                arr1.Length == arr2.Length &&
                arr1.Select((a, i) => arr2[i].Equals(a)).All(i => i)
            );
        }

        public static void Assert(bool condition)
        {
            Check(condition, "assertion");
        }

#pragma warning disable IDE1006 // Naming Styles
        public static int atoi(string s)
#pragma warning restore IDE1006 // Naming Styles
        {
            int i = 0;

            try
            {
                i = int.Parse(s);
            }
            catch (Exception ex)
            {
                Fatal($"Cannot read '{s}' as integer: {ex.Message}");
            }

            return i;
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
        /// Return int array [start, start+1, ... start+count-1]
        /// </summary>
        public static int[] Range(int start, int count)
        {
            return Enumerable.Range(start, count).ToArray();
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
