using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using static akExtractMatMultSolution.MatrixDimensions;
using static akExtractMatMultSolution.Util;

namespace akExtractMatMultSolution
{
    class Program
    {
        private static readonly string[] usageStrings =
        [
            "usage:",
            "",
            "akExtractMatMultSolution akboole_solution_file [yacas_script_output_file [brent_equations_file]]",
            "Translate akBoole solution file to Yacas algorithm file.",
            "",
            "or",
            "",
            "akExtractMatMultSolution --sat akboole_cnf_file sat_solution_file [yacas_script_output_file [brent_equations_file]]",
            "Translate akBool CNF input and SAT solver solution file to Yacas algorithm file",
            "",
            "or",
            "",
            "akExtractMatMultSolution --gringo gringo_solution_file [yacas_script_output_file [brent_equations_file]]",
            "Translate Gringo solution file to Yacas algorithm file",
            "",
            "or",
            "",
            "akExtractMatMultSolution --bini bini_solution_file [yacas_script_output_file [brent_equations_file]]",
            "Translate solution from Bini matrix form in Yacas algorithm file",
            "",
            "or",
            "",
            "akExtractMatMultSolution --binigen yacas_solution_file [bini_output_file [brent_equations_file]]",
            "Translate Yacas algorithm file to Bini matrix format",
            "",
            "or",
            "",
            "akExtractMatMultSolution --tensor tensor_solution_file [yacas_script_file [brent_equations_file]]",
            "Translate tensor solution format to Yacas algorithm file.",
            "In tensor solution form, each product is a product of a, b and c sums.",
            "",
            "or",
            "",
            "akExtractMatMultSolution --tensorgen yacas_solution_file [tensor_output_file [brent_equations_file]]",
            "Translate Yacas algorithm file to tensor solution form.",
            "",
            "or",
            "",
            "akExtractMatMultSolution --combine akboole_mod2_solution akboole_rev_solution [yacas_script_output_file [brent_equations_file]]",
            "Combine akBoole solutions for the mod2 case and the revised case into a universal Yacas algorithm file.",
            "The revised solution provides '+'/'-' signs for the mod 2 solution. 1 = '+', 0 = '-'",
            "",
            "or",
            "",
            "akExtractMatMultSolution --yacas yacas_solution_file [unified_yacas_script_output_file [brent_equations_file]]",
            "Unify a Yacas algorithm to adjust syntax and layout.",
            "Show statistics of the algorithm",
            "",
            "or",
            "",
            "akExtractMatMultSolution --graph yacas_solution_file [graphviz_detail_file [graphviz_overview_file [product_report_file]]]",
            "Translate Yacas algorithm file into a GraphViz graph",
            "",
            "or",
            "",
            "akExtractMatMultSolution --reduce yacas_solution_file ixjxn [yacas_script_output_file]",
            "Derive a reduced solution for the ixjxn problem.",
            "i = number of A rows, j = number of A cols, n = number of C columns",
            "By default, one row and one column are eliminated from product matrix [C].",
            "",
            "or",
            "",
            "akExtractMatMultSolution --literals yacas_solution_file [akboole_solution_file [brent_equations_file]]",
            "Translate Yacas algorithm file to akBoole solution format (= list of literals and their values).",
            "",
            "or",
            "",
            "akExtractMatMultSolution --contrib yacas_solution_file [product_contributions_file [brent_equations_file]]",
            "Show which coefficient contributes to which product",
            "",
            "or",
            "",
            "akExtractMatMultSolution --cse yacas_solution_file [simplified_yacas_script_output_file [brent_equations_file]]",
            "Strive to eliminate common subexpressions from sums in algorithm.",
            "",
            "Note: use empty string \"\" or \"none\" to suppress output of brent_eqations_file",
            ""
        ];

        private struct LitDescriptor
        {
            public Mat mat;
            public int row;
            public int col;
            public int product;
        };

        enum ParseState { started, name, row, col, finished };

        static Dictionary<int, LitDescriptor> dicLit;

        static readonly char[] XSeparator = ['x'];

        static string biniFileName = "";
        static string tensorFileName = "";
        static string brentEquationFileName = "";
        static string graphvizDetailFileName = "";
        static string graphvizOverviewFileName = "";
        static string reducedSignature = "";
        static string reducedYacasFileName = "";
        static string productReportFileName = "";
        static string solutionFileName = "";
        static string simplifiedSolutionFileName = "";
        static string productContributionsFileName = "";
        static string yacasInputFileName = "";
        static readonly List<string> reduceResult = [];

        static void Main(string[] args)
        {
            Welcome();

            if ((args.Length < 1) || "-h~--help~/?~-?".Contains(args[0]))
            {
                Usage();
            }

            //  for international data formatting
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            ReadLiteralValues(args);
            GetProblemDimensions();
            SetArraysToReadonly();
            WriteReducedYacasSolution(args);
            WriteYacasSolution(args);
            WriteBiniFile(biniFileName);
            WriteTensorFile(tensorFileName);
            WriteBrentEquations(brentEquationFileName);
            WriteGraphvizDetailFile(graphvizDetailFileName);
            WriteGraphvizOverviewFile(graphvizOverviewFileName);
            WriteProductReportFile(productReportFileName);
            WriteProductContributionsFile(productContributionsFileName);
            WriteSolutionFile(solutionFileName);
            WriteSimplifiedYacasSolution(simplifiedSolutionFileName);
            Finish(0);
        }
         
        private static void Welcome()
        {
            o("");
            o("akExtractMatMultSolution  -  Recover or Convert Matrix Multiplication Solution");
            o("------------------------------------------------------------------------------");
            o($"Compiled: {Properties.Resources.BuildDate.Substring(0,10)}");
            o("");
#if DEBUG
            o($"DEBUG mode");
            o("");
#endif
        }

        static void ReadLiteralValues(string[] args)
        {
            switch (args[0])
            {
                case "--gringo":
                    CreateLiteralArrays(defaultValue: 0);

                    ReadLiteralValuesGringo(args[1]);
                    break;

                case "--bini":
                    CreateLiteralArrays(defaultValue: undefined);

                    ReadLiteralValuesBini(args[1]);
                    break;

                case "--yacas":
                case "--graph":
                case "--reduce":
                case "--binigen":
                case "--tensorgen":
                case "--literals":
                case "--contrib":
                case "--cse":
                    CreateLiteralArrays(defaultValue: 0);

                    ReadLiteralValuesYacas(args[1]);
                    break;

                case "--tensor":
                    CreateLiteralArrays(defaultValue: 0);

                    ReadLiteralValuesTensor(args[1]);
                    break;

                case "--sat":
                    CheckFile(args[1], "akboole CNF/DIMACS");
                    CheckFile(args[2], "SAT solution");
                    o($"Reading CNF/DIMACS file '{args[1]}' and SAT solution file '{args[2]}'");

                    dicLit = [];

                    CreateLiteralArrays(defaultValue: undefined);

                    ReadAkBooleLiteralIDs(args[1]);
                    ReadSatSolution(args[2]);
                    break;

                case "--combine":
                    CheckFile(args[1], "akBoole solution");
                    CheckFile(args[2], "akBoole revised solution");
                    o($"Reading akBoole solution file '{args[1]}' and revised solution file '{args[2]}'");

                    dicLit = [];

                    CreateLiteralArrays(defaultValue: undefined);

                    ReadAkBooleSolution(args[1], revisedMode:false);
                    GetProblemDimensions();
                    ReadAkBooleSolution(args[2], revisedMode: true);
                    break;

                default:
                    using (StreamReader sr = OpenReader(args[0], "akBoole solution"))
                    {
                        CreateLiteralArrays(defaultValue: undefined);

                        while (!sr.EndOfStream)
                        {
                            string s = sr.ReadLine();

                            if (s.Contains(" = ") && !s.StartsWith("[") && !s.StartsWith("Variables:") &&
                                (s.EndsWith("0") || s.EndsWith("1")))
                            {
                                ReadLiteralValue(s, revisedMode: false);
                            }
                        }
                    }
                    break;
            }
        }

        private static void ReadSatSolution(string fileName)
        {
            o($"Reading variable assignments");

            using StreamReader sr = OpenReader(fileName, "SAT solution");
            string s = sr.ReadLine();
            s ??= "<null> empty file?";
            Check(s == "SAT", $"SAT as first line expected. Got '{s}'");

            while (!sr.EndOfStream)
            {
                s = sr.ReadLine();
                if (s.StartsWith("v ", StringComparison.InvariantCultureIgnoreCase))
                {
                    s = s.Substring(2);
                }
                string[] a = Tokenize(s);

                foreach (string lit in a)
                {
                    int litVal = lit.StartsWith("-") ? 0 : 1;
                    int id = ((litVal == 1) ? 1 : -1) * atoi(lit);

                    if (id == 0)
                    {
                        break;
                    }

                    if (dicLit.ContainsKey(id))
                    {
                        if (dicLit.TryGetValue(id, out LitDescriptor ld))
                        {
                            litArrays[(int)ld.mat][ld.row, ld.col, ld.product] = litVal;
                        }
                        else
                        {
                            Fatal($"Could not retrieve literal ID {id}");
                        }
                    }
                }
            }
        }

        private static void ReadAkBooleLiteralIDs(string fileName)
        {
            o($"Extracting literal IDs");

            using StreamReader sr = OpenReader(fileName, "akboole CNF/DIMACS");
            while (!sr.EndOfStream)
            {
                string s = sr.ReadLine();

                if (s.StartsWith("c ") &&
                    (s.Length >= "c F11a 1".Length) &&
                    ("fgdFGD".IndexOf(s[2]) >= 0) &&
                    ("123456789".IndexOf(s[3]) >= 0) &&
                    ("123456789".IndexOf(s[4]) >= 0))
                {
                    string[] a = Tokenize(s);

                    if (a.Length >= 3)
                    {
                        LitDescriptor ld = new();

                        string sID = a[a.Length - 1];
                        string sName = a[1].ToLower();

                        Check(a[0].Equals("c"), $"Expected line start 'c ' rather than '{a[0]}'");
                        Check(sName.Length == 4, $"Expected 4-char name rather than '{sName}'");
                        Check(IsNumeric(sID), $"Expected numeric literal ID rather than '{sID}'");

                        int id = atoi(sID);
                        Check(id > 0, "Non-positive ID in {0}", sID);

                        ld.mat = sName.StartsWith("f") ? Mat.F
                                : sName.StartsWith("g") ? Mat.G
                                : sName.StartsWith("d") ? Mat.D
                                : Mat.unknown;
                        Check(ld.mat != Mat.unknown, $"Invalid matrix type in '{sName}'");

                        string sRow = sName.Substring(1, 1);
                        Check(IsNumeric(sRow), $"Invalid row in '{sName}'");
                        ld.row = atoi(sRow);

                        string sCol = sName.Substring(2, 1);
                        Check(IsNumeric(sCol), $"Invalid column in '{sName}'");
                        ld.col = atoi(sCol);

                        char cProduct = sName.Substring(3, 1)[0];
                        Check((cProduct >= 'a') && (cProduct <= 'z'), $"Invalid product index in '{sName}'");
                        ld.product = 1 + (cProduct - 'a');

                        Check(!dicLit.ContainsKey(id), $"ID {id} already present in dicLit");
                        dicLit.Add(id, ld);
                    }
                }
            }
        }

        private static void ReadAkBooleSolution(string fileName, bool revisedMode)
        {
            string mode = revisedMode ? "revised " : "";
            int litCount = 0;

            o($"Extracting literal values");

            using StreamReader sr = OpenReader(fileName, $"akBoole {mode}solution");
            while (!sr.EndOfStream)
            {
                string s = sr.ReadLine();

                if (s.Contains(" = ") && !s.StartsWith("[") && !s.StartsWith("Variables:") &&
                   (s.EndsWith("0") || s.EndsWith("1")))
                {
                    ReadLiteralValue(s, revisedMode);
                    litCount++;
                }
            }

            Check(litCount > 0, $"Solution file '{fileName}' does not contain any literals");
            o($"Literals in solution file '{fileName}': {litCount}");
        }

        private static void ReadLiteralValuesBini(string fileName)
        {
            int aRows = undefined;
            int aCols = undefined;
            int bRows = undefined;
            int bCols = undefined;
            int cRows = undefined;
            int cCols = undefined;
            int products = undefined;
            char[] semicolon = [';'];

            o($"Extracting variable value assignments");

            using StreamReader sr = OpenReader(fileName, "Bini-format solution");
            while (!sr.EndOfStream)
            {
                string s = sr.ReadLine();

                if (s.StartsWith("#") || (s.Length < 2))
                {
                    continue;
                }
                else if (s.StartsWith("Bini", StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] arr = Tokenize(s);

                    Check(arr.Length == 5, "Invalid Bini line! 4 fields expected: " + s);

                    aRows = atoi(arr[1]);
                    aCols = atoi(arr[2]);
                    bRows = aCols;
                    bCols = atoi(arr[3]);
                    cRows = aRows;
                    cCols = bCols;
                    products = atoi(arr[4]);
                }
                else if (s.StartsWith("product", StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] arr = Tokenize(s);

                    Check(arr.Length == 4, "Invalid product line! 4 fields expected: " + s);
                    Check(arr[1] == "Gamma", "Gamma expected as field 2: " + s);
                    Check(arr[2] == "Alpha", "Alpha expected as field 3: " + s);
                    Check(arr[3] == "Beta", "Beta expected as field 4: " + s);
                }
                else if (s.Contains(";"))
                {
                    string[] arr = Tokenize(s, semicolon);

                    Check((aRows != undefined) && (aCols != undefined) && (bCols != undefined) && (products != undefined),
                        "Missing/inconsistent Bini line!");

                    Check(arr.Length == 4, "Value line with 4 sections expected: " + s);

                    int k = atoi(arr[0].Trim());
                    Check((products != undefined) && (k > 0) && (k <= products), "Inconsistent product index: " + s);

                    string[] v = Tokenize(arr[1]);
                    Check(v.Length == cRows * cCols, "Inconsistent number of Gamma values: " + s);
                    for (int row = 0; row < cRows; row++)
                        for (int col = 0; col < cCols; col++)
                        {
                            litArrayD[row + 1, col + 1, k] = atoi(v[row * bCols + col]);
                        }

                    v = Tokenize(arr[2]);
                    Check(v.Length == aRows * aCols, "Inconsistent number of Alpha values: " + s);
                    for (int row = 0; row < aRows; row++)
                        for (int col = 0; col < aCols; col++)
                        {
                            litArrayF[row + 1, col + 1, k] = atoi(v[row * aCols + col]);
                        }

                    v = Tokenize(arr[3]);
                    Check(v.Length == bRows * bCols, "Inconsistent number of Beta values: " + s);
                    for (int row = 0; row < bRows; row++)
                        for (int col = 0; col < bCols; col++)
                        {
                            litArrayG[row + 1, col + 1, k] = atoi(v[row * bCols + col]);
                        }
                }
                else
                {
                    Fatal($"Unexpected line: '{s}'");
                }
            }
        }

        private static void WriteBiniFile(string fileName)
        {
            if (fileName == "")
            {
                return;
            }

            using (StreamWriter sw = OpenWriter(fileName, "Bini-format solution"))
            {
                string s = (algorithmMode == AlgorithmMode.Mod2Brent) ? "(mod 2!) " : "";

                fOut = sw;

                o("#");
                o("# File '" + fileName + "'");
                o("#");
                o("# Solution for the " + Signature() + $" matrix multiplication problem in Bini matrix {s}format");
                o("#");
                o("# Created: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss"));
                o("#");
                o($"Bini {aRows} {aCols} {bCols} {noOfProducts}");
                o("");
                s = "product " +
                    "Gamma".PadRight(cRows * cCols * 3 + 1, ' ') + " " +
                    "Alpha".PadRight(aRows * aCols * 3 + 1, ' ') + " " +
                    "Beta";
                o(s);

                //  arrays in order C A B rather than A B C
                int[] rows = [cRows, aRows, bRows];
                int[] cols = [cCols, aCols, bCols];
                //  arrays in order D F G rather than F G D
                DynArray3D<int>[] arr = [litArrayD, litArrayF, litArrayG];

                foreach (int k in Products)
                {
                    s = k.ToString().PadLeft(3, ' ');

                    for (int dfg = 0; dfg < 3; dfg++)
                    {
                        s = s + " ;" + (dfg == 0 ? " " : "");
                        for (int row = 1; row <= rows[dfg]; row++)
                            for (int col = 1; col <= cols[dfg]; col++)
                            {
                                int val = arr[dfg][row, col, k];

                                s += val.ToString().PadLeft(3, ' ');
                            }
                    }

                    o(s);
                }
                o("");
                o("#");
            }

            fOut = null;
        }

        private static void WriteTensorFile(string fileName)
        {
            if (fileName == "")
            {
                return;
            }

            using (StreamWriter sw = OpenWriter(fileName, "tensor-format solution"))
            {
                fOut = sw;

                o("#");
                o("# File '" + fileName + "'");
                o("#");
                o("# Solution for the " + Signature() + $" matrix multiplication problem in tensor {(algorithmMode == AlgorithmMode.Mod2Brent ? "(mod 2!) " : "")}format");
                o("# Transposed form C = (A*B)^T");
                o("#");
                o("# Created: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss"));
                o("#");

                foreach (int k in Products)
                {
                    string s = "";
                    string sep = "";

                    foreach (int fgd in Fgd_all)
                    {
                        string t;

                        if (literalName[fgd] == "D")
                        {
                            //  Cpq is expressed in transposed form
                            t = GetTerm(fgd, k, transposed: true);
                        }
                        else
                        {
                            t = GetTerm(fgd, k);
                        }

                        if (!t.StartsWith("("))
                        {
                            t = $"({t})";
                        }

                        s += sep + t;
                        sep = "*";
                    }

                    o(s);
                }
                o("");
                o("#");
            }
            fOut = null;
        }

        private static void ReadLiteralValuesYacas(string fileName)
        {
            o($"Extracting variable value assignments");

            yacasInputFileName = fileName;
            using StreamReader sr = OpenReader(fileName, "Yacas-format solution");
            while (!sr.EndOfStream)
            {
                string s = sr.ReadLine().Trim().ToLower();

                if (s.StartsWith("#") || (s.Length < 2))
                {
                    continue;
                }
                else if (s.StartsWith("p"))
                {
                    ReadYacasProduct(s);
                }
                else if (s.StartsWith("c"))
                {
                    ReadYacasSumOfProducts(s);
                }
                else if (s.StartsWith("simplify"))
                //  ignore Simplify()
                {
                    continue;
                }
                else
                {
                    Fatal($"Unexpected line: '{s}'");
                }
            }
        }

        private static void ReadLiteralValuesTensor(string fileName)
        {
            int product = 0;

            o($"Extracting variable value assignments");

            using StreamReader sr = OpenReader(fileName, "Tensor-format solution");
            while (!sr.EndOfStream)
            {
                string s = sr.ReadLine().Trim().ToLower();

                if (s.StartsWith("#") || (s.Length < 2))
                {
                    continue;
                }
                else if (s.StartsWith("("))
                //  read polynome
                {
                    ReadTensorLine(s, ++product);
                }
                else
                {
                    Fatal($"Unexpected line: '{s}'");
                }
            }

            Check(product > 0, "No products found");
        }

        private static void ReadYacasSumOfProducts(string s)
        {
            s = s.Replace(" ", "").Replace(":", "").Replace(";", "") + " ";
            int row = s[1] - '0';
            int col = s[2] - '0';
            int eq = s.IndexOf('=');
            int sign = +1;
            int product = 0;
            ParseState state = ParseState.started;
            int pos = eq;

            Check(eq > 0, "Yacas sum-of-products without '=': " + s);

            while (state != ParseState.finished)
            {
                char c = s[++pos];
                switch(state)
                {
                    case ParseState.started:
                        if (c == '+')
                        {
                            sign = +1;
                            state = ParseState.name;
                        }
                        else if (c == '-')
                        {
                            sign = -1;
                            state = ParseState.name;
                        }
                        else if (c == 'p')
                        {
                            state = ParseState.row;
                        }
                        else if (c == ' ')
                        {
                            state = ParseState.finished;
                        }
                        else
                        {
                            Fatal($"Unexpected char in Yacas sum: {c}");
                        }
                        break;
                    case ParseState.name:
                        Check(c == 'p', "Unexpected char {c} != p in Yacas sum-of-products");
                        product = 0;
                        state = ParseState.row;
                        break;
                    case ParseState.row:
                        if ("0123456789".IndexOf(c) >= 0)
                        {
                            product = 10 * product + (c - '0');
                        }
                        else
                        {
                            litArrayD[row, col, product] = sign;
                            goto case ParseState.started;
                        }
                        break;
                    default:
                        Fatal("Unexpected parse state in Yacas sum-of-products");
                        break;
                }
            }
        }

        private static void ReadYacasProduct(string s)
        {
            int eq = s.IndexOf('=');
            int times = s.IndexOf('*');

            Check(eq > 0, "No '=' in Yacas product line: " + s);
            Check(times > 0, "No '*' in Yacas product line: " + s);

            string sp = s.Substring(0, eq).Replace(":", "").Replace(" ", "").Replace("p", "");

            Check(IsNumeric(sp), "Numeric product identifier expected: " + s);

            int k = atoi(sp);
            Check((k > 0) && (k < 999), "Unexpected product index outside [1..999]: " + s);

            string sa = s.Substring(eq + 1, times - eq - 1);
            ReadYacasSum(sa, k, 'a', litArrayF);

            string sb = s.Substring(times + 1);
            ReadYacasSum(sb, k, 'b', litArrayG);
        }

        private static Regex rg = null;
        private static string[] GetABCOps(string s)
        {
            string[] a = null;

            if (rg == null)
            {
                //  two alternatives:
                //  either a single literal with optional +/- sign
                //  or a term in (..)
                string pattern = @"([+-]?[a-c]\d\d|\([^)]+\))";
                rg = new Regex(pattern);
            }

            //  https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.match?view=net-9.0
            Match m = rg.Match(s);
            List<string> list = [];
            while (m.Success)
            {
                Group g = m.Groups[1];
                list.Add(g.Value);
                m = m.NextMatch();
            }

            if (list.Count == 3)
            {
                a = [.. list];
            }

            return a;
        }

        private static void ReadTensorLine(string s, int product)
        {
            string[] sarr;
            string tpl = " tensor product line: " + s;

            //  count '*'
            int asterisks = s.Count(c => c == '*');
            int opens = s.Count(c => c == '(');
            int closes = s.Count(c => c == ')');

            if (asterisks == 2)
            {
                sarr = s.Split(['*'], StringSplitOptions.RemoveEmptyEntries);
                Check(sarr.Length == 3, "Unexpected" + tpl);
            }
            else
            //  format without '*'
            {
                Check(asterisks == 0, "Expected 0 or 2 '*'" + tpl);
                Check(opens == closes, "Unbalanced brackets" + tpl);

                sarr = GetABCOps(s);

            }

            Check((product > 0) && (product < 999), "Unexpected product index outside [1..999]: " + s);

            ReadYacasSum(sarr[0], product, 'a', litArrayF);

            ReadYacasSum(sarr[1], product, 'b', litArrayG);

            ReadYacasSum(sarr[2], product, 'c', litArrayD, transposed: true);
        }

        private static void ReadYacasSum(string s, int product, char litName, DynArray3D<int> litArray, 
                                         bool transposed = false)
        {
            ParseState state = ParseState.started;
            int pos = -1;
            int row = 0;
            int sign = +1;

            s = s.Replace("(", "").Replace(")", "").Replace(" ", "").Replace(";", "");

            while (state != ParseState.finished)
            {
                char c = s[++pos];
                switch (state)
                {
                    case ParseState.started:
                        if (c == '+')
                        {
                            sign = +1;
                            state = ParseState.name;
                        }
                        else if (c == '-')
                        {
                            sign = -1;
                            state = ParseState.name;
                        }
                        else if (c == litName)
                        {
                            state = ParseState.row;
                        }
                        else
                        {
                            Fatal($"Unexpected char in Yacas sum: {c}");
                        }
                        break;
                    case ParseState.name:
                        Check(c == litName, $"Unexpected char {c} != {litName} in Yacas sum");
                        state = ParseState.row;
                        break;
                    case ParseState.row:
                        Check("0123456789".IndexOf(c) >= 0, "Expected digit in Yacas sum: " + c);
                        row = (int)c - (int)'0';
                        state = ParseState.col;
                        break;
                    case ParseState.col:
                        Check("0123456789".IndexOf(c) >= 0, "Expected digit in Yacas sum: " + c);
                        int col = (int)c - (int)'0';
                        if (transposed)
                        {
                            litArray[col, row, product] = sign;
                        }
                        else
                        {
                            litArray[row, col, product] = sign;
                        }
                        state = (pos == s.Length - 1) ? ParseState.finished : ParseState.started;
                        break;
                    default:
                        Fatal("Unexpected parse state in Yacas sum");
                        break;
                }
            }
        }

        private static void ReadLiteralValuesGringo(string fileName)
        {
            o($"Extracting variable value assignments");

            using StreamReader sr = OpenReader(fileName, "Gringo solution");
            while (!sr.EndOfStream)
            {
                string s = sr.ReadLine();

                if (s.Equals("Answer: 1"))
                {
                    s = sr.ReadLine();
                    string[] a = Tokenize(s);

                    foreach (string v in a)
                    {
                        Assert((v.Length == "f(1,1,1)".Length) || (v.Length == "f(1,1,11)".Length));
                        Assert(v[1] == '(');
                        Assert(v.EndsWith(")"));
                        Assert("fgd".IndexOf(v[0]) >= 0);

                        int row = v[2] - '0';
                        int col = v[4] - '0';
                        string t = v.Replace(")", "").Substring(6);

                        int k = atoi(t);

                        switch (v[0])
                        {
                            case 'f': litArrayF[row, col, k] = 1; break;
                            case 'g': litArrayG[row, col, k] = 1; break;
                            case 'd': litArrayD[row, col, k] = 1; break;
                        }
                    }

                    s = sr.ReadLine();
                    Check(s == "SATISFIABLE", $"SATISFIABLE expected. Got {s}");

                    break;
                }
            }
        }

        /// <summary>
        /// Return literal name
        /// </summary>
        /// <param name="name">Matrix name a, b, c</param>
        /// <param name="row">Row (1-based)</param>
        /// <param name="col">Column (1-based)</param>
        /// <param name="product">Product index (1-based)</param>
        /// <returns>The literal name</returns>
        static string Literal(string name, int row, int col, int product)
        {
            if (((noOfProducts == 0) || (noOfProducts < 53)) && (product < 53))
            {
                //                                       1111111111222222222233333333334444444444555
                //                              1234567890123456789012345678901234567890123456789012
                return name + row + "" + col + "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"[product - 1];
            }
            else
            {
                return name + row + "" + col + "_" + product.ToString("000");
            }
        }

        static void ReadLiteralValue(string s, bool revisedMode = false)
        {
            string[] a = Tokenize(s);

            Check((a.Length == 3) && a[1].Equals("="), 
                  $"3 fields with '=' as 2nd expected: '{s}'");
            Check((a[0].Length == 4) || (a[0].Length == 7), 
                  $"Literal name expected with 4 or 7 chars: '{s}'");

            int row = atoi(a[0].Substring(1, 1));
            int col = atoi(a[0].Substring(2, 1));
            int k;

            if (a[0].Length == 4)
            {
                char product = a[0][3];
                //            1111111111222222222233333333334444444444555
                //   1234567890123456789012345678901234567890123456789012
                k = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(product) + 1;
                Check(k != 0, $"Invalid product '{product}'");
            }
            else
            {
                string product = a[0].Substring(4, 3);
                if (!int.TryParse(product, out k))
                {
                    Fatal($"Invalid literal product index {a[0]}: {s}");
                }
            }

            string v = a[2];
            Check((v == "0") || (v == "1") || (v == "-1"), 
                  $"Unexpected value {v} for {a[0]}. 0, 1 or -1 expected");
            int val = atoi(v);

            char c1 = char.ToUpper(a[0][0]);

            if (!revisedMode)
            {
                if (c1 == 'F')
                {
                    Check(litArrayF[row, col, k] == undefined, 
                          "Multiple definitions for " + Literal("F", row, col, k));
                    litArrayF[row, col, k] = val;
                    Check(litArrayF[row, col, k] == val, 
                          "Cannot store literal value F");
                    Debug.WriteLine($"F{row}{col}{k} = {val}");
                }
                else if (c1 == 'G')
                {
                    Check(litArrayG[row, col, k] == undefined, 
                          "Multiple definitions for " + Literal("G", row, col, k));
                    litArrayG[row, col, k] = val;
                    Check(litArrayG[row, col, k] == val, 
                          "Cannot store literal value G");
                    Debug.WriteLine($"G{row}{col}{k} = {val}");
                }
                else if (c1 == 'D')
                {
                    Check(litArrayD[row, col, k] == undefined, 
                          "Multiple definitions for " + Literal("D", row, col, k));
                    litArrayD[row, col, k] = val;
                    Check(litArrayD[row, col, k] == val, 
                          "Cannot store literal value D");
                    Debug.WriteLine($"D{row}{col}{k} = {val}");
                }
            }
            else
            {
                Check((val == 0) || (val == 1), 
                      $"Inconsistent value {val} for revised solution. 0 or 1 expected");

                if (c1 == 'F')
                {
                    Check(litArrayF[row, col, k] != undefined, 
                          "Missing mod2 definition for " + Literal("F", row, col, k));
                    val = litArrayF[row, col, k] * (2 * val - 1);
                    litArrayF[row, col, k] = val;
                    Check(litArrayF[row, col, k] == val, 
                          "Cannot store literal value F");
                    Debug.WriteLine($"F{row}{col}{k} = {val}");
                }
                else if (c1 == 'G')
                {
                    Check(litArrayG[row, col, k] != undefined, 
                          "Missing mod2 definition for " + Literal("G", row, col, k));
                    val = litArrayG[row, col, k] * (2 * val - 1);
                    litArrayG[row, col, k] = val;
                    Check(litArrayG[row, col, k] == val, 
                          "Cannot store literal value G");
                    Debug.WriteLine($"G{row}{col}{k} = {val}");
                }
                else if (c1 == 'D')
                {
                    Check(litArrayD[row, col, k] != undefined, 
                          "Missing mod2 definition for " + Literal("D", row, col, k));
                    val = litArrayD[row, col, k] * (2 * val - 1);
                    litArrayD[row, col, k] = val;
                    Check(litArrayD[row, col, k] == val, 
                          "Cannot store literal value D");
                    Debug.WriteLine($"D{row}{col}{k} = {val}");
                    Check(!transposedMode || (row <= col), 
                          $"Inconsistent element {s} for transposed mode");
                }
            }

        }

        static void WriteBrentEquations(string fileName)
        {
            int errors = 0;

            if ((fileName == "") || (fileName == "none"))
            {
                o("No Brent equation file created");
                return;
            }

            o("Writing Brent equations");

            using (StreamWriter sw = OpenWriter(fileName, "Brent equations"))
            {
                string s;
                int correctEquations = 0;
                int[] productOrder = DetermineProductSortOrder();
                int[] nonZeroOddTriples = new int[noOfProducts];
                int[] nonZeroEvenTriples = new int[noOfProducts];
                int colWidth = (algorithmMode == AlgorithmMode.FullBrent) ? 11 : 6;

                fOut = sw;

                //  write header

                for (int phase = 0; phase < 2; phase++)
                {
                    s = "       ";

                    foreach (int k in Products)
                    {
                        if (phase == 0)
                        {
                            int t = productOrder[k - 1];

                            s += (" ".PadLeft(colWidth / 2, ' ') + t).PadRight(colWidth, ' ');
                        }
                        else
                        {
                            s += "-".PadRight(colWidth, '-');
                        }
                    }

                    o(s);
                }

                //  write odd equations in phase 0, even equations in phase 1
                for (int phase = 0; phase < 2; phase++)
                {
                    for (int ra = 1; ra <= aRows; ra++)
                        for (int ca = 1; ca <= aCols; ca++)
                            for (int rb = 1; rb <= bRows; rb++)
                                for (int cb = 1; cb <= bCols; cb++)
                                    for (int rc = 1; rc <= cRows; rc++)
                                        for (int cc = 1; cc <= cCols; cc++)
                                        {
                                            bool odd = (ra == rc) && (ca == rb) && (cc == cb);
                                            string sep = "";

                                            if (transposedMode && (rc > cc))
                                            {
                                                continue;
                                            }

                                            if ((phase == 0) == odd)
                                            {
                                                s = $"{ra}{ca}{rb}{cb}{rc}{cc}: ";
                                                int sum = 0;
                                                int nonZeroTriples = 0;

                                                foreach (int k in Products)
                                                {
                                                    int t = productOrder[k - 1];

                                                    int F = litArrayF[ra, ca, t];
                                                    int G = litArrayG[rb, cb, t];
                                                    int D = litArrayD[rc, cc, t];
                                                    int T = F * G * D;

                                                    s += sep;
                                                    sep = "+";

                                                    if (algorithmMode == AlgorithmMode.FullBrent)
                                                    {
                                                        string fgd = SignedIntString(F) + "*" 
                                                                   + SignedIntString(G) + "*" 
                                                                   + SignedIntString(D);

                                                        s += (T != 0) ? $"[{fgd}]" : $" {fgd} ";
                                                    }
                                                    else
                                                    {
                                                        s += (T != 0) ? "[111]" : $" {F}{G}{D} ";
                                                    }

                                                    if (T != 0)
                                                    {
                                                        nonZeroTriples++;

                                                        if (odd)
                                                        {
                                                            nonZeroOddTriples[k - 1]++;
                                                        }
                                                        else
                                                        {
                                                            nonZeroEvenTriples[k - 1]++;
                                                        }
                                                    }

                                                    sum += T;
                                                }

                                                s += " = " + sum;

                                                if (algorithmMode == AlgorithmMode.FullBrent)
                                                {
                                                    if ((sum == 1) != odd)
                                                    {
                                                        s += " != " + (odd ? "1" : "0");
                                                        errors++;
                                                    }
                                                    else
                                                    {
                                                        correctEquations++;
                                                    }
                                                }
                                                else
                                                {
                                                    if (((sum % 2) == 1) != odd)
                                                    {
                                                        s += " != " + (odd ? "1" : "0");
                                                        errors++;
                                                    }
                                                    else
                                                    {
                                                        correctEquations++;
                                                    }
                                                }

                                                if (nonZeroTriples > 0)
                                                {
                                                    s += " " + nonZeroTriples + "x";
                                                }

                                                o(s);
                                            }
                                        }
                }

                //  write footer

                for (int phase = 0; phase < 3; phase++)
                {
                    s = "       ";

                    foreach (int k in Products)
                    {
                        if (phase == 0)
                        {
                            s += "-".PadRight(colWidth, '-');
                        }
                        else if (phase == 1)
                        {
                            int t = nonZeroOddTriples[k - 1];

                            s += (" ".PadLeft(colWidth / 2, ' ') + t).PadRight(colWidth, ' ');
                        }
                        else if (phase == 2)
                        {
                            int t = nonZeroEvenTriples[k - 1];

                            s += (" ".PadLeft(colWidth / 2, ' ') + t).PadRight(colWidth, ' ');
                        }
                    }

                    o(s);
                }

                o("");
                if (errors > 0)
                {
                    o("Errors: " + errors);
                }
                o("Correct equations: " + correctEquations);

            }

            fOut = null;

            o("");
            o($"Brent equations written to '{fileName}'");
            o((errors == 0) ? "verified OK!" : "verification failed!!!");
            o("");
        }

        private static string SignedIntString(int f)
        {
            return ((f < 0) ? "" : " ") + f;
        }

        /// <summary>
        /// Find an ordering for products such that the odd triple vectors are sorted
        /// </summary>
        /// <returns>Array of product indices</returns>
        private static int[] DetermineProductSortOrder()
        {
            int[] order = new int[noOfProducts];
            string[] vector = new string[noOfProducts];

            foreach (int k in Products)
            {
                order[k - 1] = k;
                vector[k - 1] = GetOddTripleVectorAsString(k);
            }

            //  sort order
            bool sorted;

            do
            {
                sorted = true;

                for (int i = 0; i < order.Length - 1; i++)
                {
                    if (vector[i].CompareTo(vector[i + 1]) < 0)
                    {
                        (order[i + 1], order[i]) = (order[i], order[i + 1]);
                        (vector[i + 1], vector[i]) = (vector[i], vector[i + 1]);
                        sorted = false;
                    }
                }
            } while (!sorted);

            return order;
        }

        /// <summary>
        /// Scan through all odd equations and return a string of "0" and "1" for product column k.
        /// "0" indicates a zero-triple, "1" indicates a non-zero triple (might be negative though)
        /// </summary>
        /// <param name="k">The 1-based product column</param>
        /// <returns>The string</returns>
        private static string GetOddTripleVectorAsString(int k)
        {
            string s = "";

            for (int ra = 1; ra <= aRows; ra++)
                for (int ca = 1; ca <= aCols; ca++)
                    for (int cb = 1; cb <= bCols; cb++)
                    {
                        int F = litArrayF[ra, ca, k];
                        int G = litArrayG[ca, cb, k];
                        int D = litArrayD[ra, cb, k];

                        //  we ignore the sign
                        s += (F * G * D == 0) ? '0' : '1';
                    }

            Check(s.Length == aRows * aCols * bCols, "Unexpected vector length");
            return s;
        }

        private static void WriteGraphvizDetailFile(string graphvizFileName)
        {
            if (graphvizFileName == "")
            {
                return;
            }

            o("Writing Brent equation detail graph file");

            using (StreamWriter sw = OpenWriter(graphvizFileName, "GraphViz script"))
            {
                fOut = sw;

                o("graph BrentGraph {");

                o("size=\"8,12\";");
                o("ratio=\"fill\";");
                o("orientation=landscape;");
                o("dpi=1200;");
                o("fontsize=44;");
                o("");
                o("node [margin=0 fontcolor=black fontsize=28 ];");

                o("/*  --------------------  one cluster per product --------------------  */");
                foreach (int k in Products)
                {
                    int oddTriples = 0;
                    int evenTriples = 0;

                    o("subgraph cluster_" + k.ToString().PadLeft(2, '0') + " {");
                    o("  style=filled;");
                    o("  color=lightgrey;");
                    o($"  label=\"#{k} {(char)('a' + k - 1)}\";");

                    for (int phase = 0; phase < 2; phase++)
                        for (int ra = 1; ra <= aRows; ra++)
                            for (int ca = 1; ca <= aCols; ca++)
                                for (int rb = 1; rb <= bRows; rb++)
                                    for (int cb = 1; cb <= bCols; cb++)
                                        for (int rc = 1; rc <= cRows; rc++)
                                            for (int cc = 1; cc <= cCols; cc++)
                                            {

                                                int F = litArrayF[ra, ca, k];
                                                int G = litArrayG[rb, cb, k];
                                                int D = litArrayD[rc, cc, k];

                                                if (F * G * D != 0)
                                                {
                                                    bool odd = (ra == rc) && (ca == rb) && (cc == cb);
                                                    //  show odd triples first
                                                    if (odd == (phase == 0))
                                                    {
                                                        char c = ProductChar(k);
                                                        string t = $"t{ra}{ca}{rb}{cb}{rc}{cc}{c}";
                                                        string label = odd ? $"{ra}{ca}{cb}" : $"{ra}{ca}{rb}{cb}{rc}{cc}";
                                                        string shape = odd ? "diamond,color=red" : "box";
                                                        o($"  {t}[shape={shape},label=\"{label}\"];");
                                                        oddTriples += odd ? 1 : 0;
                                                        evenTriples += odd ? 0 : 1;
                                                    }
                                                }
                                            }
                    o($"  /*  {oddTriples + evenTriples} triples, {oddTriples} odd, {evenTriples} even  */");
                    o("}");
                    o("");
                }

                //  count number of non-zero triples per product
                int[] nzCount = new int[noOfProducts];

                for (int ra = 1; ra <= aRows; ra++)
                    for (int ca = 1; ca <= aCols; ca++)
                        for (int rb = 1; rb <= bRows; rb++)
                            for (int cb = 1; cb <= bCols; cb++)
                                for (int rc = 1; rc <= cRows; rc++)
                                    for (int cc = 1; cc <= cCols; cc++)
                                        foreach (int k in Products)
                                        {
                                            int F = litArrayF[ra, ca, k];
                                            int G = litArrayG[rb, cb, k];
                                            int D = litArrayD[rc, cc, k];

                                            if (F * G * D != 0)
                                            {
                                                nzCount[k - 1]++;
                                            }
                                        }

                int[] kOrder = Range(1, noOfProducts);

                for (int i = 0; i < noOfProducts - 1; i++)
                    for (int j = i + 1; j < noOfProducts; j++)
                        if (nzCount[i] < nzCount[j])
                        {
                            int nzTemp = nzCount[i];
                            int orderTemp = kOrder[i];
                            nzCount[i] = nzCount[j];
                            kOrder[i] = kOrder[j];
                            nzCount[j] = nzTemp;
                            kOrder[j] = orderTemp;
                        }

                o("/*  --------------------  edges  --------------------------  */");

                for (int ra = 1; ra <= aRows; ra++)
                    for (int ca = 1; ca <= aCols; ca++)
                        for (int rb = 1; rb <= bRows; rb++)
                            for (int cb = 1; cb <= bCols; cb++)
                                for (int rc = 1; rc <= cRows; rc++)
                                    for (int cc = 1; cc <= cCols; cc++)
                                    {
                                        string prev = "";

                                        foreach (int k in kOrder)
                                        {
                                            //  products with many non-zero triples come first
                                            int F = litArrayF[ra, ca, k];
                                            int G = litArrayG[rb, cb, k];
                                            int D = litArrayD[rc, cc, k];

                                            if (F * G * D != 0)
                                            {
                                                char c = ProductChar(k);
                                                string t = $"t{ra}{ca}{rb}{cb}{rc}{cc}{c}";

                                                if (prev == "")
                                                {
                                                    prev = t;
                                                }
                                                else
                                                {
                                                    bool odd = (ra == rc) && (ca == rb) && (cb == cc);
                                                    string style = odd ? " [ penwidth=2.0, color=red ]"
                                                                       : " [ penwidth=0.75, color=black ]";
                                                    o($"  {prev} -- {t}{style}");
                                                }
                                            }
                                        }
                                    }

                o("}");
            }

            fOut = null;

            o("");
            o("Brent equation graph written to " + graphvizFileName);
        }

        private static void WriteGraphvizOverviewFile(string graphvizFileName)
        {
            if (graphvizFileName == "")
            {
                return;
            }

            //  record triples per product
            HashSet<string>[] evenTriples = new HashSet<string>[noOfProducts];
            HashSet<string>[] oddTriples = new HashSet<string>[noOfProducts];

            foreach (int k in Products)
            {
                evenTriples[k - 1] = [];
                oddTriples[k - 1] = [];
            }

            for (int ra = 1; ra <= aRows; ra++)
                for (int ca = 1; ca <= aCols; ca++)
                    for (int rb = 1; rb <= bRows; rb++)
                        for (int cb = 1; cb <= bCols; cb++)
                            for (int rc = 1; rc <= cRows; rc++)
                                for (int cc = 1; cc <= cCols; cc++)
                                    foreach (int k in Products)
                                    {

                                        int F = litArrayF[ra, ca, k];
                                        int G = litArrayG[rb, cb, k];
                                        int D = litArrayD[rc, cc, k];

                                            if (F * G * D != 0)
                                            {
                                                bool odd = (ra == rc) && (ca == rb) && (cc == cb);

                                                if (odd)
                                                {
                                                    string t = $"t{ra}{ca}{cb}";

                                                    oddTriples[k - 1].Add(t);
                                                }
                                                else
                                                {
                                                    string t = $"t{ra}{ca}{rb}{cb}{rc}{cc}";

                                                    evenTriples[k - 1].Add(t);
                                                }
                                            }
                                    }


            using (StreamWriter sw = OpenWriter(graphvizFileName, "GraphViz script"))
            {
                fOut = sw;

                o("graph BrentGraph {");
                o($"label=\"{graphvizFileName + " " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm")}\";");
                o("size=\"8,12\";");
                o("ratio=\"fill\";");
                o("orientation=portrait;");
                o("dpi=1200;");
                o("fontsize=44;");
                o("");
                o("node [margin=0 fontcolor=black fontsize=28 ];");
                o("edge [ color=black fontsize=24 ];");

                o("/*  --------------------  one cluster per product --------------------  */");
                foreach (int k in Products)
                {
                    int oddTripleCount = oddTriples[k - 1].Count;
                    int evenTripleCount = evenTriples[k - 1].Count;
                    string name = ProductName(k);

                    o("subgraph cluster_" + name + " {");
                    o("  style=filled;");
                    o("  color=bisque;");
                    o($"  label=\"{name}\";");
                    if (evenTripleCount > 0)
                    {
                        o($"  {name + "even"} [ label=\"{evenTripleCount} even\" shape=box color=black ]; ");
                    }
                    o($"  {name + "odd"} [ label=\"{oddTripleCount} odd\" shape=diamond color=red ]; ");
                    o($"  /*  {oddTripleCount + evenTripleCount} triples, {oddTripleCount} odd, {evenTripleCount} even  */");
                    o("}");
                    o("");
                }

                o("/*  --------------------  edges  --------------------------  */");

                for (int i = 0; i < noOfProducts - 1; i++)
                {
                    string piName = ProductName(i + 1);
                    for (int j = i + 1; j < noOfProducts; j++)
                    {
                        IEnumerable<string> evenLinks = evenTriples[i].Intersect(evenTriples[j]);
                        int evenCount = evenLinks.Count();
                        IEnumerable<string> oddLinks = oddTriples[i].Intersect(oddTriples[j]);
                        int oddCount = oddLinks.Count();
                        string pjName = ProductName(j + 1);

                        if (evenCount > 0)
                        {
                            string style = $" [ penwidth={(evenCount + 2) * 0.25} weight={evenCount} label={evenCount} ]";
                            o($"  {piName}even -- {pjName}even{style}");
                        }

                        if (oddCount > 0)
                        {
                            string style = $" [ penwidth={(oddCount + 2) * 0.25} weight={oddCount} label={oddCount} color=red fontcolor=red fontsize=24 ]";
                            o($"  {piName}odd -- {pjName}odd{style}");
                        }
                    }
                }

                o("}");
            }
            fOut = null;

            o("");
            o("Brent equation overview graph written to " + graphvizFileName);
        }

        private static char ProductChar(int k)
        {
            Check(k <= 52, "No of products > 52 NIY");

            //                1111111111222222222233333333334444444444555
            //       1234567890123456789012345678901234567890123456789012
            return " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"[k];
        }

        static void WriteYacasSolution(string[] args)
        {
            string fileName = "";
            string problem = $"s{Signature()}";

            if (args[0].Equals("--gringo") || args[0].Equals("--bini"))
            {
                fileName = (args.Length > 2) ? args[2] : $"{problem}.txt";
                brentEquationFileName = (args.Length > 3) ? args[3] : $"{problem}.brent.txt";
            }
            else if (args[0].Equals("--yacas"))
            {
                fileName = (args.Length > 2) ? args[2] : $"{problem}.new.txt";
                brentEquationFileName = (args.Length > 3) ? args[3] : $"{problem}.brent.txt";
            }
            else if (args[0].Equals("--cse"))
            {
                simplifiedSolutionFileName = (args.Length > 2) ? args[2] : $"{problem}.simplified.txt";
                brentEquationFileName = (args.Length > 3) ? args[3] : $"{problem}.brent.txt";
            }
            else if (args[0].Equals("--tensor"))
            {
                fileName = (args.Length > 2) ? args[2] : $"{problem}.new.txt";
                brentEquationFileName = (args.Length > 3) ? args[3] : $"{problem}.brent.txt";
            }
            else if (args[0].Equals("--graph"))
            {
                brentEquationFileName = "";
                graphvizDetailFileName = (args.Length > 2) ? args[2] : $"{problem}.gvdot";
                graphvizOverviewFileName = (args.Length > 3) ? args[3] : $"{problem}.overview.gvdot";
                productReportFileName = (args.Length > 4) ? args[4] : $"{problem}.products.txt";
            }
            else if (args[0].Equals("--sat"))
            {
                fileName = (args.Length > 3) ? args[3] : $"{problem}.txt";
                brentEquationFileName = (args.Length > 4) ? args[4] : $"{problem}.brent.txt";
            }
            else if (args[0].Equals("--reduce"))
            {
                brentEquationFileName = "";
                fileName = (args.Length > 3) ? args[3] : $"{problem}.reduced.txt";
            }
            else if (args[0].Equals("--binigen"))
            {
                biniFileName = (args.Length > 2) ? args[2] : $"{problem}.bini.txt";
                brentEquationFileName = (args.Length > 3) ? args[3] : $"{problem}.brent.txt";
                return;
            }
            else if (args[0].Equals("--tensorgen"))
            {
                tensorFileName = (args.Length > 2) ? args[2] : $"{problem}.tensor.txt";
                brentEquationFileName = (args.Length > 3) ? args[3] : $"{problem}.brent.txt";
                return;
            }
            else if (args[0].Equals("--literals"))
            {
                solutionFileName = (args.Length > 2) ? args[2] : $"{problem}.akb.txt";
                brentEquationFileName = (args.Length > 3) ? args[3] : $"{problem}.brent.txt";
                return;
            }
            else if (args[0].Equals("--contrib"))
            {
                productContributionsFileName = (args.Length > 2) ? args[2] : $"{problem}.contrib.txt";
                brentEquationFileName = (args.Length > 3) ? args[3] : $"{problem}.brent.txt";
                return;
            }
            else if (args[0].Equals("--combine"))
            {
                fileName = (args.Length > 3) ? args[3] : $"{problem}.txt";
                brentEquationFileName = (args.Length > 4) ? args[4] : $"{problem}.combined.brent.txt";
            }
            else
            {
                fileName = (args.Length > 1) ? args[1] : $"{problem}.txt";
                brentEquationFileName = (args.Length > 2) ? args[2] : $"{problem}.brent.txt";
            }

            if (fileName == "")
            {
                o("No Yacas script created");
                return;
            }

            bool bVerified = false;

            using (StreamWriter sw = OpenWriter(fileName, "Yacas script"))
            {
                string s;

                fOut = sw;

                o("#");
                o($"# Yacas script '{fileName}' created " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss"));
                o("#");
                o("# Matrix multiplication method "
                    + (algorithmMode == AlgorithmMode.Mod2Brent ? "(mod 2!) " : "")
                    + "for " + Signature());
                if (reducedSignature != "")
                {
                    o("#");
                    o("# Derived by omitting rows/columns");
                    o($"# Original file: {yacasInputFileName}");
                    o($"# Reduction signature: {reducedSignature}");
                    o("#");

                    foreach(string sr in reduceResult)
                    {
                        o($"# {sr}");
                    }
                }
                if (transposedMode)
                {
                    o("#");
                    o("# Transposed mode! Matrix factors are transposed to eachother.");
                    o("# Result matrix is symmetric.");
                }

                o("#");
                o($"# Intermediate products: {noOfProducts}");
                o("#");

                //  try to minimize the number of '-' signs
                //  BeautifyLiterals();

                foreach (int k in Products)
                {
                    string aTerm = GetTerm((int)Mat.F, k, autoQuote: true);
                    string bTerm = GetTerm((int)Mat.G, k, autoQuote: true);

                    s = $"{ProductName(k)} := {aTerm} * {bTerm};";

                    o(s);
                }

                o("");
                for (int row = 1; row <= cRows; row++)
                    for (int col = 1; col <= cCols; col++)
                        if (!transposedMode || (row <= col))
                        {
                            s = $"c{row}{col} := ";
                            s += GetSumOfProductsExtended(row, col);
                            s += ";";
                            o(s);
                        }

                o("");

                WriteYacasStatistics();

                WriteYacasSolutionFooter(fileName, ref bVerified);
            }
            fOut = null;

            o("");
            o($"Algorithm  written to file '{fileName}'");
            o(bVerified ? "verified OK!" : "verification failed!!!");
            o("");
        }

        private static void WriteYacasStatistics()
        {
            o("# Solution statistics:");
            o("# ====================");
            o("#");

            int dim = Fgd_all.Length;
            MinMax[] ops = MinMax.Array(dim);
            MinMax[] add_ops = MinMax.Array(dim);
            MinMax[] sub_ops = MinMax.Array(dim);
            MinMax total_ops = new();
            MinMax total_adds = new();
            MinMax total_subs = new();
            int[] term0_cnt = new int[dim];
            int[] term1_cnt = new int[dim];

            foreach (int fgd in Fgd_all)
                foreach (int k in Products)
                {
                    (int k_ops, int k_adds, int k_subs) = GetTermStatistics(fgd, k);
                    ops[fgd].Register(k_ops);
                    add_ops[fgd].Register(k_adds);
                    sub_ops[fgd].Register(k_subs);
                    total_ops.Register(k_ops);
                    total_adds.Register(k_adds);
                    total_subs.Register(k_subs);
                    if (k_ops == 1)
                    {
                        term1_cnt[fgd]++;
                    }
                    if (k_ops == 0)
                    {
                        term0_cnt[fgd]++;
                    }
                }

            const int a = (int)Mat.F;
            const int b = (int)Mat.G;
            const int c = (int)Mat.D;

            const int w = 10; // column width
            o($"#                 {"[a]",w} {"[b]",w} {"[c]",w} {"total",w}");
            o("# --------------------------------------------------------------");
            string sa = PrettyNum(ops[a].Sum);
            string sb = PrettyNum(ops[b].Sum);
            string sc = PrettyNum(ops[c].Sum);
            string st = PrettyNum(total_ops.Sum);
            o($"# operands         {sa,w} {sb,w} {sc,w} {st,w}");
            o($"# + operands       {add_ops[a].Sum,w} {add_ops[b].Sum,w} {add_ops[c].Sum,w} {total_adds.Sum,w}");
            o($"# - operands       {sub_ops[a].Sum,w} {sub_ops[b].Sum,w} {sub_ops[c].Sum,w} {total_subs.Sum,w}");
            o($"# term length      {ops[a].Range(),w} {ops[b].Range(),w} {ops[c].Range(),w} {total_ops.Range(),w}");
            if (term0_cnt.Sum() > 0)
            {
                o($"# empty terms      {term0_cnt[a],w} {term0_cnt[b],w} {term0_cnt[c],w} {term0_cnt.Sum(),w}"
                 + "  *** should not happen ***");
            }
            o($"# terms with 1 op  {term1_cnt[a],w} {term1_cnt[b],w} {term1_cnt[c],w} {term1_cnt.Sum(),w}");
            o();

            int[] nonZeroOddTriples = new int[noOfProducts];
            int[] nonZeroEvenTriples = new int[noOfProducts];
            MinMax eqOddTriples = new();
            MinMax eqEvenTriples = new();
            int eqOdd1_cnt = 0;
            int eqOdd_cnt = 0;
            int eqEven0_cnt = 0;
            int eqEven_cnt = 0;
            int product1_cnt = 0;

            for (int ra = 1; ra <= aRows; ra++)
                for (int ca = 1; ca <= aCols; ca++)
                    for (int rb = 1; rb <= bRows; rb++)
                        for (int cb = 1; cb <= bCols; cb++)
                            for (int rc = 1; rc <= cRows; rc++)
                                for (int cc = 1; cc <= cCols; cc++)
                                {
                                    bool odd = (ra == rc) && (ca == rb) && (cc == cb);

                                    if (transposedMode && (rc > cc))
                                    {
                                        continue;
                                    }

                                    int nonZeroTriples = 0;

                                    foreach (int k in Products)
                                    {
                                        int F = litArrayF[ra, ca, k];
                                        int G = litArrayG[rb, cb, k];
                                        int D = litArrayD[rc, cc, k];
                                        int T = F * G * D;

                                        if (T != 0)
                                        {
                                            nonZeroTriples++;

                                            if (odd)
                                            {
                                                nonZeroOddTriples[k - 1]++;
                                            }
                                            else
                                            {
                                                nonZeroEvenTriples[k - 1]++;
                                            }
                                        }
                                    }

                                    if (odd)
                                    {
                                        eqOddTriples.Register(nonZeroTriples);
                                        if (nonZeroTriples == 1)
                                        {
                                            eqOdd1_cnt++;
                                        }
                                        eqOdd_cnt++;
                                    }
                                    else
                                    {
                                        eqEvenTriples.Register(nonZeroTriples);
                                        if (nonZeroTriples == 0)
                                        {
                                            eqEven0_cnt++;
                                        }
                                        eqEven_cnt++;
                                    }
                                }

            MinMax oddTriples = new();
            MinMax evenTriples = new();

            foreach (int k in Products)
            {
                if (nonZeroOddTriples[k - 1] == 1)
                {
                    product1_cnt++;
                }
                oddTriples.Register(nonZeroOddTriples[k - 1]);
                evenTriples.Register(nonZeroEvenTriples[k - 1]);
            }

            o("#");
            o("# Non-zero triples (= products of three coefficients != 0)");
            o("# ----------------------------------------------------------");
            o("# (equations are 'odd' for (a_row == c_row) && (a_col == b_row) && (b_col == c_col))");
            o("#");
            o($"# Total number of equations      {PrettyNum(eqOdd_cnt + eqEven_cnt)}");
            o($"# Odd triples per equation       {eqOddTriples}");
            o($"# Odd equations                  {PrettyNum(eqOdd_cnt)}");
            o($"# Odd equations with 1 triple    {eqOdd1_cnt}");
            o($"# Even triples per equation      {eqEvenTriples}");
            o($"# Even equations                 {PrettyNum(eqEven_cnt)}");
            o($"# Even equations with 0 triples  {PrettyNum(eqEven0_cnt)}");
            o($"# Odd triples per product        {oddTriples}");
            o($"# Even triples per product       {evenTriples.Range()}");
            o($"# Products with 1 odd triple     {product1_cnt}");
            o("#");
        }

        private static string GetTerm(int fgd, int product, bool transposed = false, bool autoQuote = false)
        {
            return GetTerm(Rows[fgd], Cols[fgd], product, litArrays[fgd], 
                           literalName[fgd], coefficientName[fgd], 
                           transposed, autoQuote);
        }

        private static string GetTerm(int rows, int cols, int product, DynArray3D<int> litArray, 
                                      string literalName, string elementName, 
                                      bool transposed = false, bool autoQuote = true)
        {
            string term = "";

            for (int row = 1; row <= rows; row++)
                for (int col = 1; col <= cols; col++)
                {
                    int val = litArray[row, col, product];

                    Check(val != undefined, 
                          Literal(literalName, row, col, product) + " is undefined");

                    if (val != 0)
                    {
                        Check(val * val == 1, 
                              Literal(literalName, row, col, product) + " has strange value");

                        if (val < 0)
                        {
                            term += "- ";
                        }
                        else if (term.Length > 0)
                        {
                            term += "+ ";
                        }

                        term += transposed ? $"{elementName}{col}{row} " : $"{elementName}{row}{col} ";
                    }
                }

            term = term.Trim();
            if (autoQuote && (term.Contains("+") || term.Contains("-")))
            {
                term = $"({term})";
            }

            return term;
        }



        /// <summary>
        /// Count number of non-zero elements in term
        /// </summary>
        /// <param name="k">Product index</param>
        /// <param name="rowSet">allowed rows</param>
        /// <param name="colSet">allowed columns</param>
        /// <param name="litArray">coefficient arary</param>
        /// <param name="transpose">access array transposed</param>
        /// <returns>number of non-zero elements</returns>
        private static int GetTermArity(int k, int[] rowSet, int[] colSet, DynArray3D<int> litArray, bool transpose = false)
        {
            int arity = 0;

            foreach (int row in rowSet)
                foreach (int col in colSet)
                    if (litArray[row, col, k] != 0)
                    {
                        arity++;
                    }

            return arity;
        }

        private static bool VerifyBrentEquations()
        {
            bool ret = true;
            int errCount = 0;
            int okCount = 0;
            MinMax evenMinMax = new();
            MinMax oddMinMax = new();
            MinMax colMinMax = new();
            int[] columnSums = new int[noOfProducts];
            int[] fk = new int[noOfProducts];
            int[] gk = new int[noOfProducts];

            for (int aRow = 1; aRow <= aRows; aRow++)
                for (int aCol = 1; aCol <= aCols; aCol++)
                {
                    foreach (int k in Products)
                    {
                        fk[k - 1] = litArrayF[aRow, aCol, k];
                    }
                    for (int bRow = 1; bRow <= bRows; bRow++)
                        for (int bCol = 1; bCol <= bCols; bCol++)
                        {
                            foreach (int k in Products)
                            {
                                gk[k - 1] = fk[k-1] * litArrayG[bRow, bCol, k];
                            }
                            for (int cRow = 1; cRow <= cRows; cRow++)
                                for (int cCol = 1; cCol <= cCols; cCol++)
                                {
                                    if (transposedMode && (cRow > cCol))
                                    {
                                        continue;
                                    }

                                    bool k1 = (aRow == cRow) && (aCol == bRow) && (bCol == cCol);
                                    string s = $"{aRow}{aCol}{bRow}{bCol}{cRow}{cCol}: ";

                                    int sum = 0;
                                    int noOfNonZeroTriples = 0;

                                    foreach (int k in Products)
                                    {
                                        int tr = gk[k-1] * litArrayD[cRow, cCol, k];

                                        if (tr == 0)
                                        {
                                            s += "  0 ";
                                        }
                                        else if (tr < 0)
                                        {
                                            s += $"[{tr}]";
                                            noOfNonZeroTriples++;
                                        }
                                        else
                                        {
                                            s += $" [{tr}]";
                                            noOfNonZeroTriples++;
                                        }

                                        if (k1 && (tr != 0))
                                        {
                                            columnSums[k - 1]++;
                                        }

                                        sum += tr;
                                    }

                                    (k1 ? oddMinMax : evenMinMax).Register(noOfNonZeroTriples);

                                    if (k1 == ((algorithmMode == AlgorithmMode.Mod2Brent ? (sum & 1) : sum) == 1))
                                    {
                                        okCount++;
                                    }
                                    else
                                    {
                                        s += k1 ? $" = {sum} != 1" : $" = {sum} != 0";
                                        o($"# {s}");
                                        ret = false;
                                        errCount++;
                                    }
                                }
                        }
                }

            foreach (int i in columnSums)
            {
                colMinMax.Register(i);
            }

            if (errCount > 0)
            {
                string msg = (algorithmMode == AlgorithmMode.Mod2Brent) ? " mod 2" : "";

                o($"# {errCount} equations are not fulfilled{msg}.");
                o($"# {okCount} equations are fulfilled{msg}.");
            }

            o("# Brent equation statistics about non-zero triples:");
            oddMinMax.Show("# in odd equations ");
            evenMinMax.Show("# in even equations");
            colMinMax.Show("# in kernel columns");
            o("");

            return ret;
        }

        static string Signature()
        {
            return $"{aRows}x{aCols}x{cCols}_{noOfProducts}";
        }

        static void Usage()
        {
            foreach (string s in usageStrings)
            {
                o(s);
            }

            Finish(0);
        }

        static string CSV<T>(IEnumerable<T> ie)
        {
            return string.Join(", ", ie.Select(i => i.ToString()));
        }

        static void WriteProductReportFile(string productReportFileName)
        {
            if (productReportFileName == "")
            {
                return;
            }

            using (StreamWriter sw = OpenWriter(productReportFileName, "product report"))
            {
                fOut = sw;
                o($"# Product report file '{productReportFileName}'");
                o("# Created: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
                o("");

                //  collect triple references
                //  each triple has a list of referencing product indices
                Dictionary<string, List<int>> refs = [];
                Dictionary<string, List<int>> oddRefs = [];
                int[] oddCount = new int[noOfProducts + 1];
                int[] evenCount = new int[noOfProducts + 1];

                for (int aRow = 1; aRow <= aRows; aRow++)
                    for (int bRow = 1; bRow <= bRows; bRow++)
                        for (int cRow = 1; cRow <= cRows; cRow++)
                            for (int aCol = 1; aCol <= aCols; aCol++)
                                for (int bCol = 1; bCol <= bCols; bCol++)
                                    for (int cCol = 1; cCol <= cCols; cCol++)
                                    {
                                        bool k1 = (aRow == cRow) && (aCol == bRow) && (bCol == cCol);
                                        string s = $"{aRow}{aCol}{bRow}{bCol}{cRow}{cCol}: ";

                                        foreach (int k in Products)
                                        {
                                            int tf = litArrayF[aRow, aCol, k];
                                            int tg = litArrayG[bRow, bCol, k];
                                            int td = litArrayD[cRow, cCol, k];
                                            int tr = tf * tg * td;

                                            if (tr != 0)
                                            {
                                                Dictionary<string, List<int>> r = k1 ? oddRefs : refs;

                                                if (!r.ContainsKey(s))
                                                {
                                                    r[s] = [];
                                                }
                                                r[s].Add(k);

                                                (k1 ? oddCount : evenCount)[k] += 1;
                                            }
                                        }
                                    }

                foreach (int k in Products)
                {
                    o($"Product {ProductName(k)}: {oddCount[k]} odd, {evenCount[k]} even triples");

                    string terms = GetTerm((int)Mat.F, k) + " " +
                                   GetTerm((int)Mat.G, k) + " " +
                                   GetTerm((int)Mat.D, k);
                    o($"Terms {terms}");

                    //  first odd, then even triples
                    for (int phase = 0; phase < 2; phase++)
                    {
                        Dictionary<string, List<int>> r = (phase == 0) ? oddRefs : refs;

                        //  loop through triples
                        foreach (string t in r.Keys)
                        {
                            if (r[t].Contains(k))
                            //  product is referencing current triple
                            {
                                //  the list of other product indices referencing this triple
                                string s = CSV(r[t].Where(i => i != k).Select(i => ProductName(i)));

                                if (s != "")
                                {
                                    s = " ==> " + s;
                                }
                                o($"{((phase == 0) ? "odd" : "   ")} {t} {s}");
                            }
                        }
                    }
                    o("");
                }

                o("");
                o("#");
                o($"# End of product report file '{productReportFileName}'");
                o("#");
            }

            fOut = null;
        }

        static void WriteProductContributionsFile(string productContributionsFileName)
        {
            if (productContributionsFileName == "")
            {
                return;
            }

            using (StreamWriter sw = OpenWriter(productContributionsFileName, "product contributions report"))
            {
                fOut = sw;
                o($"# Product contributions file '{productContributionsFileName}'");
                o("# Created: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
                o("");

                //  collect triple references
                //  each triple has a list of referencing product indices
                Dictionary<string, List<int>> refs = [];
                Dictionary<string, List<int>> oddRefs = [];
                int[] oddCount = new int[noOfProducts + 1];
                int[] evenCount = new int[noOfProducts + 1];

                for (int aRow = 1; aRow <= aRows; aRow++)
                    for (int bRow = 1; bRow <= bRows; bRow++)
                        for (int cRow = 1; cRow <= cRows; cRow++)
                            for (int aCol = 1; aCol <= aCols; aCol++)
                                for (int bCol = 1; bCol <= bCols; bCol++)
                                    for (int cCol = 1; cCol <= cCols; cCol++)
                                    {
                                        bool k1 = (aRow == cRow) && (aCol == bRow) && (bCol == cCol);
                                        string s = $"{aRow}{aCol}{bRow}{bCol}{cRow}{cCol}: ";

                                        foreach (int k in Products)
                                        {
                                            int tf = litArrayF[aRow, aCol, k];
                                            int tg = litArrayG[bRow, bCol, k];
                                            int td = litArrayD[cRow, cCol, k];
                                            int tr = tf * tg * td;

                                            if (tr != 0)
                                            {
                                                Dictionary<string, List<int>> r = k1 ? oddRefs : refs;

                                                if (!r.ContainsKey(s))
                                                {
                                                    r[s] = [];
                                                }
                                                r[s].Add(k);

                                                (k1 ? oddCount : evenCount)[k] += 1;
                                            }
                                        }
                                    }

                foreach (int k in Products)
                {
                    o($"Product {ProductName(k)}: {oddCount[k]} odd, {evenCount[k]} even triples");

                    //  collect coefficient matrices in lists of strings

                    List<string>[] lines = new List<string>[3];

                    foreach (int fgd in Fgd_all)
                    {
                        List<string> ll = [];
                        lines[fgd] = ll;
                        string bar = $"+{"".PadLeft(Cols[fgd], '-')}+";

                        ll.Add($" {literalName[fgd]}:{bar}");

                        for (int row = 1; row <= Rows[fgd]; row++)
                        {
                            string s = "";

                            for (int col = 1; col <= Cols[fgd]; col++)
                            {
                                int lit = litArrays[fgd][row, col, k];

                                s += "- +"[lit + 1];
                            }

                            ll.Add($"   |{s}|");
                        }

                        ll.Add($"   {bar}");
                    }

                    //  output matrices side-by-side

                    string line = "x";

                    for (int row = 0; line != ""; row++)
                    {
                        line = "";
                        foreach (int fgd in Fgd_all)
                        {
                            if (lines[fgd].Count > row)
                            {
                                line += lines[fgd][row];
                            }
                            else
                            {
                                line += "".PadLeft(lines[fgd].First().Length);
                            }
                            line += " ";
                        }
                        line = line.TrimEnd();
                        o(line);
                    }
                }

                o("");
                o("#");
                o($"# End of product contributions file '{productContributionsFileName}'");
                o("#");
            }

            fOut = null;
        }

        static private void ShowReduceResult(string s)
        {
            o(s);
            reduceResult.Add(s);
        }

        static void WriteReducedYacasSolution(string[] args)
        {
            if (args[0] != "--reduce")
            {
                return;
            }

            Check(!transposedMode, "--reduce not implemented for transpose mode");

            reducedSignature = (args.Length > 2) ? args[2] : $"{aRows - 1}x{aCols - 1}x{bCols - 1}";

            int noOfSelectedARows = GetSignaturePart(reducedSignature, 0);
            int noOfSelectedACols = GetSignaturePart(reducedSignature, 1);
            int noOfSelectedBCols = GetSignaturePart(reducedSignature, 2);

            Check((noOfSelectedARows > 0) && (noOfSelectedARows <= cRows), "Invalid signature A rows");
            Check((noOfSelectedACols > 0) && (noOfSelectedACols <= aCols), "Invalid signature A cols");
            Check((noOfSelectedBCols > 0) && (noOfSelectedBCols <= bCols), "Invalid signature B cols");

            o("Evaluating reduction options");

            int[] allARows = Range(1, aRows);
            int[] allACols = Range(1, aCols);
            int[] allBCols = Range(1, bCols);

            //  register the so far best solution
            int multsOpt = int.MaxValue;
            int addsOpt = 0;
            int[] selectedARowsOpt = null;
            int[] selectedAColsOpt = null;
            int[] selectedBColsOpt = null;
            int[] selectedProductsOpt = null;

            //  combinations are used to select the
            //  rows and columns which are mapped from the
            //  original matrix product to the reduced one

            IEnumerable<IEnumerable<int>> rowACombinations = allARows.Combinations(noOfSelectedARows);
            IEnumerable<IEnumerable<int>> colACombinations = allACols.Combinations(noOfSelectedACols);
            IEnumerable<IEnumerable<int>> colBCombinations = allBCols.Combinations(noOfSelectedBCols);

            foreach (IEnumerable<int> rowsA in rowACombinations)
            {
                int[] selectedARows = [.. rowsA];

                foreach (IEnumerable<int> colsA in colACombinations)
                {
                    int[] selectedACols = [.. colsA];

                    foreach (IEnumerable<int> colsB in colBCombinations)
                    {
                        int[] selectedBCols = [.. colsB];
                        int mults = 0;
                        int adds = 0;
                        HashSet<int> selectedProducts = [];
                        bool transpose = false;

                        foreach (int k in Products)
                        {
                            int aElements = GetTermArity(k, selectedARows, selectedACols, litArrayF, transpose);
                            int bElements = GetTermArity(k, selectedACols, selectedBCols, litArrayG, transpose);
                            int cElements = GetTermArity(k, selectedARows, selectedBCols, litArrayD, transpose);

                            //  a product is used unless A term or B term vanish,
                            //  or the product actually does not actually occur in any C element sum
                            if (aElements * bElements * cElements != 0)
                            {
                                mults++;
                                adds += (aElements - 1) + (bElements - 1);
                                selectedProducts.Add(k);
                            }
                        }

                        foreach (int row in selectedARows)
                            foreach (int col in selectedBCols)
                            {
                                //  one add less than the number of summands
                                adds--;
                                foreach (int k in Products)
                                {
                                    if (litArrayD[row, col, k] != 0)
                                    {
                                        adds++;
                                    }
                                }
                            }

                        if ((mults < multsOpt) ||
                             ((mults == multsOpt) && (adds < addsOpt)))
                        {
                            o($"improved to {mults} products, {adds} adds/substracts");
                            multsOpt = mults;
                            addsOpt = adds;
                            selectedARowsOpt = selectedARows;
                            selectedAColsOpt = selectedACols;
                            selectedBColsOpt = selectedBCols;
                            selectedProductsOpt = [.. selectedProducts];
                        }
                    }
                }
            }

            o("Evaluation complete");

            ShowReduceResult($"Best reduction solution requires {multsOpt} products and {addsOpt} add/subtract operations");
            ShowReduceResult($"Products dropped: {noOfProducts - multsOpt}");
            ShowReduceResult($"{selectedARowsOpt.Length} of {aRows} A rows selected: {string.Join(", ", selectedARowsOpt)}");
            ShowReduceResult($"{selectedAColsOpt.Length} of {aCols} A cols selected: {string.Join(", ", selectedAColsOpt)}");
            ShowReduceResult($"{selectedBColsOpt.Length} of {bCols} B cols selected: {string.Join(", ", selectedBColsOpt)}");

            if (reducedYacasFileName == "")
            {
                reducedYacasFileName = $"s{reducedSignature}_{multsOpt}.reduced.txt";
            }

            o($"Picking best solution");

            Array.Sort(selectedProductsOpt);
            Check(selectedProductsOpt.Length == multsOpt, "Unexpected selected products");
            ReduceLiteralArrays(selectedARowsOpt, selectedAColsOpt, selectedBColsOpt, selectedProductsOpt);
        }

      
        static void WriteSimplifiedYacasSolution(string simplifiedSolutionFileName)
        {
            if (simplifiedSolutionFileName == "")
            {
                return;
            }

            if (algorithmMode==AlgorithmMode.Mod2Brent)
            {
                Fatal("Common Subexpression Elimination is not supported for mod 2 algorithms");
            }
            PreparePrimeElementValidation(out Calculator calc, out Dictionary<string, double> dicName2Val);

            CommonSubexpressionEliminator fCse = CreateCommonSubexpressionEliminator(Mat.F, "fCSE");
            CommonSubexpressionEliminator gCse = CreateCommonSubexpressionEliminator(Mat.G, "gCSE");
            CommonSubexpressionEliminator dCse = CreateCommonSubexpressionEliminator(Mat.D, "dCSE");
            bool bVerified = false;

            int[] prevOpCounts = [fCse.OpCount, gCse.OpCount, dCse.OpCount];

            fCse.Simplify("Simplify (a) sums");
            gCse.Simplify("Simplify (b) sums");
            dCse.Simplify("Simplify sums of products");

            int[] opCounts = [fCse.OpCount, gCse.OpCount, dCse.OpCount];

            using (StreamWriter sw = OpenWriter(simplifiedSolutionFileName, "simplified Yacas script"))
            {
                string s;

                fOut = sw;

                WriteBlockComment($"Simplified Yacas script '{simplifiedSolutionFileName}' created "
                    + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") + "\n#",
                    "Matrix multiplication method "
                    + (algorithmMode == AlgorithmMode.Mod2Brent ? "(mod 2!) " : "")
                    + "for " + Signature());
                if (transposedMode)
                {
                    WriteBlockComment("Transposed mode! Matrix factors are transposed to eachother.",
                                      "# Result matrix is symmetric.");
                }

                int prevSum = prevOpCounts.Sum();
                int opcSum = opCounts.Sum();

                if (opcSum < prevSum)
                {
                    o($"# Operation count was reduced by {prevSum - opcSum} add/subtract operations:");
                }
                else
                {
                    o("# Operation count could not be reduced");
                }

                o("#");
                o("#         original  now");
                o($"# a terms: {prevOpCounts[0],4}  => {opCounts[0],3}");
                o($"# b terms: {prevOpCounts[1],4}  => {opCounts[1],3}");
                o($"# c terms: {prevOpCounts[2],4}  => {opCounts[2],3}");
                o("# ---------------------------");
                o($"# total:   {prevSum,4}  => {opcSum,3}");
                o("#");

                WriteBlockComment($"Intermediate products: {noOfProducts}");

                if (fCse.AuxExpressions.Any() || gCse.AuxExpressions.Any())
                {
                    WriteBlockComment("Auxiliary variables:");

                    WriteAuxiliaryAssignments(fCse, calc);
                    o();
                    WriteAuxiliaryAssignments(gCse, calc);
                }

                WriteBlockComment("Product terms:");

                foreach (int k in Products)
                {
                    (string aTerm, Subexpression aSe) = CseTerm(fCse, "a", "f", litArrayF, aRows, aCols, k);
                    (string bTerm, Subexpression bSe) = CseTerm(gCse, "b", "g", litArrayG, bRows, bCols, k);

                    s = $"{ProductName(k)} := {aTerm} * {bTerm};";
                    o(s);
                    CalculateProduct(calc, aTerm, aSe, bTerm, bSe, product: k);
                }

                if (dCse.AuxExpressions.Any())
                {
                    WriteBlockComment("Auxiliary variables for sums of products:");

                    WriteAuxiliaryAssignments(dCse, calc);
                }

                WriteBlockComment("Target matrix sums of products:");

                for (int row = 1; row <= cRows; row++)
                    for (int col = 1; col <= cCols; col++)
                    {
                        if (transposedMode && (row > col))
                        {
                            Fatal("Transposed mode not supported yet. Sorry!");
                            continue;
                        }

                        (string term, Subexpression se) = CseSumOfProducts(dCse, row, col);
                        calc.Calculate($"c{row}{col}", se);

                        s = $"c{row}{col} := {term}";
                        s = s.Trim() + ";";
                        o(s);
                    }

                o("");

                AnnounceValidationOutcome(calc, dicName2Val);

                WriteYacasSolutionFooter(simplifiedSolutionFileName, ref bVerified);
            }
            fOut = null;

            o("");
            o($"Algorithm  written to file '{simplifiedSolutionFileName}'");
            o(bVerified ? "verified OK!" : "verification failed!!!");
            o("");

        }

        /// <summary>
        /// Products consist of two factors.
        /// Each of them can be a true sum (2+ operands) or a simple variable (single operand).
        /// </summary>
        private static void CalculateProduct(Calculator calc, string aTerm, Subexpression aSe, 
                                                              string bTerm, Subexpression bSe, int product)
        {
            string productName = ProductName(product);

            if (aSe != null)
            //  True sum, provided as Subexpression
            {
                calc.Calculate($"a{productName}", aSe);
            }
            else
            //  A simple variable, provided as string
            {
                calc.Calculate($"a{productName}", aTerm);
            }
            if (bSe != null)
            {
                calc.Calculate($"b{productName}", bSe);
            }
            else
            {
                calc.Calculate($"b{productName}", bTerm);
            }
            //  The calculator cannot multiply.
            //  Therefore, the multiplication is done here
            //  and stored as result.
            calc.Store($"{productName}",
                       calc.Retrieve($"a{productName}")
                     * calc.Retrieve($"b{productName}"));
        }

        private static void AnnounceValidationOutcome(Calculator calc, Dictionary<string, double> dicName2Val)
        {
            //  Compare the expected values against the actual values
            bool cVerified = calc.Validate(dicName2Val, commentPrefix: "# ");

            if (cVerified)
            {
                WriteBlockComment("Algorithm validated via prime element calculation.");
            }
            else
            {
                WriteBlockComment("Algorithm could *not* be validated via prime element calculation.");
            }
            o();
        }

        private static void PreparePrimeElementValidation(out Calculator calc, out Dictionary<string, double> dicName2Val)
        {
            calc = new();
            PrimeNumberGenerator primeGen = new();
            IEnumerator<int> primes = primeGen.Primes(100, aRows * aCols + bRows * bCols);
            int[,] a = new int[aRows, aCols];
            int[,] b = new int[bRows, bCols];
            int sign = 1;

            for (int row = 0; row < aRows; row++)
                for (int col = 0; col < aCols; col++)
                {
                    primes.MoveNext();
                    sign = -sign;
                    a[row, col] = sign * primes.Current;
                    calc.Store($"a{row + 1}{col + 1}", a[row, col]);
                }

            for (int row = 0; row < bRows; row++)
                for (int col = 0; col < bCols; col++)
                {
                    primes.MoveNext();
                    sign = -sign;
                    b[row, col] = sign * primes.Current;
                    calc.Store($"b{row + 1}{col + 1}", b[row, col]);
                }

            dicName2Val = [];
            for (int row = 0; row < cRows; row++)
                for (int col = 0; col < cCols; col++)
                {
                    int sum = 0;

                    for (int k = 0; k < aCols; k++)
                    {
                        sum += a[row, k] * b[k, col];
                    }

                    dicName2Val[$"c{row + 1}{col + 1}"] = sum;
                }
        }

        private static void WriteBlockComment(string comment, string comment2 = null)
        {
            o();
            o("#");
            o($"# {comment}");
            if (comment2 != null)
            {
                o($"# {comment2}");
            }
            o("#");
        }

        private static void WriteAuxiliaryAssignments(CommonSubexpressionEliminator cse, Calculator calc)
        {
            int idx = 0;

            foreach (Subexpression se in cse.AuxExpressions)
            {
                idx++;
                o($"{cse.AuxVarName(idx)} := {se};");
                calc.Calculate(cse.AuxVarName(idx), se);
            }
        }

        private static void WriteYacasSolutionFooter(string solutionFileName, ref bool bVerified)
        {
            string s;

            if (algorithmMode == AlgorithmMode.FullBrent)
                for (int row = 1; row <= cRows; row++)
                    for (int col = 1; col <= cCols; col++)
                    {
                        if (!transposedMode || (row <= col))
                        {
                            s = $"Simplify(c{row}{col} - (";

                            for (int k = 1; k <= aCols; k++)
                            {
                                s += (k > 1) ? " + " : "";
                                s += $"a{row}{k}*b{k}{col}";
                            }

                            s += "));";
                            o(s);
                        }
                    }

            o();
            if (!VerifyBrentEquations())
            {
                o("# ==> Algorithm does not fulfill Brent Equations");
            }
            else
            {
                o("# Algorithm properly fulfills all Brent Equations"
                 + (algorithmMode == AlgorithmMode.Mod2Brent ? " mod 2" : ""));
                bVerified = true;
            }

            o();
            o("#");
            o($"# End of {Signature()} solution file '{Path.GetFileName(solutionFileName)}'");
            o("#");
        }

        private static CommonSubexpressionEliminator 
            CreateCommonSubexpressionEliminator(Mat mat, string title)
        {
            int fgd = (int)mat;
            //  prefix for D is T to move aux variables to the end of the sums
            string prefix = literalName[fgd].Replace("D", "T");
            // var cse = new CommonSubexpressionEliminator(prefix);
            CommonSubexpressionEliminator cse = new(prefix);

            o($"Creating Common Subexpression Eliminator {title}");

            if (mat != Mat.D)
            {
                o($"Registering {noOfProducts} sums. One for every product.");
                foreach(int k in Products)
                {
                    string s = GetTerm(fgd, k, autoQuote: false);
                    cse.RegisterSum(s);
                }
                o($"{cse.NoOfSums} sums registered. {noOfProducts - cse.NoOfSums} sums have one operand only.");
            }
            else
            {
                o($"Registering {cRows * cCols} sums of products. One fo every {cRows}x{cCols} C matrix element. ");
                for (int row = 1; row <= cRows; row++)
                    for (int col = 1; col <= cCols; col++)
                    {
                        string s = GetSumOfProducts(row, col);
                        cse.RegisterSum(s);
                    }
                o($"{cse.NoOfSums} sums of products registered. "
                 +$"{cRows*cCols - cse.NoOfSums} SOPs have one operand only.");
            }
            o();

            return cse;
        }

        private static string GetSumOfProducts(int row, int col)
        {
            string sop = "";

            foreach (int k in Products)
            {
                int val = litArrayD[row, col, k];

                if (val != 0)
                {
                    Check(val != undefined, Literal("D", row, col, k) + " is undefined");
                    Check(val * val == 1, "Strange D value");
                    Check((val > 0) || (algorithmMode == AlgorithmMode.FullBrent),
                          "Unexpected negative value not allowed for mod 2!");

                    sop += $"{((val > 0) ? "+" : "-")}{ProductName(k)}";
                }
            }
            return sop;
        }

        private static bool NegativeP1Exists()
        {
            if (algorithmMode == AlgorithmMode.FullBrent)
            {
                for (int row = 1; row <= cRows; row++)
                    for (int col = 1; col <= cCols; col++)
                        if (litArrayD[row, col, 1] < 0)
                        {
                            return true;
                        }
            }

            return false;
        }

        private static string GetSumOfProductsExtended(int row, int col)
        {
            string s = "";
            string sumSep = "  ";

            foreach (int k in Products)
            {
                int val = litArrayD[row, col, k];

                Check(val != undefined, Literal("D", row, col, k) + " is undefined");

                if (val != 0)
                {
                    Check(val * val == 1, Literal("D", row, col, k) + "has strange value");
                    if (val < 0)
                    {
                        Check(algorithmMode == AlgorithmMode.FullBrent,
                              "Unexpected negative value!");
                        s += "- ";
                    }
                    else
                    {
                        //  omit the first '+' in sum not preceeded by a '-'
                        s += sumSep;
                    }
                    sumSep = "+ ";

                    s += $"{ProductName(k)} ";
                }
                else
                {
                    s += "  " + "".PadRight(1 + productDigits, ' ') + " ";
                }
            }

            if (!NegativeP1Exists())
            {
                s = s.Substring(2);
            }

            return s.TrimEnd();
        }

        private static (string, Subexpression) CseTerm(CommonSubexpressionEliminator cse, string litName, string prefix, 
                                      DynArray3D<int> litArray, int rows, int cols, int product)
        {
            string s = GetTerm(rows, cols, product, litArray, prefix.ToUpper(), litName, autoQuote:false);
            string term;
            Subexpression se;
            
            (term, se) = cse.SimplifiedSum(s);

            if (term.Contains('+') || term.Contains("-"))
            {
                term = $"({term})";
            }
            return (term, se);
        }

        private static (string, Subexpression) CseSumOfProducts(CommonSubexpressionEliminator cse, int row, int col)
        {
            string s = GetSumOfProducts(row, col);
            string t;
            Subexpression se;
            (t, se) = cse.SimplifiedSum(s);

            return (t, se);
        }

        static void WriteSolutionFile(string solutionFileName)
        {
            if (solutionFileName == "")
            {
                return;
            }

            o($"Writing solution");

            using (StreamWriter sw = OpenWriter(solutionFileName, "solution"))
            {
                fOut = sw;
                o($"# Solution file '{solutionFileName}'");
                o("# Created: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
                o("");

                foreach (int fgd in Fgd_all)
                    for (int row = 1; row <= Rows[fgd]; row++)
                        for (int col = 1; col <= Cols[fgd]; col++)
                            foreach (int k in Products)
                            {
                                o($"{Literal(literalName[fgd], row, col, k)} = {litArrays[fgd][row, col, k]}");
                            }
                o("");
            }
            fOut = null;
        }

        private static int GetSignaturePart(string reducedSignature, int v)
        {
            try
            {
                string[] arr = Tokenize(reducedSignature, XSeparator);
                return int.Parse(arr[v]);
            }
            catch(Exception ex)
            {
                o($"Invalid signature '{reducedSignature}': {ex.Message}. Two parts expected. Format rxc");
                Finish(1);
                return 0;
            }
        }
    }
}
