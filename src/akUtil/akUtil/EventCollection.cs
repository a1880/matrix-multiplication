using System.Collections.Generic;

namespace akUtil
{
    /// <summary>
    /// A Dictionary collection of Events for statistical reporting
    /// </summary>
    class EventCollection : Util
    {
        private static string prefix = "c ";

        private readonly Dictionary<string, Event> events;

        /// <summary>
        /// Construct an empty/new EventCollection
        /// </summary>
        public EventCollection()
        {
            events = [];
        }

        /// <summary>
        /// Insert a new Event
        /// this might be just an increment of an existing Event count
        /// </summary>
        /// <param name="name"></param>
        public void Register(string name)
        {
            if (events.TryGetValue(name, out Event ev))
            {
                ev.Inc();
            }
            else
            {
                ev = new Event(name);

                events.Add(name, ev);
            }
        }

        /// <summary>
        /// Write all Event reports to standard output 
        /// put most frequent Events first
        /// </summary>
        /// <param name="title"></param>
        /// <param name="minCount"></param>
        public void Report(string title, int minCount = -1, HtmlWriter hw = null)
        {
            List<string> names;

            if (hw != null)
            {
                hw.H2(prefix);
                if (!string.IsNullOrEmpty(title))
                {
                    hw.H2(prefix + title);
                }
                if (events.Count == 0)
                {
                    hw.Html("<P>No events to report</P>");
                }

                hw.Table();
            }
            else
            {
                o(prefix.Trim());
                if (!string.IsNullOrEmpty(title))
                {
                    o(prefix + title);
                    o(prefix + "".PadLeft(title.Length, '-'));
                }
                if (events.Count == 0)
                {
                    o(prefix + "No events to report");
                }
            }

            names = [.. events.Keys];
            names.Sort();

            foreach (string name in names)
            {
                events.TryGetValue(name, out Event ev);
                if ((minCount < 0) || (ev.Count >= minCount))
                {
                    ev.Report(hw);
                }
            }

            if (hw != null)
            {
                hw.TableEnd();
            }
            else
            {
                o(prefix.Trim());
            }
        }

        /// <summary>
        /// Define the string prefex to be prepended to all output texts
        /// </summary>
        /// <param name="s"></param>
        public static void SetPrefix(string s)
        {
            Assert(s != null);

            prefix = s;
            Event.SetPrefix(s);
        }
    }
}
