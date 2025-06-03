using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Threading;     //  for Process suspend
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace akUtil
{
    public class Util
    {
        public static bool quiet;                //  flag to shut off o() output
        public static StreamWriter fOut = null;  //  the output file for o()
        public static int debugLevel = 2;

        public static readonly bool IsConsoleApp = (Console.In != StreamReader.Null);

        //  colon ':' is used as column separator for o("..") output
        //  if ignoreColon is true, no tabular output is created
        private static readonly bool ignoreColon = false;

        private static StreamWriter shellOut;    //  redirected output for shell()
        private static WriteLineCallBack shellStdoutCallBack = null;
        private static WriteLineCallBack shellStderrCallBack = null;
        private static volatile Process shelledProcess;   // = null;
        private static volatile bool bShellNullReceived;

        private static string projectName = "";

        /// <summary>
        /// Append text of fileName2 to file fileName1
        /// </summary>
        /// <param name="fileName1"></param>
        /// <param name="fileName2"></param>
        public static void AppendFile(string fileName1, string fileName2)
        {
            try
            {
                string text = "";

                if (Exists(fileName2))
                {
                    text = File.ReadAllText(fileName2);
                }
                else
                {
                    Raise($"File to append not found: {fileName2}");
                }

                if (text.Trim() == "")
                {
                    return;
                }

                if (!Exists(fileName1))
                {
                    File.WriteAllText(fileName1, text);
                }
                else
                {
                    File.AppendAllText(fileName1, text);
                }
            }
            catch (Exception ex)
            {
                Fatal($"Could not append '{fileName2}' to file '{fileName1}': {ex.Message}");
            }
        }

        /// <summary>
        /// Convert string to bool
        /// </summary>
        /// <param name="a">The string</param>
        /// <returns>The resulting bool</returns>
#pragma warning disable IDE1006 // Naming Styles
        public static bool atob(string a)
#pragma warning restore IDE1006 // Naming Styles
        {
            return "true;yes;on;1".Contains(a.Trim().ToLower());
        }

        /// <summary>
        /// Convert string to double
        /// handle comma like period
        /// </summary>
        /// <param name="a">The string</param>
        /// <returns>The resulting double</returns>
#pragma warning disable IDE1006 // Naming Styles
        public static double atod(string a)
#pragma warning restore IDE1006 // Naming Styles
        {
            if (a.EndsWith("%"))
            {
                return 0.01 * atod(a.Substring(0, a.Length - 1));
            }
            if (double.TryParse(a.Replace(".", ","), out double result))
            {
                return result;
            }

            return double.NaN;
        }

        /// <summary>
        /// Convert string to float
        /// handle comma like period
        /// </summary>
        /// <param name="a">The string</param>
        /// <returns>The resulting float</returns>
#pragma warning disable IDE1006 // Naming Styles
        public static float atof(string a)
#pragma warning restore IDE1006 // Naming Styles
        {
            return (float)atod(a);
        }

        /// <summary>
        /// Convert double 'x' to internationally formatted string
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        public static string ftoa(double x)
#pragma warning restore IDE1006 // Naming Styles
        {
            return x.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convert string to int
        /// </summary>
        /// <param name="s">The string</param>
        /// <returns>The int</returns>
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


        /// <summary>
        /// Check assertion and trigger fatal error if condition is not true
        /// </summary>
        /// <param name="cond">The condition</param>
        [Conditional("DEBUG")]
        public static void Assert(bool condition,
                    [CallerLineNumber] int lineNumber = 0,
                    [CallerMemberName] string caller = null)
        {
            if (!condition)
            {
                if (lineNumber != 0)
                {
                    Check(condition, "Assertion violated");
                }
                else
                {
                    Check(condition, $"Assertion violated at line {lineNumber} ({caller})");
                }
            }
        }

        /// <summary>
        /// Surround str1, str2, and str2 with a box
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <param name="str3"></param>
        /// <returns></returns>
        public static string BoxPrint(string str1, string str2, string str3)
        {
            const int width = 79;
            const string nl = "\r\n";
            int len1 = str1.Length;
            int len2 = str2.Length;
            int len3 = str3.Length;
            int row;
            int col;
            int from1 = (width - len1) / 2 + 1;
            int to1 = from1 + len1 - 1;
            int from2 = (width - len2) / 2 + 1;
            int to2 = from2 + len2 - 1;
            int from3 = (width - len3) / 2 + 1;
            int to3 = from3 + len3 - 1;
            string s = nl;

            for (row = 1; row <= 7; row++, s += nl)
                for (col = 1; col <= width; col++)
                    switch (row)
                    {
                        case 1:
                            s += ((col == 1) || (col == width - 1)) ? '[' :
                                 ((col == 2) || (col == width)) ? ']' :
                                 '-';
                            break;
                        case 2:
                            s += ((col == 1) || (col == width)) ? '|' :
                                 ' ';
                            break;
                        case 3:
                            s += ((col == 1) || (col == width)) ? '|' :
                                 ((col >= from1) && (col <= to1)) ? str1[col - from1] :
                                 ' ';
                            break;
                        case 4:
                            s += ((col == 1) || (col == width)) ? '|' :
                                 ((col >= from2) && (col <= to2)) ? str2[col - from2] :
                                 ' ';
                            break;
                        case 5:
                            s += (col == 1) || (col == width) ? '|' :
                                 (col >= from3) && (col <= to3) ? str3[col - from3] :
                                 ' ';
                            break;
                        case 6:
                            goto case 2;
                        case 7:
                            goto case 1;
                    }

            //  add "ignore colon" prefix
            return "~:" + s;
        }

        /// <summary>
        /// Check the condition if abort if false
        /// </summary>
        /// <param name="condition">condtion to check</param>
        /// <param name="format">fatal error message format</param>
        /// <param name="args">fatal error message arguments</param>
        [Conditional("DEBUG")]
        public static void Check(bool condition, string format, params object[] args)
        {
            if (!condition)
            {
                Fatal(format, args);
            }
        }

        /// <summary>
        /// Separate string into sub-strings with maximum length 
        /// </summary>
        /// <param name="s">The string to separate</param>
        /// <param name="maxLen">Maximum length of sub-strings</param>
        /// <param name="indent">Number of blanks for 2nd and following lines to be indented</param>
        /// <returns>Array of strings (at least one element)</returns>
        public static string[] ChopReflow(string s, int maxLen = 72, int indent = 0)
        {
            Check(s != null, "can't chop null string");

            if (s.Length <= maxLen)
            {
                //  return new string[] { s.TrimEnd() };
                return Concat<string>(s.TrimEnd());
            }
            else
            {
                string margin = "".PadRight(indent);

                for (int pos = maxLen-1; pos > 0; pos--)
                {
                    char c = s[pos];

                    if (" \t\r\n".IndexOf(c) >= 0)
                    {
                        string[] t = [s.Substring(0, pos)];
                        string[] a = ChopReflow(margin + s.Substring(pos + 1), maxLen, indent);

                        //  return t.Concat(a).ToArray();
                        return Concat<string>(t, a);
                    }
                    else if ("-".IndexOf(c) >= 0)
                    {
                        string[] t = [s.Substring(0, pos + 1)];
                        string[] a = ChopReflow(margin + s.Substring(pos + 1), maxLen, indent);

                        //  return t.Concat(a).ToArray();
                        return Concat<string>(t, a);
                    }
                }

                string[] tt = [s.Substring(0, maxLen).TrimEnd()];
                string[] aa = ChopReflow(margin + s.Substring(maxLen + 1), maxLen, indent);

                //  return tt.Concat(aa).ToArray();
                return Concat<string>(tt, aa);
            }
        }

        public static void OutputReflowed(string s, int maxLen = 72, int indent = 0)
        {
            string[] a = ChopReflow(s, maxLen, indent);

            foreach (string ss in a)
            {
                Console.WriteLine(ss);
            }
        }

        /// <summary>
        /// Concatenate a variable number of elements and arrays
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="args">variable list of element and array parameters</param>
        /// <returns>The concatenated array; might be empty</returns>
        public static T[] Concat<T>(params object[] args)
        {
            Type elementType = typeof(T);
            Type arrayType = typeof(T[]);
            int len = 0;
            int pos = 0;

            foreach (object obj in args)
            {
                Type objType = obj.GetType();

                if (objType == elementType)
                {
                    len += 1; 
                }
                else if (objType == arrayType)
                {
                    len += (obj as T[]).Length;
                }
                else
                {
                    Raise("invalid type in concat");
                }
            }

            T[] ra = new T[len];

            foreach (object obj in args)
            {
                Type objType = obj.GetType();

                if (objType == elementType)
                {
                    ra[pos] = (T)obj;
                    pos += 1;
                }
                else if (objType == arrayType)
                {
                    T[] arr = (T[])obj;
                    Array.Copy(arr, 0, ra, pos, arr.Length);
                    pos += arr.Length;
                }
            }

            return ra;
        }

        /// <summary>
        /// Return date in dd.MM.yyyy format as string
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string Date(DateTime dt)
        {
            return dt.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Return number as EUR currency string
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string EUR(double x)
        {
            return $"{x:#,##0.00} €";
        }

        /// <summary>
        /// Open file with notepad editor
        /// </summary>
        /// <param name="fileName">The file</param>
        public static void Edit(string fileName)
        {
            if (!Exists(fileName))
            {
                Raise("Cannot edit! File " + Quote(fileName) + " does not exist!");
            }

            Shell("notepad.exe", fileName, false);
        }

        /// <summary>
        /// Check if file exists
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool Exists(string fileName)
        {
            return File.Exists(Unquote(fileName));
        }

        public static int ExtractInt(string s, int offset, int length)
        {
            Check(s != null, "ExtractInt: null string");
            Check((offset >= 0) && (offset < s.Length), "ExtractInt: invalid offset");
            Check(offset + length <= s.Length, "ExtractInt: invalid length");
            s = s.Substring(offset, length);
            return atoi(s);
        }

        /// <summary>
        /// Issue fatal error message and fiish the program
        /// </summary>
        /// <param name="format">message format</param>
        /// <param name="args">message arguments</param>
        public static void Fatal(string format, params object[] args)
        {
            quiet = false;
            o_ignore_colon("Fatal error:");
            o_ignore_colon("!" + format, args);

            if (IsConsoleApp)
            {
                o("!<press ESC to continue>");

                while (true)
                {
                    try
                    {
                        ConsoleKeyInfo ki = Console.ReadKey();

                        if (ki.Key == ConsoleKey.Escape)
                        {
                            break;
                        }
                        o("!<press ESC to continue>");
                    }
                    catch(Exception)
                    {
                        break;
                    }
                }
                o("");
                Finish(1);
            }
        }

        /// <summary>
        /// Expands environment variables and, if unqualified, locates the exe in the working directory
        /// or the evironment's path.
        /// </summary>
        /// <param name="exe">The name of the executable file</param>
        /// <returns>The fully-qualified path to the file</returns>
        /// <exception cref="FileNotFoundException">Raised when the exe was not found</exception>
        public static string FindExePath(string exe)
        {
            exe = Environment.ExpandEnvironmentVariables(exe);
            if (!File.Exists(exe))
            {
                if (Path.GetDirectoryName(exe) == string.Empty)
                {
                    foreach (string test in ((";" + Environment.GetEnvironmentVariable("PATH")) ?? "").Split(';'))
                    {
                        string path = test.Trim();
                        if (!string.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exe)))
                        {
                            return Path.GetFullPath(path);
                        }
                    }
                }
                throw new FileNotFoundException(new FileNotFoundException().Message, exe);
            }
            return Path.GetFullPath(exe);
        }

        /// <summary>
        /// End the current program and return code to caller
        /// </summary>
        /// <param name="returnCode"></param>
        public static void Finish(int returnCode)
        {
            o2("");
            o2("Finished: " + Timestamp());
            o2("");

            Console.Out.Flush();
            fOut?.Close();

            if (IsConsoleApp)
            {
                Environment.Exit(returnCode);
            }
        }

        /// <summary>
        /// Return the build date
        /// </summary>
        /// <returns></returns>
        public static string GetBuild()
        {
            DateTime dt = new(2000, 1, 1);
            Assembly asy = Assembly.GetExecutingAssembly();
            //  requires [assembly: AssemblyVersion("1.0.*")] in Assembly
            int build = asy.GetName().Version.Build;
            string ret = dt.AddDays(build).ToString("dd-MMM-yyyy", DateTimeFormatInfo.InvariantInfo);

            return ret;
        }

        /// <summary>
        /// ProjectName is used to personalize names of temporary files
        /// (cf. GetTempFile())
        /// </summary>
        /// <param name="name"></param>
        public static void SetProjectName(string name)
        {
            projectName = name;
        }

        public static string GetProjectName()
        {
            return projectName;
        }

        public static string GetApplicationPath()
        {
            string path = Assembly.GetEntryAssembly().Location;

            return path;
        }

        public static double GetPhysicalMemoryMegabytes()
        {
            var info = new Microsoft.VisualBasic.Devices.ComputerInfo();
            double mb = (double)(info.TotalPhysicalMemory / (1024.0 * 1024.0));

            return mb;
        }

        /// <summary>
        /// Create random name for a temporary file without actually creating the file.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string GetTempFile(string extension = "")
        {
            string pre = projectName.Equals("") ? "" : (projectName + ".");

            return Path.Combine(Path.GetTempPath(), pre + Path.GetRandomFileName() + extension);
        }

        /// <summary>
        /// Return val1 iff cond==true
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cond">condition</param>
        /// <param name="val1"></param>
        /// <param name="val2"></param>
        /// <returns>val1 or val2 depending on condition</returns>
        public static T Iff<T>(bool cond, T val1, T val2)
        {
            return cond ? val1 : val2;
        }


        /// <summary>
        /// Return true, iff compiled in DEBUG mode
        /// </summary>
        /// <returns>false, iff RELEASE mode</returns>
        public static bool IsInDebugMode()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Check if string purely consists of digits 0..9
        /// Empty string is taken as non-numeric
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNumeric(string s)
        {
            bool ret = true;

            Assert(s != null);

            foreach (char c in s)
            {
                if ((c < '0') || (c > '9'))
                {
                    ret = false;
                    break;
                }
            }

            return ret & (s.Length > 0);
        }

        /// <summary>
        /// Output string without trating contained ':' characaters as column separator
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        public static void o_ignore_colon(string format = "", params object[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            o("~:" + format, args);
        }

        /// <summary>
        /// Output string to console and output file
        /// </summary>
        /// <param name="format">message format</param>
        /// <param name="args">message arguments</param>
#pragma warning disable IDE1006 // Naming Styles
        public static void o(string format = "", params object[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            string s;
            try
            {
                s = (args.Length == 0) ? format : format.StringFormat(args);
            }
            catch(Exception e) 
            { 
                Console.WriteLine("Offending format '{0}'", format);
                throw e;
            }

            bool flushIt = false;
            bool noNewline = false;
            bool treatColon = !ignoreColon;
            string t;

            Assert(s != null);
             
            if (s.StartsWith("!"))
            {
                flushIt = true;
                t = s.Substring(1);
            }
            else if (s.StartsWith(">"))
            {
                noNewline = true;
                treatColon = false;
                t = s.Substring(1);
            }
            else if (s.StartsWith("~:"))
            {
                treatColon = false;
                t = s.Substring(2);
                if (t.StartsWith("!"))
                {
                    flushIt = true;
                    t = s.Substring(3);
                }
            }
            else
            {
                t = s;
            }

            if ((t.IndexOf(":\\") < 0) && (t.IndexOf("://") < 0) && treatColon)
            {
                int sep = ignoreColon ? 0 : t.IndexOf(":");
                if (sep > 0)
                {
                    string s1 = t.Substring(0, sep).Trim();
                    string s2 = t.Substring(sep + 1).Trim();

                    try
                    {
                        t = string.Format($"{s1,-36}:{s2,24}");
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("offending s1: {0}", s1);
                        Console.WriteLine("offending s2: {0}", s2);
                        Console.WriteLine("sep {0}", sep);

                        throw e;
                    }
                }
            }
            if (!quiet)
            {
                if (noNewline)
                {
                    Console.Write("{0}", t);
                }
                else if (string.IsNullOrEmpty(t))
                {
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("{0}", t);
                }
            }

            if (fOut != null)
            {
                if (noNewline)
                {
                    fOut.Write(t);
                }
                else
                {
                    fOut.WriteLine(t);
                }

                if (flushIt)
                {
                    fOut.Flush();
                }
            }
#if DEBUG
            if (noNewline)
            {
                Debug.Write(t);
            }
            else
            {
                Debug.WriteLine(t);
            }
#endif
        }

#pragma warning disable IDE1006 // Naming Styles
        public static void o2(string format = "", params object[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            if (debugLevel >= 2)
            {
                o(format, args);
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        public static void o3(string format = "", params object[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            if (debugLevel >= 3)
            {
                o(format, args);
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        public static void o4(string format = "", params object[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            if (debugLevel >= 4)
            {
                o(format, args);
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        public static void o5(string format = "", params object[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            if (debugLevel >= 5)
            {
                o(format, args);
            }
        }

        public static StreamReader OpenReader(string fileName = "<stdin>", Encoding enc = null)
        {
            StreamReader sr = null;

            fileName = Unquote(fileName);

            if (string.IsNullOrEmpty(fileName) || fileName.Equals("<stdin>"))
            {
                sr = new StreamReader(Console.OpenStandardInput(), enc??Encoding.Default);
            }
            else if (Exists(fileName))
            {
                try
                {
                    sr = new StreamReader(fileName, enc);
                }
                catch (Exception ex)
                {
                    Fatal($"Error opening input file '{fileName}' ({ex.Message}'");
                }
            }
            else
            {
                Fatal($"Cannot find input file '{fileName}'");
            }

            return sr;
        }

        public static IEnumerable<string> ReadFileLines(string fileName, Encoding enc = null)
        {
            using (var sr = OpenReader(fileName, enc))
            {
                while (!sr.EndOfStream)
                {
                    yield return sr.ReadLine();
                }
            }

            yield break;
        }

        public static StreamWriter OpenWriter(string fileName = "<stdout>", Encoding enc = null)
        {
            StreamWriter sw = null;

            fileName = Unquote(fileName);

            if (string.IsNullOrEmpty(fileName) || (fileName == "<stdout>"))
            {
                sw = new StreamWriter(Console.OpenStandardOutput(), enc??Encoding.Default);
            }
            else
            {
                try
                {
                    sw = new StreamWriter(fileName, false, enc??Encoding.Default);
                }
                catch (Exception ex)
                {
                    Fatal($"Cannot open output file '{fileName}' ({ex.Message})");
                }
            }

            return sw;
        }

        /// <summary>
        /// Return string enclosed in double quotes
        /// unless already enclosed in quotes
        /// </summary>
        public static string Quote(string s)
        {
            s = s.Trim();

            if (s.StartsWith("\"") && s.EndsWith("\""))
            {
                return s;
            }

            return $"\"{s}\"";
        }

        /// <summary>
        /// throw a general Exception
        /// </summary>
        /// <param name="format">message format</param>
        /// <param name="args">message argument</param>
        public static void Raise(string format, params object[] args)
        {
            string msg = format.StringFormat(args);

            if (IsInDebugMode())
            {
                o("");
                o("Call stack:");
                o("");

                StackTracer.ShowStack();
            }

            throw new Exception(msg);
        }

        public static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = 
                new("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();

            //  recursively kill all child processes
            foreach (ManagementObject mo in moc.Cast<ManagementObject>())
            {
                int id = Convert.ToInt32(mo["ProcessID"]);

                KillProcessAndChildren(id);
            }

            //  finally call the process itself
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            { 
                /* process already exited */ 
            }
            catch (Exception ex)
            {
                o("KillProcessAndChildren: exception " + ex.Message);
            }
        }

        public static bool IsProcessActive(int pid)
        {
            ManagementObjectSearcher searcher = 
                new("Select * From Win32_Process Where ProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            bool ret = false;

            foreach (ManagementObject mo in moc.Cast<ManagementObject>())
            {
                int id = Convert.ToInt32(mo["ProcessID"]);

                if (id == pid)
                {
                    o4("Process PID " + pid + " is still active");
                    ret = true;
                }
            }

            return ret;
        }

        /// <summary>
        /// return number as string with thousands separators
        /// </summary>
        public static string Pretty3Num(long i)
        {
            if (i < 1000)
            {
                if (i < 0)
                {
                    return "-" + Pretty3Num(-i);
                }

                return i.ToString();
            }
            else
            {
                return $"{Pretty3Num(i / 1000)},{i % 1000:000}";
            }
        }

        /// <summary>
        ///  return number as string with k, m, g (factor 1000) suffix as applicable
        /// </summary>
        public static string PrettyNum(double x)
        {
            string s;
            IFormatProvider ifp = CultureInfo.InvariantCulture.NumberFormat;

            if (double.IsNaN(x))
            {
                s = "NaN";
            }
            else if (x < 0.001)
            {
                s = "-";
            }
            else if (x < 0.1)
            {
                //  display MB with x1000
                s = string.Format(ifp, "{0:0.#}", 1024 * x);
            }
            else if (x < 10)
            {
                s = string.Format(ifp, "{0:0.#}", x);
            }
            else if (x < 100)
            {
                s = string.Format(ifp, "{0:0.#}", x);
            }
            else if (x < 1000)
            {
                s = string.Format(ifp, "{0:0}", x);
            }
            else if (x < 10000)
            {
                s = string.Format(ifp, "{0:0.#}", x / 1000);
            }
            else if (x < 250e3)
            {
                s = string.Format(ifp, "{0:#,0.0#}k", x / 1000);
            }
            else if (x < 250e6)
            {
                s = string.Format(ifp, "{0:#,0.0#}m", x / 1e6);
            }
            else if (x < 250e9)
            {
                s = string.Format(ifp, "{0:#,0.0#}g", x / 1e9);
            }
            else
            {
                s = string.Format(ifp, "{0:#,0.0#}t", x / 1e12);
            }

            return s;
        }

        /// <summary>
        ///  return number as string with k, m, g (factor 1024) suffix as applicable
        /// </summary>
        public static string PrettyNumIB(double x)
        {
            string s;
            IFormatProvider ifp = CultureInfo.InvariantCulture.NumberFormat;

            if (double.IsNaN(x))
            {
                s = "NaN";
            }
            else if (x < 0.001)
            {
                s = "-";
            }
            else if (x < 10)
            {
                s = string.Format(ifp, "{0:0.#}", x);
            }
            else if (x < 100)
            {
                s = string.Format(ifp, "{0:0.#}", x);
            }
            else if (x < 1000)
            {
                s = string.Format(ifp, "{0:0}", x);
            }
            else if (x < 250e3)
            {
                s = string.Format(ifp, "{0:#,0.0#}k", x / 1024);
            }
            else if (x < 250e6)
            {
                s = string.Format(ifp, "{0:#,0.0#}m", x / (1024.0 * 1024.0));
            }
            else if (x < 250e9)
            {
                s = string.Format(ifp, "{0:#,0.0#}g", x / (1024.0 * 1024.0 * 1024.0));
            }
            else
            {
                s = string.Format(ifp, "{0:#,0.0#}t", x / (1024.0 * 1024.0 * 1024.0 * 1024.0));
            }

            return s;
        }

        public static IEnumerable<int> Range(int length)
        {
            return Enumerable.Range(0, length);
        }

        public static void Title(string format, params object[] args)
        {
            string s = string.Format(format, args);
            o("");
            o(s);
            o("=".PadLeft(s.Length, '='));
        }

        /// <summary>
        /// Convert int to string with 3-digit-groups
        /// </summary>
        /// <param name="n">The long int to convert</param>
        /// <returns>The resulting string</returns>
        public static string Pretty_int(long n)
        {
            if (n < 0)
            {
                return "-" + Pretty_int(-n);
            }
            else if (n < 1000)
            {
                return "" + n;
            }
            else
            {
                return Pretty_int(n / 1000) + "," + (n % 1000).ToString("000");
            }
        }

        /// <summary>
        /// Close process without handling a possible exception
        /// </summary>
        /// <param name="p">The process</param>
        private static void ProcessClose(Process p)
        {
            if (p != null)
            {
                try
                {
                    p.Close();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine("Process close exception ignored: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Close the shelled process, if there is any
        /// </summary>
        /// <param name="reason">why closing?</param>
        public static void ShellTerminate(string reason)
        {
            Process p = shelledProcess;

            try
            {
                if (p == null)
                {
                    o3(reason + ". No shelled process.");
                }
                else if (p.HasExited)
                {
                    o3(reason + ". Process has exited already.");
                }
                else
                {
                    //Process is still running.
                    //Test to see if the process is hung up.
                    if (p.Responding)
                    {
                        //Process was responding; close the main window.
                        o3(reason + ". Process is being closed gracefully.");
                        if (p.CloseMainWindow())
                        {
                            //Process was not responding; force the process to close.
                            o3(reason + ". Process is being killed.");
                            p.Kill();
                            ProcessClose(p);
                            p = null;
                        }
                    }

                    if (p != null)
                    {
                        //Process was not responding; force the process to close.
                        o3(reason + ". Process is being killed.");
                        p.Kill();
                        Sleep(1000);

                        if (!p.WaitForExit(2000))
                        {
                            o3(reason + ". Process kill has not succeeded yet.");

                            KillProcessAndChildren(p.Id);
                        }

                        ProcessClose(p);
                    }
                }

                if (shellOut != null)
                {
                    shellOut.Flush();
                    shellOut.Close();
                    shellOut = null;
                }

                shelledProcess = null;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                o("Process kill in progress?");
                o("Win32Exception in shellTerminate: " + ex.Message);
            }
            catch (Exception ex)
            {
                o("Exception in shellTerminate: " + ex.Message);
            }
        }

        private static void TaskKill(int pid)
        {
            if (IsProcessActive(pid))
            {
                o("Killing PID " + pid);
                using Process proc = new();
                Sleep(1000);
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = "taskkill.exe";
                proc.StartInfo.Arguments = "/F /T /PID " + pid;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit(8000);
            }
        }

        public delegate void WriteLineCallBack(string s);

        /// <summary>
        /// Start an external program as sub-process
        /// </summary>
        /// <param name="prog">Name of the executable program</param>
        /// <param name="args">Arguments</param>
        /// <param name="wait">True, if we wait until program completes</param>
        /// <param name="timeOutInMs">Force process to xit after timeOutInMs milliseconds; 0 = no timeout</param>
        /// <param name="outputFileName">Name of the file to write. Empty, if no file is required</param>
        /// <param name="stdoutCallBack">Function to write stdout strings</param>
        /// <param name="stderrCallBack">Function to write stderr strings</param>
        public static void Shell(string prog, string args, bool wait = true, int timeOutInMs = 0, string outputFileName = "",
                                 WriteLineCallBack stdoutCallBack = null, WriteLineCallBack stderrCallBack = null)
        {
            //  use an extended class to get proper line breaks in output data
            Process p = new();
            bool redirectOut = (outputFileName != "") || (stdoutCallBack != null) || (stderrCallBack != null);
            int pid;
            AsyncStreamReader stdoutReader = null;
            AsyncStreamReader stderrReader = null;
            var psi = p.StartInfo;

            try
            {
                shellOut = null;
                shelledProcess = null;
                prog = FindExePath(prog);

                if (!Exists(prog))
                {                  
                    Raise("Program file '" + prog + "' not found.");
                }

                if (debugLevel >= 1)
                {
                    OutputReflowed($"Starting {prog} {args}");
                }

                psi.FileName = prog;
                psi.Arguments = args;

                if (redirectOut)
                {
                    if (outputFileName != "")
                    {
                        o4($"Output redirected to '{outputFileName}'");
                        shellOut = OpenWriter(outputFileName);
                    }
                    else
                    {
                        o4("Output redirected to callbacks");
                    }

                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    psi.RedirectStandardInput = true;  // http://stackoverflow.com/a/31076753/1911064
                    psi.CreateNoWindow = true;
                }
                else
                {
                    psi.WindowStyle = ProcessWindowStyle.Normal;
                }
                p.Exited += Shell_Exited;
                p.EnableRaisingEvents = true;

                shelledProcess = p;
                p.Start();
                // p.PriorityClass = ProcessPriorityClass.BelowNormal;
                pid = p.Id;

                if (redirectOut)
                {
                    bShellNullReceived = false;

                    stdoutReader = new AsyncStreamReader(p.StandardOutput);
                    stdoutReader.DataReceived += Shell_OutputDataReceived;
                    stdoutReader.Start();

                    stderrReader = new AsyncStreamReader(p.StandardError);
                    stderrReader.DataReceived += Shell_ErrorDataReceived;
                    stderrReader.Start();

                    shellStdoutCallBack = stdoutCallBack;
                    shellStderrCallBack = stderrCallBack;
                }

                if (wait)
                {
                    if (timeOutInMs == 0)
                    {
                        o3($"Waiting for {prog} (PID {pid}) to complete");
                        p.WaitForExit();

                        if (redirectOut && !bShellNullReceived)
                        {
                            o4("Waiting for final data null");
                            for (int i = 1; (i < 50) && !bShellNullReceived; i++)
                            {
                                Sleep(50);
                            }

                            if (!bShellNullReceived)
                            {
                                o4("Giving up to wait");
                            }
                            else
                            {
                                o4("ok! Wait was successful");
                            }
                        }

                        ProcessClose(p);
                        shelledProcess = null;
                    }
                    else
                    {
                        string t = "Timeout " + (timeOutInMs / 1000.0).ToString("#,##0.00") + "s";

                        o3($"Process {prog} (PID {pid}) started with " + t);
                        p.WaitForExit(timeOutInMs);

                        if ((shelledProcess != null) && !p.HasExited)
                        //  event shell_Exited has not been called
                        {
                            //  ProcessExtension Romy.Core
                            //  We can only hope that a suspended process is easier and more realiable to kill
                            p.PriorityClass = ProcessPriorityClass.Idle;
                            p.Suspend();

                            try
                            {
                                p.Kill();
                            }
                            catch(Exception ex)
                            {
                                o("Exception in Kill(): " + ex.Message);
                                Sleep(10000);
                            }

                            Sleep(100);

                            ProcessClose(p);
                            shelledProcess = null;

                            TaskKill(pid);
                            TaskKill(pid);
                            TaskKill(pid);

                            throw new TimeoutException(t + " reached");
                        }
                    }
                }
            }
            catch (TimeoutException /* ex */)
            {
                throw; // ex;
            }
            catch (Exception e)
            {
                o($"Error executing \"{prog} {args}\"");
                Fatal($"Exception: {e.Message}");
            }
            finally
            {
                if (shellOut != null)
                {
                    shellOut.Flush();
                    shellOut.Close();
                    shellOut = null;
                }
                shelledProcess = null;
            }
        }

        private static void Shell_WriteLine(string s, bool bStderr = false)
        {
            shellOut?.WriteLine(s);

            if (bStderr)
            {
                if (shellStderrCallBack != null)
                {
                    shellStderrCallBack(s);
                }
                else
                {
                    shellStdoutCallBack?.Invoke(s);
                }
            }
            else
            {
                shellStdoutCallBack?.Invoke(s);
            }
        }

        // write out info to the display window
        private static void Shell_OutputDataReceived(object sender, string s)
        {
            if (s == null)
            //  final data received
            {
                o4("Final external process data received");
                bShellNullReceived = true;
            }
            else if (((shellOut != null) || (shellStdoutCallBack != null)) && (shelledProcess != null))
            {
                if (s.EndsWith("\r\n"))
                {
                    s = s.Substring(0, s.Length - 2);
                    o4("~:" + s);
                    Shell_WriteLine(s);
                }
                else if (s.EndsWith("\r"))
                {
                    s = s.Substring(0, s.Length - 1);
                    o4("~:" + s);
                    Shell_WriteLine(s);
                }
                else
                {
                    o4("~:>" + s);
                    Shell_WriteLine(">" + s);
                }
            }
            else
            {
                Fatal($"shell_OutputDataReceived: '{s}'");
            }
        }

        private static void Shell_ErrorDataReceived(object sender, string s)
        {
            if (s != null) 
            {
                o4(s);
                Shell_WriteLine(s, bStderr: true);
            }
            else
            {
                o4("Final external process error data received");
            }
        }

        private static void Shell_Exited(object sender, EventArgs e)
        {
            Process p = shelledProcess;

            if (p != null)
            {
                if (shellOut != null)
                {
                    o3("Not yet ready to exit process. Data outstanding ...");
                }
                else
                {
                    shelledProcess = null;
                    o3("Shell process exited");
                }
            }
        }

        public static void Sleep(int milliSeconds)
        {
            Thread.Sleep(milliSeconds);
        }

        public static Assembly GetAssembly(string name)
        {
            AppDomain ad = AppDomain.CurrentDomain;
            Assembly[] ar = ad.GetAssemblies();
            Assembly ret = null;

            o5("Looking for Assembly '" + name + "'");
            foreach (Assembly asy in ar)
            {
                if (asy.FullName.StartsWith(name))
                {
                    ret = asy;
                    break;
                }
            }

            o5((ret != null) ? "Assembly found" : "Assembly not found");

            return ret;
        }

        public static void ShowAssemblyVersion(string name)
        {
            Assembly asy = GetAssembly(name);

            if (asy != null)
            {
                string v = asy.FullName;
                int comma = v.IndexOf(",", name.Length + 2);

                if (comma > 0)
                {
                    v = v.Substring(0, comma);
                }
                o(v);
            }
            else
            {
                o(name + ", Version ???");
            }
        }

        public static void ShowElapsed(string msg, Stopwatch watch)
        {
            if (debugLevel >= 2)
            {
                watch.Stop();
                long e = watch.ElapsedMilliseconds;

                if (e < 1000)
                {
                    o($"{msg} {e} ms");
                }
                else
                {
                    o(msg + " " + (e / 1000.0).ToString("#,##0.0", NumberFormatInfo.InvariantInfo) + " s");
                }
            }
        }

        /// <summary>
        /// Return current time of the day as string
        /// </summary>
        /// <returns></returns>
        public static string Timestamp()
        {
            string ret = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo);

            return ret;
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        public static void Unlink(string fileName)
        {
            try
            {
                if (Exists(fileName))
                {
                    o4($"Deleting file {fileName}");
                    File.Delete(fileName);
                }
            }
            catch(Exception ex)
            {
                o3($"Unlink({fileName}): Exception  {ex.Message}");
            }
        }

        /// <summary>
        /// Return a string with surrounding double quotes removed
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Unquote(string s)
        {
            string ret = s;

            if ((s.Length >= 2) && s.StartsWith("\"") && s.EndsWith("\""))
            {
                ret = s.Substring(1, s.Length - 2).Trim();
            }

            return ret;
        }
    }
}

//  end of class Util.cs
