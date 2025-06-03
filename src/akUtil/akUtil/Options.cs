using akUtil;
using System.IO;

namespace akOptions
{
    /// <summary>
    /// Options class provides a tool to parse commandline options
    /// Inspired by and ported from Minisat
    /// </summary>
    public class Options : CommandlineParser
    {
        /// <summary>
        /// Iterate arguments 'args' and evaluate optios.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        /// <param name="strict">true: do not accept unknown flags</param>
        /// The non-recognized arguments are copied to the beginning of array
        /// 'args'. To indicate the end of arguments, the remaining 'args'
        /// are set to null.
        public static void ParseOptions(string[] args, bool strict)
        {
            int i, j;
            int argc = args.Length;
            for (i = j = 0; i < argc; i++)
            {
                string str = args[i];
                if (Match(ref str, "--") && Match(ref str, Option.helpPrefixString) && Match(ref str, "help"))
                {
                    if (str == "")
                    {
                        PrintUsageAndExit(verbose:true);
                    }
                    else if (Match(ref str, "-verb"))
                    {
                        PrintUsageAndExit(verbose:true);
                    }
                    else if (Match(ref str, "-brief"))
                    {
                        PrintUsageAndExit(verbose: true);
                    }
                }
                else
                {
                    bool parsed_ok = false;
                    var options = Option.GetOptions();

                    foreach (var option in options)
                    {
                        parsed_ok = option.Parse(args[i]);

                        if (parsed_ok)
                        {
                            break;
                        }
                    }

                    if (!parsed_ok)
                    {
                        if (strict && Match(ref args[i], "-"))
                        {
                            Fatal($"Unknown flag \"-{args[i]}\" or missing \"=\". Use '--{Option.helpPrefixString}help' for help.");
                        }
                        else
                        {
                            args[j++] = args[i];
                        }
                    }
                }
            }

            argc -= (i - j);

            //  clear arguments which have been processed
            for (int k = args.Length - 1; k >= argc; k--)
            {
                args[k] = null;
            }
        }

        /// <summary>
        /// The usage help string may contain {0} to reference the program name
        /// </summary>
        /// <param name="str">String to display as header for --help</param>
        public static void SetUsageHelp(string str)
        {
            Option.usageString = str;
        }

        /// <summary>
        /// This can be used to modify the parameter name --help to --XYZhelp
        /// </summary>
        /// <param name="str">The prefix</param>
        public static void SetHelpPrefixStr(string str)
        {
            Option.helpPrefixString = str;
        }

        public static void PrintUsageAndExit(bool verbose = false)
        {
            string usage = Option.usageString;

            if (usage != null)
            {
                var prog = GetApplicationPath();

                if (prog != string.Empty)
                {
                    prog = Path.GetFileNameWithoutExtension(prog);
                }

                o(usage, prog);
            }

            Option.SortOptions();

            string prev_cat = null;
            string prev_type = null;

            var options = Option.GetOptions();

            foreach (Option option in options)
            {
                string cat = option.category;
                string type = option.type_name;

                if (cat != prev_cat)
                {
                    o_ignore_colon($"\n{cat} OPTIONS:\n");
                }
                else if (type != prev_type)
                {
                    o("");
                }

                option.Help(verbose);

                prev_cat = option.category;
                prev_type = option.type_name;
            }

            o_ignore_colon("\nHELP OPTIONS:\n\n");
            o_ignore_colon("  {0,-15}Print verbose help message.", $"--{Option.helpPrefixString}help");
            //  o("  {0,-15}Print verbose help message.", $"--{Option.helpPrefixString}help-verb");
            o_ignore_colon("  {0,-15}Print brief help message.", $"--{Option.helpPrefixString}help-brief");
            o("");
            Finish(0);
        }
    }
}
