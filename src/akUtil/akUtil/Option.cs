using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace akOptions
{
    /// <summary>
    /// Option class is the base class of all commandline parameter options
    /// Inspired by and ported from Minisat
    /// cf. https://github.com/niklasso/minisat/tree/master/minisat/utils
    /// </summary>
    public abstract class Option : CommandlineParser
    {
        public string name;
        public string longName;
        public string description;
        public string category;
        public string type_name;

        private static List<Option> options = [];
        public static string usageString = "";
        public static string helpPrefixString = "";

        public static IEnumerable<Option> GetOptions()
        {
            return options;
        }

        //  show name and long name as array for Option method help()
        protected string[] Names { get { return [name, longName]; } }

        //  show description as formatted text
        protected void ShowDescription(bool verbose)
        {
            const int maxLineLen = 58;
            const int margin = 17;

            if (verbose)
            {
                string[] paragraphs = description.Split(['\n']);

                foreach (var paragraph in paragraphs)
                {
                    string[] words = paragraph.Split([' ']);
                    string line = "";
                    string sep = "";

                    foreach(string word in words)
                    {
                        if (line.Length + word.Length >= maxLineLen)
                        {
                            o_ignore_colon("{0," + margin + "}{1}", "", line);
                            line = word;
                        }
                        else
                        {
                            line = $"{line}{sep}{word}";
                            sep = " ";
                        }
                    }
                    if (!".!?".Contains(line.Last()))
                    {
                        line += ".";
                    }
                    o_ignore_colon("{0," + margin + "}{1}", "", line);
                }
                o("");
            }
        }

        public static void SortOptions()
        {
            options = [.. options.OrderBy(o => o.name).ThenBy(o => o.category)];
        }

        /// <summary>
        /// Construct an Option
        /// base class for Option of specific types
        /// </summary>
        /// <param name="name">The short name (usually single character)</param>
        /// <param name="longName">The long name</param>
        /// <param name="description">A descriptive text. May contain '\n' for line break-</param>
        /// <param name="category">Option category in all-upercase letters</param>
        /// <param name="type_name">Indicate expected value type like &lt;double&gt;</param>
        public Option(string name, string longName, string description, string category, string type_name)
        {
            this.name = name;
            this.longName = longName;
            this.description = description;
            this.category = category;
            this.type_name = type_name;

            //  descriptions should start with uppercase
            Debug.Assert(char.IsUpper(description[0]));
            foreach(char c in category)
            {
                Debug.Assert(char.IsUpper(c));
            }

            if (type_name != "")
            {
                Debug.Assert(type_name.StartsWith("<") && type_name.EndsWith(">"));
            }

            options.Add(this);
        }

        public string OptionNames()
        {
            string ret = "for option";

            if (name != "")
            {
                ret += "\"{name}\" ";

                if (longName != "")
                {
                    ret += "or ";
                }
            }

            if (longName != "")
            {
                ret += "\"{longName}\" ";
            }

            return ret;
        }

        public abstract bool Parse(string str);
        public abstract void Help(bool verbose = false);
    }

    //
    //  Range classes
    //

    public class Range<T>(T min, T max) where T : IComparable
    {
        public T begin = min;
        public T end = max;

        public bool IsInRange(T v, Option o)
        {
            if (v.CompareTo(end) > 0)
            {
                CommandlineParser.Fatal($"value <{v}> is too large {o.OptionNames()}.");
            }
            else if (v.CompareTo(begin) < 0)
            {
                CommandlineParser.Fatal($"value <{v}> is too small {o.OptionNames()}.");
            }

            return true;
        }
    }

    //
    //  Double options:
    //

    /// <summary>
    /// Construct an Option for a &lt;double&gt; parameter
    /// </summary>
    /// <param name="category">Option category in all-upercase letters</param>
    /// <param name="name">The short name (usually single character)</param>
    /// <param name="longName">The long name</param>
    /// <param name="description">A descriptive text. May contain '\n' for line break-</param>
    /// <param name="def">The default value; used if parameter is not specified</param>
    /// <param name="r">Allowed range of values</param>
    public class DoubleOption(string category, string name, string longName, string description, double def = default, Range<double> r = null) : Option(name, longName, description, category, "<double>")
    {
        protected Range<double> range = r ?? (new Range<double>(double.MinValue, double.MaxValue));
        public double Value { set; get; } = def;

        public static implicit operator double(DoubleOption d)
        {
            //  convert DoubleOption to double without cast
            return d.Value;
        }

        public override void Help(bool verbose = false)
        {
            string dash = "-";

            foreach (var name in Names)
            {
                if (name != "")
                {
                    string s = (range.begin == double.MinValue) ?
                               " " : $"{range.begin:G4}";

                    s += " .. ";

                    s += (range.end == double.MaxValue) ?
                         " " : $"{range.end:G4}";

                    o_ignore_colon($"  {dash + name,-12} = {type_name,-8} [{s}] (default: {Value:G4})");
                }
                dash = "--";
            }
            ShowDescription(verbose);
        }

        public override bool Parse(string str)
        {
            string span = str;
            bool ret = true;

            if (IsOtherOption(ref span, name, longName))
            {
                ret = false;
            }
            else if (!double.TryParse(span.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double tmp))
            {
                ret = false;
            }
            else if (range.IsInRange(tmp, this))
            {
                Value = tmp;
            }

            return ret;
        }
    }


    //==================================================================================================
    // Int options:

    /// <summary>
    /// Construct an Option for a &lt;Int32&gt; (32 bit) parameter
    /// </summary>
    /// <param name="category">Option category in all-upercase letters</param>
    /// <param name="name">The short name (usually single character)</param>
    /// <param name="longName">The long name</param>
    /// <param name="description">A descriptive text. May contain '\n' for line break-</param>
    /// <param name="def">The default value; used if parameter is not specified</param>
    /// <param name="r">Allowed range of values</param>
    public class IntOption(string category, string name, string longName, string description, int def = default, Range<int> r = null) : Option(name, longName, description, category, "<int32>")
    {
        protected Range<int> range = r ?? new Range<int>(int.MinValue, int.MaxValue);
        public int Value { get; set; } = def;

        public static implicit operator int(IntOption i)
        {
            //  convert IntOption to System.Int32 (equivalent to "int") without cast
            return i.Value;
        }

        public override bool Parse(string str)
        {
            string span = str;
            bool ret = true;

            if (IsOtherOption(ref span, name, longName))
            {
                ret = false;
            }
            else if (!int.TryParse(span, out int tmp))
            {
                ret = false;
            }
            else if (range.IsInRange(tmp, this))
            {
                Value = tmp;
            }

            return ret;
        }

        public override void Help(bool verbose = false)
        {
            string dash = "-";

            foreach (var name in Names)
            {
                if (name != "")
                {
                    string s = (range.begin == int.MinValue) ? 
                          " " : $"{range.begin,4}";

                    s += " .. ";

                    s += (range.end == int.MaxValue) ?
                        " " : $"{range.end,4}";

                    o_ignore_colon($"  {dash + name,-12} = {type_name,-8} [{s}] (default: {Value})");
                }
                dash = "--";
            }
            ShowDescription(verbose);
        }
    };

    /// <summary>
    /// Construct an Option for a &lt;Int64&gt; (64 bit) parameter
    /// </summary>
    /// <param name="category">Option category in all-upercase letters</param>
    /// <param name="name">The short name (usually single character)</param>
    /// <param name="longName">The long name</param>
    /// <param name="description">A descriptive text. May contain '\n' for line break-</param>
    /// <param name="def">The default value; used if parameter is not specified</param>
    /// <param name="r">Allowed range of values</param>
    public class Int64Option(string category, string name, string longName, string description, long def = default, Range<long> r = null) : Option(name, longName, description, category, "<int64>")
    {
        protected Range<long> range = r ?? new Range<long>(long.MinValue, long.MaxValue);
        public long Value { get; set; } = def;

        public static implicit operator long(Int64Option i)
        {
            //  convert Int64Option to long without cast
            return i.Value;
        }

        public override bool Parse(string str)
        {
            string span = str;
            bool ret = true;

            if (IsOtherOption(ref span, name, longName))
            {
                ret = false;
            }
            else if (!long.TryParse(span, out long tmp))
            {
                ret = false;
            }
            else if (range.IsInRange(tmp, this))
            {
                Value = tmp;
            }

            return ret;
        }

        public override void Help(bool verbose = false)
        {
            string dash = "-";

            foreach(string name in Names)
            {
                if (name != "")
                {
                    string s = (range.begin == long.MinValue) ?
                             " " : $"{range.begin,4}";

                    s += " .. ";

                    s += (range.end == long.MaxValue) ?
                      " " : $"{range.end,4}";

                    o_ignore_colon($"  {dash+name,-12} = {type_name,-8} [{s}] (default: {Value})");
                }
                dash = "--";
            }
            ShowDescription(verbose);
        }
    }

    //==================================================================================================
    // String option:

    /// <summary>
    /// Construct an Option for a &lt;string&gt; parameter
    /// </summary>
    /// <param name="category">Option category in all-upercase letters</param>
    /// <param name="name">The short name (usually single character)</param>
    /// <param name="longName">The long name</param>
    /// <param name="description">A descriptive text. May contain '\n' for line break-</param>
    /// <param name="def">The default value; used if parameter is not specified</param>
    public class StringOption(string category, string name, string longName, string description, string def = null, bool noshow = false) : Option(name, longName, description, category, "<string>")
    {
        public string Value { get; set; } = def;
        private readonly bool noshow = noshow;

        public static implicit operator string(StringOption s)
        {
            //  convert StringOption to string without cast
            return s.Value;
        }

        public override bool Parse(string str)
        {
            string span = str;
            bool ret = true;

            if (IsOtherOption(ref span, name, longName))
            {
                ret = false;
            }
            else if ((span.Length >= 2) && span.StartsWith("\"") && span.EndsWith("\""))
            {
                Value = span.Substring(1, span.Length - 2);
            }
            else
            {
                Value = span;
            }

            return ret;
        }

        public override void Help(bool verbose = false)
        {
            string dash = "-";

            foreach (var name in Names)
            {
                if (name != "")
                {
                    var d = "";

                    if ((Value != null) && (Value != "") && !noshow)
                    {
                        d = $" (default: \"{Value}\")";
                    }
                    o($"  {dash + name, -12} = {type_name,8}{d}");
                }
                dash = "--";
            }
            ShowDescription(verbose);
        }
    }

    //==================================================================================================
    // Bool option:

    /// <summary>
    /// Construct an Option for a &lt;bool&gt; parameter
    /// </summary>
    /// <param name="category">Option category in all-upercase letters</param>
    /// <param name="name">The short name (usually single character)</param>
    /// <param name="longName">The long name</param>
    /// <param name="description">A descriptive text. May contain '\n' for line break-</param>
    /// <param name="def">The default value; used if parameter is not specified</param>
    public class BoolOption(string category, string name, string longName, string description, bool def = false) : Option(name, longName, description, category, "<bool>")
    {
        public bool Value { get; set; } = def;

        public static implicit operator bool(BoolOption b)
        {
            //  convert BoolOption to bool without cast
            return b.Value;
        }

        public override bool Parse(string str)
        {
            string span = str;
            bool ret = false;

            if (Match(ref span, "--"))
            {
                bool b = !Match(ref span, "no-");

                if (span == longName)
                {
                    Value = b;
                    ret = true;
                }
            }
            else if (Match(ref span, "-"))
            {
                bool b = !Match(ref span, "no-");

                if (span == name)
                {
                    Value = b;
                    ret = true;
                }
            }

            return ret;
        }

        public override void Help(bool verbose = false)
        {
            string dash = "-";

            foreach (var name in Names)
            {
                if (name != "")
                {
                    string s = $"  {dash+name+", "+dash+"no-"+name, -24}";
                    string def = Value ? "on / set" : "off / not set";

                    o_ignore_colon($"{s} (default: {def})");
                }
                dash = "--";
            }
            ShowDescription(verbose);
        }
    }
}

//  end of file Option.cs
