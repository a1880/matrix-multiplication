using System.Collections.Generic;
using System.Linq;
using static akExtractMatMultSolution.Util;

namespace akExtractMatMultSolution
{
    public class MinMax
    {
        readonly HashSet<int> values = [];

        public void Register(int i)
        {
            values.Add(i);
        }

        public void Show(string title)
        {
            string s = title + " [";
            string sep = "";
            List<int> list = values.ToList<int>();

            list.Sort();

            foreach (int i in list)
            {
                s += sep + i;
                sep = ", ";
            }

            o(s + "]");
        }
    }
}
