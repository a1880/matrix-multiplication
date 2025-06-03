using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace akUtil
{
    public class HtmlWriter : Util, IDisposable
    {
        protected const string CharacterSet = "utf-8";    //  "iso-8859-1"

        private string body;

        private StreamWriter htmlFile;

        private string htmlFileName;

        private readonly List<bool> isRowOpen;  //  isRowOpen[0] refers to the current table

        private readonly string AdobeSvgNamespace;

        private string VMLNamespace;

        private readonly Timer timer;

        ~HtmlWriter()
        {
            Dispose();
        }

        private static readonly string[] defaultStyle =
        [
            "a:link    { font-family: Verdana,Arial;",
            "            text-decoration: none; color:black; }",
            "a:active  { font-family: Verdana,Arial;",
            "            text-decoration: none; color:red; }",
            "a:visited { font-family: Verdana,Arial;",
            "            text-decoration: none; color:black; }",
            "a:hover   { font-family: Verdana,Arial;",
            "            text-decoration: underline; color:blue; }",
            "  body     { background-color:#fffff;",
            "           }",
            "  .header  { font-size:16pt;",
            "             font-family:Helvetica,Arial;",
            "             text-align:left;",
            "             margin-top:15pt;",
            "             color:#000000;",
            "             background-color:#ffffff;",
            "             }",
            "       H1  { font-size:18pt;",
            "             font-family:Helvetica,Arial;",
            "             text-align:left;",
            "             margin-top:20pt;",
            "             color:#000000;",
            "             background-color:#ffffff;",
            "             }",
            "       H2  { font-size:14pt;",
            "             font-family:Helvetica,Arial;",
            "             text-align:left;",
            "             margin-top:20pt;",
            "             color:#000000;",
            "             background-color:#ffffff;",
            "             }",
            "       H3  { font-size:12pt;",
            "             font-family:Helvetica,Arial;",
            "             text-align:left;",
            "             margin-top:20pt;",
            "             color:#000000;",
            "             background-color:#ffffff;",
            "             }",
            "        P  { font-size:10pt;",
            "             font-family:Helvetica,Arial; ",
            "             text-align:left;",
            "             }",
            "     table {",
            //"             border: 1px solid thin black;",
            //"             border: 1px thin solid black;",
            "             border: none;",
            "             border-collapse: collapse;  ",
            "             empty-cells: hide; ",
            "             /*  width: 100%; */",
            "           }",
            "        td {",
            "             border-style: solid;",
            "             border-width: 1px;",
            "             border-color: gray;",
            "             border-collapse: collapse;  ",
            "             empty-cells: hide; ",
            "             margin: 0px;",
            "             padding: 10px;",
            "             /*  border: dashed thin gray;  */",
            "             font-family:Helvetica,Arial; ",
            "             font-size: 1.0em;",
            "           }",
            "     .tr1  { font-size:12pt;",
            "             font-family:Helvetica,Arial; ",
            "             font-weight:bold;",
            "             margin-top:5pt;",
            "             margin-left:5pt;",
            "             vertical-align:middle;",
            "             background-color:#000000;",
            "             color:#ffffff;",
            "           }",
            "     .tro  { font-size:11pt;",
            "             font-family:Helvetica,Arial; ",
            "             font-weight:normal;",
            "             margin-top:5pt;",
            "             margin-left:5pt;",
            "             vertical-align:middle;",
            "             background-color:#d0d0d0;",
            "           }",
            "     .tre  { font-size:11pt;",
            "             font-family:Helvetica,Arial; ",
            "             font-weight:normal;",
            "             margin-top:5pt;",
            "             margin-left:5pt;",
            "             vertical-align:middle;",
            "             background-color:#f0f0f0;",
            "           }",
            "    .mark  { background-color:#ffff00;",
            "             font-weight:600;",
            "           }"
        ];


        /**
         * HtmlWriter constructor declaration
         */
        public HtmlWriter()
        {
            body = null;
            htmlFile = null;
            htmlFileName = null;
            isRowOpen = [];
            AdobeSvgNamespace = "";
            VMLNamespace = "";   // "v";
            timer = new Timer();
        }

        public void Dispose()
        {
            if (htmlFile != null)
            {
                //  htmlFile.Close();
                htmlFile = null;
            }
        }

        /**
         * Return string as named HTML anchor
         * @param s The string
         * @param name The anchor name
         * @return The HTML anchor string
         */
        public string Anchor(string s, string name)
        {
            return $"<A name=\"{name}\">{s}</A>";
        }

        /**
         * Surround string with bold tags
         * @param s The String
         * @return The enbolded string
         */
        public string Bold(string s)
        {
            return "<b>" + s + "</b>";
        }


        public void BarChart(double[] yValues, string[] xValues, string[] colors, string[] titles)
        {
            const int chartWidth = 800;
            const int chartHeight = 500;
            const string bgColor = "#F0F0F0";
            string div;
            double yMax = 0.01;
            int r;
            int i = 0;
            int cellWidth = chartWidth / yValues.Length;
            int divWidth = cellWidth - 7;
            int cellHeight = (int)(chartHeight / 1.4);
            string hAttr = " height='" + cellHeight + "px' ";
            // string tdAttr = "align='center' valign='bottom' style='padding:0;font-size:7pt;' width='" + cellWidth + "px'";
            string tdAttr = "align='center' valign='bottom' style='padding:0;font-size:7pt;' ";
            string divAttr = "style='background-color:$COLOR; width:" + divWidth + "px;";
            Table("width='" + chartWidth + "px' style='table-layout:fixed;'");
            Html("<colgroup span=\"" + yValues.Length + "\"/>");
            Row("bgcolor='" + bgColor + "'");

            foreach (double y in yValues)
            {
                if (y > yMax)
                    yMax = y;
            }

            foreach (double y in yValues)
            {
                if (y < 0.001)
                {
                    div = "";
                }
                else
                {
                    r = (int)(chartHeight * y / (1.4 * yMax));
                    div = "<div title='" + titles[i] + "' " + divAttr.Replace("$COLOR", colors[i]) + "height:" + r + ";'/>";
                }

                i++;
                CellAttr(PrettyNum(y) + "<br/>" + div, tdAttr + hAttr);
            }

            Row("bgcolor='" + bgColor + "'");

            i = 0;
            foreach (string x in xValues)
            {
                CellAttr(x, tdAttr + " valign='middle' title='" + titles[i++] + "'");
            }
            TableEnd();
        }

        /**
         * Output double 'd' in table cell; perform rounding
         * 0.0 is displayed as '-'. Values are right-aligned. Negative values are
         * displayed in red rather than in black.
         *
         * @param d the double value
         *
         * @return d
         */
        public double Cell(double d)
        {
            string s;
            if (Math.Abs(d) > 100)
            {
                s = string.Format("{0}", Math.Round(d));
            }
            else if (Math.Abs(d) > 10)
            {
                s = string.Format("{0:00.0}", Math.Round(10 * d) / 10.0);
            }
            else
            {
                s = string.Format("{0:000.00}", Math.Round(100 * d) / 100.0);
            }

            if (d == 0.0)
            {
                CellRight("-");
            }
            else if (d > 0.0)
            {
                CellRight(s);
            }
            else
            {
                CellRight("<FONT color=\"red\"><U>" + s + "</U></FONT>");
            }

            return (d);
        }

        /**
         * Output string as table cell. Strings with '%' or '[' are right-aligned.
         *
         * @param s the String
         */
        public void Cell(string s)
        {
            if ((s.IndexOf("%") > 1) && (s.IndexOf("[") < 1))
            {
                CellRight(s);
            }
            else
            {
                Html("<TD>" + s + "</TD>");
            }
        }

        /**
         * Output String right-aligned as table cell
         *
         * @param s the String
         *
         * @see #cell
         */
        public void CellRight(string s)
        {
            Html("<TD align=\"right\">" + s + "</TD>");
        }

        /**
         * Output String value as table cell with specified 'width' attribute
         *
         * @param s the String
         * @param width the width attribute (e.g. "12%" or "50")
         *
         * @see #cell
         */
        public void Cell(string s, string width)
        {
            Html($"<TD width=\"{width}\">{s}</TD>");
        }

        /**
         * Output String value as table cell with specified 'width' attribute
         *
         * @param s the String
         * @param width the width attribute (e.g. "12%" or "50")
         * @param additional attribute
         *
         * @see #cell
         */
        public void Cell(string s, string width, string attr)
        {
            Html($"<TD width=\"{width}\" {attr}>{s}</TD>");
        }

        /**
         * Output String value as table cell with specified attribute
         *
         * @param s the String
         * @param attr the attribute 
         *
         * @see #cell
         */
        public void CellAttr(string s, string attr)
        {
            Html("<TD " + attr + ">" + s + "</TD>");
        }

        /**
         * Output int value as table cell with specified attribute
         *
         * @param i the Int
         * @param attr the attribute 
         *
         * @see #cell
         */
        public void CellAttr(int i, string attr)
        {
            Html("<TD " + attr + ">" + i + "</TD>");
        }

        /**
         * Output String as table cell in center-aligned form.
         *
         * @param s the String
         *
         * @see #cell
         */
        public void CellCenter(string s)
        {
            Html("<TD align=\"center\">" + s + "</TD>");
        }

        /**
         * Output int as table cell in center-aligned form.
         *
         * @param i the Int
         *
         * @see #cell
         */
        public void CellCenter(int i)
        {
            Html("<TD align=\"center\">" + i + "</TD>");
        }

        /**
         * Output String as table cell in right-aligned form.
         * Specify width attribute
         *
         * @param s the String
         * @param width the width attribute (e.g. "12%" or "50")
         *
         * @see #cell
         */
        public void CellRight(string s, string width)
        {
            Html("<TD width=\"" + width + "\" align=\"right\">" + s + "</TD>");
        }

        /**
         * Output int as table cell in right-aligned form.
         *
         * @param i the Int
         *
         * @see #cell
         */
        public void CellRight(int i)
        {
            Html("<TD align=\"right\">" + i + "</TD>");
        }

        /**
         * Finish html output. The html footer is output and the file closed.
         */
        public void Close(bool quiet = false)
        {
            if (!quiet)
            {
                Footer();
            }
            htmlFile.Close();
            o("HTML file '" + GetFileName() + "' ready!");
        }

        /**
         * Output HTML comment message
         *
         * @param msg the message
         *
         * @see #html
         */
        public void Comment(string msg)
        {
            string pre = "<!--  ";
            string post = "  -->";
            int len = pre.Length + msg.Length + post.Length;

            while (len < 73)
            {
                pre += "=";

                if (++len < 73)
                {
                    post = "=" + post;
                    ++len;
                }
            }

            if (len < 76)
            {
                pre += "<";
                post = ">" + post;
            }

            while (len < 78)
            {
                pre += " ";

                if (++len < 78)
                {
                    post = " " + post;
                    ++len;
                }
            }

            Html(pre + msg + post);
        }

        public static string Encode(string s)
        {
            return System.Net.WebUtility.HtmlEncode(s);
        }

        public static string Decode(string s)
        {
            return System.Net.WebUtility.HtmlDecode(s);
        }

        /**
         * Output HTML footer as end of HTML output.
         * An open table is closed automatically.
         *
         * @see #html
         * @see #header
         */
        private void Footer()
        {
            if (IsTableOpen)
            {
                TableEnd();
            }

            Html("<P class=\"subTitle\">");
            Html("created in " + timer.ToString());
            Html(" with a hand-crafted tool of ");
            Link("Axel Kemper", $"mailto:{AkMailAddress()}");
            Html("</BODY>");
            Html("</HTML>");
            Comment("end of file:");
            Comment("'" + GetFileNameShort() + "'");
            Comment("======================");
        }

        private string AkMailAddress()
        {
            var user = Environment.GetEnvironmentVariable("USERNAME");

            if ((user != null) && (user.Length == 2))
            {
                return "axel@kemperzone.de";
            }
            else
            {
                return "axel.kemper@eon.com";
            }
        }

        /**
         * Output <object> tag for Adobe's SVG viewer
         *
         * @see #html
         * @see #header
         */
        private void GenerateHtmlReferenceToAdobeSVG()
        {
            Html("<object id='AdobeSVG' classid='clsid:78156a80-c6a1-4bbf-8e6a-3cd390eeb4e2'>");
            Html("</object>");
            Html("<?import namespace='" + AdobeSvgNamespace
                    + "' implementation='#AdobeSVG'?>");
        }

        /**
         * Output <sytle> for VML
         *
         * @see #header
         * @see #html
         */
        private void GenerateHtmlStyleForVML()
        {
            Html("<style>");
            Comment("reference to Microsoft's VML rendering engine");
            Comment("'behaviour' is spelled 'behavior' in the US and in Redmond ...");
            Html(VMLNamespace + "\\:* { behavior: url(#default#VML); }");

            Comment("added for Office extensions");
            Html("o\\:* { behavior: url(#default#VML);}");

            Html("</style>");
        }

        /**
         * Finish html output. The html footer is output and the memory stream closed.
         * returns complete HTML output as one string
         */
        public string GetAllText()
        {
            string s;
            StreamReader reader;

            Footer();
            htmlFile.Flush();

            // convert stream to string
            htmlFile.BaseStream.Position = 0;
            reader = new StreamReader(htmlFile.BaseStream);
            s = reader.ReadToEnd();
            htmlFile.Close();
            reader.Close();

            return s;
        }


        /**
         * Return HTML file name
         *
         * @return the name of the HTML file.
         */
        public string GetFileName()
        {
            return (htmlFileName);
        }

        /**
         * Return HTML file name is shorter format
         *
         * @return the (possibly shortened) name of the HTML file.
         */
        public string GetFileNameShort()
        {
            string s = GetFileName();
            int pos;

            while (s.Length > 48)
            {
                pos = s.IndexOf("\\", 3);
                if (pos >= 2)
                    s = ".." + s.Substring(pos);
                else
                    break;
            }
            return (s);
        }

        public void H2(string s)
        {
            Html($"<H2>{s}</H2>");
        }

        /**
         * Output HTML header as start of HTML file
         *
         * @param title the HTML title information (displayed in window banner)
         *
         * @see #footer
         */
        private void Header(string title, string[] style = null)
        {
            Html("<!DOCTYPE html>");

            // we have to add the namespace for Adobe SVG, if there
            // is a namespace definition
            if (AdobeSvgNamespace.Equals(""))
            {
                if (VMLNamespace.Equals(""))
                {
                    Html("><HTML>");
                }
                else
                {
                    Html("><HTML xmlns:" + VMLNamespace
                            + "=\"urn:schemas-microsoft-com:vml\">");
                }
            }
            else
            {
                Html("<HTML xmlns:" + AdobeSvgNamespace
                        + "='http://www.w3.org/2000/svg'>");
            }

            Comment("====================");
            Comment("(c) Axel Kemper");
            Comment($"mailto: {AkMailAddress()}");
            Comment("File: '" + GetFileNameShort() + "'");
            Comment("Created: " + DateTime.Now.ToString());
            Comment("====================");

            Html("<HEAD>");
            Html("<META HTTP-EQUIV=\"Content-Type\" content=\"text/html; charset=utf-8\" />");

            title = title.Replace("%f", GetFileNameShort());
            title = title.Replace("%F", GetFileName());
            Html("<TITLE>" + title + "</TITLE>");

            Comment("introduced for Internet Explorer 11");
            Html("<META HTTP-EQUIV=\"X-UA-Compatible\" CONTENT=\"IE=EmulateIE7\" />");

            // we don't want our pages to be cached
            Html("<META HTTP-EQUIV=\"Expires\" CONTENT=\"0\"/>");

            // we have to add the <object> for Adobe SVG, if there
            // is a namespace definition
            if (!AdobeSvgNamespace.Equals(""))
            {
                GenerateHtmlReferenceToAdobeSVG();
            }

            // add VML style if there is a VML namespace defined
            if (!VMLNamespace.Equals(""))
            {
                GenerateHtmlStyleForVML();
            }

            Style(style);

            Html("</HEAD>");

            if (body == null)
            {
                Html("<BODY>");
            }
            else
            {
                Html("<BODY " + body + ">");
            }
        }

        /**
         * Output a string to the HTML file
         * A '>' in first column suppresses newlines.
         *
         * @param s the string
         */
        public void Html(string format, params object[] args)
        {
            string s = (args.Length == 0) ? format : string.Format(format, args);

            if (htmlFile == null)
            {
                Fatal("Missing call of 'HtmlWriter.open()'!");
            }

            if (s.StartsWith(">"))
            {
                Debug.Write(s.Substring(1));
                htmlFile.Write(s.Substring(1));
            }
            else
            {
                Debug.Write("\n" + s);
                htmlFile.Write("\n" + s);
            }
        }

        public bool IsTableOpen
        {
            get => isRowOpen.Any();
        }

        /**
         * Surround string with italics tags
         * @param s The String
         * @return The enbolded string
         */
        public string Italics(string s)
        {
            return "<i>" + s + "</i>";
        }

        /**
         * Output a link
         * 
         * @param s The visible string
         * @param href The URL to link
         */
        public void Link(string s, string href)
        {
            Html("<A href=\"" + href + "\">" + s + "</A>");
        }

        /**
         * Surround string as "mark" span
         * @param s The String
         * @return The marked string
         */
        public string Mark(string s)
        {
            return "<SPAN class=\"mark\">" + s + "</SPAN>";
        }

        public void NewLine()
        {
            Html("<BR/>&nbsp;");
        }

        /**
         * Create the HTML file for output and write the HTML header.
         *
         * @param fileName the name of the file ("<Memory>" for in-memory creation)
         * @param title the HTML title
         *
         * @see #header
         */
        public void Open(string fileName, string title, string[] ccsStyle = null)
        {
            System.Text.Encoding enc;
            MemoryStream stream;

            if (fileName.Equals("<Memory>"))
            {
                this.timer.Restart();

                enc = System.Text.Encoding.GetEncoding(HtmlWriter.CharacterSet);
                stream = new MemoryStream();
                htmlFile = new StreamWriter(stream, enc);
                htmlFileName = fileName;

                o("Creating HTML contents in memory");
            }
            else
            {
                try
                {
                    enc = System.Text.Encoding.GetEncoding(HtmlWriter.CharacterSet);
                    htmlFile = new StreamWriter(fileName, false, enc);

                    htmlFileName = Path.GetFullPath(fileName);

                    o("Creating HTML file '" + GetFileName() + "'");
                }
                catch (IOException e)
                {
                    Fatal("Error creating HTML output file '" + fileName + "' {0}", e.Message);
                }

                o("Creating HTML contents in file " + fileName);
            }

            Header(title, ccsStyle);
        }

        /**
         * Open a new table row. Automatically close an open row.
         */
        public void Row()
        {
            Check(IsTableOpen, "No open table!");
            if (isRowOpen[0])
            {
                RowEnd();
            }

            Html("<TR>");

            isRowOpen[0] = true;
        }

        /**
         * Open a new table row with attribute. Automatically close an open row.
         *
         * @param attr the attribute string (e.g. a background color)
         */
        public void Row(string attr)
        {
            Check(IsTableOpen, "No open table!");
            if (isRowOpen[0])
            {
                RowEnd();
            }

            Html("<TR " + attr + ">");

            isRowOpen[0] = true;
        }

        /// <summary>
        /// Open an even or odd Row for striped table layout
        /// </summary>
        /// <param name="bOdd"></param>
        public void Row(bool bOdd)
        {
            if (bOdd)
            {
                Row("class='tro'");
            }
            else
            {
                Row("class='tre'");
            }
        }

        /// <summary>
        /// Open first Row in a table
        /// </summary>
        public void Row1()
        {
            Row("class='tr1'");
        }

        /**
         * Close an open table row
         *
         * @see #Row
         */
        public void RowEnd()
        {
            Check(IsTableOpen, "No open table!");
            Check(isRowOpen[0], "No open row!");

            Html("</TR>");

            isRowOpen[0] = false;
        }

        /**
         * Set the HTML namespace for VML graphics
         *
         * @param namespace (typically something like "v"
         *
         * @see #header
         */
        public void SetVMLNamespace(string VMLNamespace)
        {
            this.VMLNamespace = VMLNamespace;
        }

        /**
         * Set the HTML body string
         *
         * @param body the body string (can be used to specify a background color)
         *
         * @see #header
         */
        public void SetBody(string body)
        {
            this.body = body;
        }

        /**
         * Style
         * 
         * Write embedded CSS style sheet
         *
         */
        public void Style(string[] style = null)
        {
            style ??= defaultStyle;

            Html("<STYLE type=\"text/css\">");
            Html("<!--");

            foreach (string s in style)
            {
                Html(s);
            }

            Html("    //-->");
            Html("    </STYLE>");
        }

        /**
         * Close an open HTML table. An open row is also closed automatically.
         *
         * @see #rowEnd
         */
        public void TableEnd()
        {
            Check(IsTableOpen, "No open table!");
            if (isRowOpen[0])
            {
                RowEnd();
            }

            Html("</TABLE>");

            isRowOpen.RemoveAt(0);
        }

        /**
         * Open an HTML table. Nested tables are supported
         *
         * @param attr an attribute string. Can be used to specify a table width.
         */
        public void Table(string attr = "")
        {
            Html("<TABLE " + attr + ">");
            isRowOpen.Insert(0, false);
        }
    }
}
