using System.Collections.Generic;
using System.Linq;
using static akExtractMatMultSolution.Util;

namespace akExtractMatMultSolution
{
    public class MinMax
    {
        private readonly HashSet<int> values = [];
        private int sum = 0;
        private int count = 0;

        public void Register(int i)
        {
            values.Add(i);
            sum += i;
            count++;
        }

        public int Count => count;
        public int Sum => sum;
        public int Max => values.Max();
        public int Min => values.Min();

        /// <summary>
        /// Return a string the the range of values
        /// </summary>
        public string Range()
        {
            int min = Min;
            int max = Max;

            if (min == max)
            {
                return min.ToString();
            }
            else
            {
                return $"{min} .. {PrettyNum(max)}";
            }
        }

        /// <summary>
        /// Show the sorted list of values
        /// </summary>
        public void Show(string title)
        {
            o($"{title} [{ToString()}]");
        }

        public override string ToString()
        {
            List<int> list = [.. values];

            list.Sort();

            string s = string.Join(", ", list.Select(x => x.ToString()));

            return s;
        }

        /// <summary>
        /// Create an array of MinMax objects
        /// </summary>
        public static MinMax[] Array(int length) => 
            [.. from _ in Util.Range(0, length) select new MinMax()];
    }
}
