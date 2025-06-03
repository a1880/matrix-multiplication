using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace akUtil
{
    class StackTracer : Util
    {
        public static void ShowStack()
        {
            var t = new StackTrace(skipFrames:1, fNeedFileInfo:true);
            var frames = t.GetFrames();
            bool bExternal = false;
            var sb = new StringBuilder();
            string prevPath = "~~~~";
            string prevNamespace = "~~~~";

            foreach(var frame in frames)
            {
                if (frame.GetFileName() == null)
                {
                    if (!bExternal)
                    {
                        sb.Append("[External Code]\n");
                        bExternal = true;
                    }
                }
                else
                {
                    var method = frame.GetMethod();
                    string sClass = method.DeclaringType.FullName;

                    if (sClass.StartsWith(prevNamespace))
                    {
                        sClass = sClass.Substring(prevNamespace.Length);
                    }
                    else
                    {
                        prevNamespace = sClass.Split(['.'])[0] + ".";
                    }

                    var parameters = method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}");
                    string args = string.Join(", ", parameters);
                    string s = $"{sClass}.{method.Name}({args})";
                    string fileName = frame.GetFileName();
                    string path = Path.GetDirectoryName(fileName) + @"\";

                    if (fileName.StartsWith(prevPath))
                    {
                        fileName = fileName.Replace(prevPath, "");
                    }
                    else
                    {
                        prevPath = path;
                    }
                    s += $" in {fileName}:{frame.GetFileLineNumber()}:{frame.GetFileColumnNumber()}\n";

                    sb.Append(s);                   
                }
            }

            o(sb.ToString());
            o("");
        }
    }
}
