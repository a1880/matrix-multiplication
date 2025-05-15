using System;
using System.Collections.Generic;

namespace akExtractMatMultSolution
{
    public static class HashSetExtensions
    {
        /// <summary>
        /// Convert IEnumerable to a set
        /// 
        /// Example:
        /// Returns a HashSet<int> containing 1, 2 and 3
        /// new int[] { 1, 2, 2, 3, 3 }.ToSet();
        /// </summary>
        public static HashSet<T> ToSet<T>(this IEnumerable<T> source)
        {
            return [.. source];
        }

        /// <summary>
        /// Get item out of set, if it is contained.
        /// 
        /// This makes sense, if the copy of 'item' in the set does carry additional
        /// information not present in the 'item' object of the call.
        /// </summary>
        public static T Retrieve<T>(this HashSet<T> hs, T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return hs.TryGetValue(item, out T hit) ? hit: default;
        }

        /// <summary>
        /// Calculate the sum of all set items wrt to function t(item)
        /// </summary>
        public static int Sum<T>(this ISet<T> hs, Func<T, int> t)
        {
            int sum = 0;

            foreach (T item in hs) 
            { 
                sum += t(item);
            }

            return sum;
        }
    }
}
