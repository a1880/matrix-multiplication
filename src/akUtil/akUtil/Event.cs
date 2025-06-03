namespace akUtil
{
    /// <summary>
    /// Class to register re-occurring events for statistical reporting
    /// </summary>
    public class Event : Util
    {
        //  prefixed to all output text lines
        static private string prefix = "c ";

        /// <summary>
        /// number of occurrences for this event
        /// </summary>
        public int Count { get; private set; }

        private readonly string name;

        /// <summary>
        /// Construct an Event
        /// </summary>
        /// <param name="name"></param>
        public Event(string name)
        {
            Assert(!string.IsNullOrEmpty(name));

            this.name = name;
            Count = 1;
        }

        /// <summary>
        /// Increment the occurrence count by one
        /// </summary>
        public void Inc()
        {
            Count++;
        }

        /// <summary>
        /// Write Event with count to standard output
        /// </summary>
        public void Report(HtmlWriter hw = null)
        {
            if (hw == null)
            {
                if (Count >= 1)
                {
                    o(prefix + name + " (x " + Count + ")");
                }
                else
                {
                    o(prefix + name);
                }
            }
            else
            {
                hw.Row();
                if (Count >= 1)
                {
                    hw.Cell(prefix + name);
                    hw.Cell("" + Count);
                }
                else
                {
                    hw.Cell(prefix + name);
                    hw.Cell("");
                }
                hw.RowEnd();
            }
        }

        /// <summary>
        /// Set the string prefix to be prepended to all output text lines
        /// </summary>
        /// <param name="s"></param>
        public static void SetPrefix(string s)
        {
            Assert(s != null);

            prefix = s;
        }
    }

}
