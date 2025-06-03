using akUtil;

namespace akOptions
{
    public class CommandlineParser : Util
    {
        protected static bool Match(ref string str, string pattern)
        {
            if (str.StartsWith(pattern))
            {
                str = str.Substring(pattern.Length);
                return true;
            }

            return false;
        }

        protected static bool IsOtherOption(ref string str, string name, string longName)
        {
            bool ret = false;

            if (Match(ref str, "--"))
            {
                if (!Match(ref str, longName) || !Match(ref str, "="))
                {
                    ret = true;
                }
            }
            else if (Match(ref str, "-"))
            {
                if (!Match(ref str, name) || !Match(ref str, "="))
                {
                    ret = true;
                }
            }
            else
            {
                ret = true;
            }

            return ret;
        }
    }
}
