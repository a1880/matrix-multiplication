using System.Collections.Generic;
using System.Linq;
using static akExtractMatMultSolution.Util;

namespace akExtractMatMultSolution
{
    /// <summary>
    /// Class to simplify a collection of sums by extracting and replacing
    /// commonly used subexpressions.
    /// This class is geared towards sums in matrix multiplication algorithms.
    /// </summary>
    /// <remarks>
    /// Construct CommonSubexpressionEliminator object
    /// </remarks>
    /// <param name="prefix">String to prepend to auxiliary variable names</param>
    public class CommonSubexpressionEliminator(string prefix)
    {
        /// <summary>
        /// auxiliary variable names are prefixed
        /// </summary>
        private readonly string prefix = prefix;

        /// <summary>
        /// Index for auxiliary variables
        /// </summary>
        private int auxVarIdx = 0;

        /// <summary>
        /// Collection of sums to be simplified
        /// </summary>
        private readonly HashSet<Subexpression> sums = [];

        /// <summary>
        /// Collection of potential common subexpressions
        /// </summary>
        private readonly HashSet<Subexpression> subexprs = [];

        /// <summary>
        /// Auxiliary expressions used to simplify collection of sums.
        /// Sorted according to indices of the auxiliary variables.
        /// </summary>
        private readonly List<Subexpression> auxExprs = [];

        /// <summary>
        /// Return the list of auxiliary expressions
        /// sorted according to auxVarIdx index.
        /// </summary>
        public List<Subexpression> AuxExpressions => auxExprs;

        private int auxVarDigits;
        public string AuxVarName(int idx) => $"{prefix}{idx.ToString().PadLeft(auxVarDigits, '0')}";

        /// <summary>
        /// Return number of subexpressions (of the sums)
        /// </summary>
        public int NoOfSubexpressions => subexprs.Count;

        /// <summary>
        /// Return number of sums (= expressions to simplify)
        /// </summary>
        public int NoOfSums => sums.Count;

        /// <summary>
        /// Number of operations needed to calculate all sums and all
        /// auxiliary sum expressions.
        /// </summary>
        public int OpCount => sums.Sum(sum => sum.Length - 1) 
                            + auxExprs.Sum(e => e.Length - 1);

        /// <summary>
        /// Register new sum in collection of sums to simplify.
        /// </summary>
        /// <param name="sum">The sum string. Example: a11+a12</param>
        public void RegisterSum(string sum)
        { 
            if (SimpleVariable(sum))
            {
                return;
            }

            var expr = new Subexpression(sum);

            if (!sums.Contains(expr))
            {
                //  Store Subexpression objects in a List.
                //  Otherwise, they would be recreated again and again.
                var subs = new List<Subexpression>(expr.AllSubexpressions(subexprs));

                sums.Add(expr);
                int prevCount = subexprs.Count;
                subexprs.UnionWith(subs);

                if (subexprs.Count != prevCount)
                {
                    foreach (Subexpression sub in subs)
                    {
                        sub.CountOccurrences(sums);
                    }
                }
            }
            else
            {
                //  Duplicate sums will be eliminated by a common
                //  auxiliary variable.
                o($"Duplicate sum: '{sum}'");
            }
        }

        /// <summary>
        /// For debugging:
        /// Display variables of CommonSubexpressionEliminator object.
        /// </summary>
        public void Show(string title, bool verbose = false)
        {
            o();
            o($"CSE Show {title}");
            o();
            o($"OpCount {OpCount}");
            o();
            o($"{sums.Count} sums:");
            foreach(var sum in sums)
            {
                o($"  {sum}");
            }
            o();
            o($"{auxExprs.Count} aux sums:");
            int idx = 0;
            foreach (var auxExpr in auxExprs)
            {
                o($"  {prefix}{++idx} = {auxExpr}");
            }

            o();
            o($"{subexprs.Count} subexpressions:");
            if (!verbose)
            {
                o("(entries with 0 Merit not shown)");
            }
            idx = 0;
            foreach (var subexpr in subexprs.Where(se => verbose || (se.Merit > 0)))
            {
                o($"  {subexpr}");

                if (++idx > 200)
                {
                    o("......");
                    o($"(list interrupted after {idx} entries)");
                    o();
                    break;
                }
            }
            o();
            o("-----------------------------------------------------------------------");
            o();
        }

        private bool SimpleVariable(string name)
        {
            return name.Length < (name.Contains("P") ? "P1+P2" : "a11*a12").Length;
        }

        public (string, Subexpression) SimplifiedSum(string sum)
        {
            if (SimpleVariable(sum))
            {
                return (sum, null);
            }

            Subexpression se = Subexpression.Retrieve(sum);

            return (se.ToString(), se);
        }

        /// <summary>
        /// Try to replace reoccurring subexpressions.
        /// </summary>
        public void Simplify(string title = "")
        {
            int replacementCount = 0;
            int prevOpCount = OpCount;

            auxVarDigits = auxExprs.Count.ToString().Length;

            o();
            if (title != null) 
            {
                o(title);
                o("=".PadLeft(title.Length, '='));
            }
            o($"Simplify started: opcount {prevOpCount}");
            o($"Sums:           {sums.Count,6}");
            o($"Subexpressions: {subexprs.Count,6}");

            //  Removal of subexpression can reduce the occurrence counts
            //  of other subexpressions.
            foreach (Subexpression sub in subexprs)
            {
                sub.CountOccurrences(sums);
            }

            for ( ; ; )
            {
                // Select Subexpression with highest Merit.
                // As most Subexpressions don't have any Merit, filter them out
                // to reduce sorting time.
                var seBest = subexprs
                    .Where(se => se.Merit > 0)
                    .OrderByDescending(se => se.Merit)
                    .FirstOrDefault();

                if (seBest != null)
                {
                    int usageCount = 0;
                    var auxName = $"{prefix}{++auxVarIdx}";

                    // o($"Best subexpression: {auxName} := {seBest};");
                    // o($"                    {seBest.ToString(verbose:true)}");

                    auxExprs.Add(seBest);

                    // var prevOcc = seBest.Occurrences;
                    // seBest.CountOccurrences(sums);
                    // var occ = seBest.Occurrences;
                    // Check(prevOcc == occ, "Inconsistent occ");

                    foreach (var sum in sums)
                        if (sum.Contains(seBest))
                        {
                            sum.ReplaceSubexpression(auxName, seBest);
                            replacementCount++;
                            usageCount++;
                        }

                    Check(usageCount == seBest.Occurrences, "Inconsistent usage count");
                    var removed = subexprs.RemoveWhere(se => seBest.Contains(se));
                    // o($"Removed Subexpressions: {removed}");
                    // o($"Extracted '{seBest}': opcount {OpCount}");

                    //  Removal of subexpression can reduce the occurrence counts
                    //  of other subexpressions.
                    foreach (Subexpression sub in subexprs)
                    {
                        sub.CountOccurrences(sums);
                    }
                }
                else
                {
                    break;
                }
            }

            if (replacementCount == 0) 
            {
                o("No replacements found for simplification");
            }
            else
            {
                o("Replacements found for simplification: "
                + $"{auxExprs.Count} * {replacementCount/auxExprs.Count} = {replacementCount}");
                o($"Resulting count of operations: {OpCount}");
            }
            o();
        }

        /// <summary>
        /// For debugging/development:
        /// Perform some CSE tests/experiments.
        /// </summary>
        public static void Test()
        {
            var cse = new CommonSubexpressionEliminator("t");

            cse.RegisterSum("a11+a12-a13+a14 - a51 - a61");
            cse.RegisterSum("a31-a12+a13-a14+a42-a61-a51");
            cse.Show("before simplify", verbose:false);
            cse.Simplify();
            cse.Show("after simplify", verbose:false);

            Finish(0);
        }
    }  //  end of class CommonSubexpressionEliminator
}

//  end of file CommonSubexpressionEliminator.cs

