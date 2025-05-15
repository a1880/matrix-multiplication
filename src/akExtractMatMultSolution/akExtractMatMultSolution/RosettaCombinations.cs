using System.Collections.Generic;

namespace akExtractMatMultSolution
{
    //  from https://rosettacode.org/wiki/Combinations#C#

    /// <summary>
    /// Recursive algorithm to find Combinations of integers
    /// </summary>
    public class RosettaCombinations
    {
        private static IEnumerable<int[]> FindCombosRec(int[] buffer, int done, int begin, int end)
        {
            for (int i = begin; i < end; i++)
            {
                buffer[done] = i;

                if (done == buffer.Length - 1)
                {
                    yield return buffer;
                }
                else
                {
                    foreach (int[] child in FindCombosRec(buffer, done + 1, i + 1, end))
                    {
                        yield return child;
                    }
                }
            }
        }

        /// <summary>
        /// Find all combinations with m integers between 0 and n-1
        /// </summary>
        public static IEnumerable<int[]> FindCombinations(int m, int n)
        {
            return FindCombosRec(buffer:new int[m], done:0, begin:0, end:n);
        }
    }
}
