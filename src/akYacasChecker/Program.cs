using akOptions;
using akUtil;
using System;

namespace akYacasChecker
{
    class Program : Util
    {
        const string logo = "akYacasChecker - Validate Matrix Multiplication Yacas script";
        const string whodidit = "Axel Kemper, Springe, 2025, compiled: $";
        const string contact = "contact: axel@kemperzone.de";

        static void Main(string[] args)
        {
            var build = GetBuild();
            string welcome = 
				BoxPrint(logo, whodidit.Replace("$", build), contact);

            Options.SetUsageHelp(welcome + 
			                     "\nUSAGE: {0} [options] <file-name>\n\n"
                                 + "Evaluates a Yacas script to check the correctness of a \n"
                                 + "matrix multiplication algorithm. This also works for\n"
                                 + "complex and float coefficients.\n");
            
            string cat = "MAIN";  //  capital letters!
            var rangeDebugLevel = new Range<int>(0, 5);
            var debug = 
				new IntOption(cat, "d", "debug", 
			                  "Debug level (0=quiet, ... 5=ultra-verbose)", 
							  def:3, rangeDebugLevel);

            Options.ParseOptions(args, strict: true);

            debugLevel = debug;

            o2(welcome);

            //  evaluate remaining positional parameters
            if ((args.Length == 0) || (args[0] == null))
            {
                Options.PrintUsageAndExit(verbose: true);
            }

            try
            {
                Timer _ = new("Yacas validation");
                o();
                YacasChecker yc = new(args[0]);

                yc.Validate();
            }
            catch(Exception ex)
            {
                o("");
                o($"Exception: {ex.Message}");
                o("");

                Finish(1);
            }

            Finish(0);
        }   //  end of main()
    }  //  end of class program
}  //  end of namespace
