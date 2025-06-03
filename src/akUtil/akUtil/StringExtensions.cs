using System;
using System.Text.RegularExpressions;

namespace akUtil
{
    //  https://www.codeproject.com/Articles/692603/Csharp-String-Extensions

    public static class StringExtensions
    {
        /// <summary>
        /// String format with improved Exception handling
        /// </summary>
        /// <param name="stringparam">The string to use as format</param>
        /// <param name="args">The format arguments</param>
        /// <returns>The formatted string</returns>
        public static string StringFormat(this string stringparam, params object[] args)
        {
            string s;

            try
            {
                s = string.Format(stringparam, args);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Cannot format string \"{stringparam}\": {ex.Message}");
            }

            return s;
        }

        /// <summary>
        /// String format with improved Exception handling
        /// </summary>
        /// <param name="stringparam">The string to use as format</param>
        /// <returns>The original string</returns>
        public static string StringFormat(this string stringparam)
        {
            return stringparam;
        }


        /// <summary>
        /// Returns the number of lines in a string
        /// In Linq, this could be written as
        /// s.Where(c => c == '\n').Count()
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Number of '\n' characters in string s</returns>
        public static long Lines(this string s)
        {
            long count = 1;
            int position = 0;
            //  the following loop is faster than a direct
            //  loop through the string.
            //  IndexOf() has more efficient string access
            //  compared to user-level code.
            while ((position = s.IndexOf('\n', position)) != -1)
            {
                count++;
                position++;         // Skip this occurrence!
            }
            return count;
        }


        /// <summary>
        /// VB Left function
        /// </summary>
        /// <param name="stringparam"></param>
        /// <param name="numchars"></param>
        /// <returns>Left-most numchars characters</returns>
        public static string Left(this string stringparam, int numchars)
        {
            // Handle possible Null or numeric stringparam being passed
            stringparam += string.Empty;

            // Handle possible negative numchars being passed
            numchars = Math.Abs(numchars);

            // Validate numchars parameter
            if (numchars > stringparam.Length)
                numchars = stringparam.Length;

            return stringparam.Substring(0, numchars);
        }

        /// <summary>
        /// VB Right function
        /// </summary>
        /// <param name="stringparam"></param>
        /// <param name="numchars"></param>
        /// <returns>Right-most numchars characters</returns>
        public static string Right(this string stringparam, int numchars)
        {
            // Handle possible Null or numeric stringparam being passed
            stringparam += string.Empty;

            // Handle possible negative numchars being passed
            numchars = Math.Abs(numchars);

            // Validate numchars parameter
            if (numchars > stringparam.Length)
                numchars = stringparam.Length;

            return stringparam.Substring(stringparam.Length - numchars);
        }

        /// <summary>
        /// VB Mid function - to end of string
        /// </summary>
        /// <param name="stringparam"></param>
        /// <param name="startIndex">VB-Style startindex, 1st char startindex = 0</param>
        /// <returns>Balance of string beginning at startindex character</returns>
        public static string Mid(this string stringparam, int startIndex)
        {
            // Handle possible Null or numeric stringparam being passed
            stringparam += string.Empty;

            // Handle possible negative startindex being passed
            startIndex = Math.Abs(startIndex);

            // Validate numchars parameter
            if (startIndex >= stringparam.Length)
                startIndex = stringparam.Length - 1;

            // C# strings are zero-based
            return stringparam.Substring(startIndex);
        }

        /// <summary>
        /// VB Mid function - for number of characters
        /// </summary>
        /// <param name="stringparam"></param>
        /// <param name="startIndex">VB-Style startindex, 1st char startindex = 0</param>
        /// <param name="numchars">number of characters to return</param>
        /// <returns>Balance of string beginning at startindex character</returns>
        public static string Mid(this string stringparam, int startIndex, int numchars)
        {
            // Handle possible Null or numeric stringparam being passed
            stringparam += string.Empty;

            // Handle possible negative startindex being passed
            startIndex = Math.Abs(startIndex);

            // Handle possible negative numchars being passed
            numchars = Math.Abs(numchars);

            // Validate numchars parameter
            if (startIndex >= stringparam.Length)
                startIndex = stringparam.Length-1;

            // C# strings are zero-based
            return stringparam.Substring(startIndex, numchars);
        }

        /// <summary>
        /// Return true, if string matches a wildcard pattern.
        /// ? = match any char
        /// * = match any string
        /// </summary>
        /// <param name="stringparam"></param>
        /// <param name="pattern"></param>
        /// <returns>True, iff match</returns>
        public static bool MatchWildcard(this string stringparam, string pattern)
        {
            string regexPattern = "^";

            foreach (char c in pattern)
            {
                regexPattern += c switch
                {
                    '*' => ".*",
                    '?' => ".",
                    _ => "[" + c + "]",
                };
            }
            return new Regex(regexPattern + "$").IsMatch(stringparam);
        }

        public static double ToDouble(this string input, bool throwExceptionIfFailed = false)
        {
            var valid = double.TryParse(input.Replace('.',','), out double result);
            if (!valid && throwExceptionIfFailed)
            {
                throw new FormatException($"'{input}' cannot be converted as double");
            }
            return result;
        }

        public static float ToFloat(this string input, bool throwExceptionIfFailed = false)
        {
            var valid = float.TryParse(input.Replace('.', ','), out float result);
            if (!valid && throwExceptionIfFailed)
            {
                throw new FormatException($"'{input}' cannot be converted as float");
            }
            return result;
        }

        public static int ToInt(this string input, bool throwExceptionIfFailed = false)
        {
            var valid = int.TryParse(input, out int result);
            if (!valid && throwExceptionIfFailed)
            {
                throw new FormatException($"'{input}' cannot be converted as int");
            }
            return result;
        }

        public static string[] Tokenize(this string input, string separators)
        {
            return input switch
            {
                null => null,
                "" => [],
                _ => input.Split(separators.ToCharArray(), StringSplitOptions.RemoveEmptyEntries),
            };
        }
    }
}
