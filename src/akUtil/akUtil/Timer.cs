using System;
using static akUtil.Util;

namespace akUtil
{
    public class Timer
    {
        private DateTime started;
        private string purpose = "";

        public Timer(string purpose = "")
        {
            Restart();
            this.purpose = purpose;

            if (purpose != "")
            {
                o($"{purpose} started");
            }
        }

        ~Timer()
        {
            End();
        }

        public long Elapsed()
        {
            TimeSpan span = DateTime.Now - started;

            return (long)span.TotalMilliseconds;
        }

        /// <summary>
        /// Explicitely end the timer
        /// without waiting for the Timer object to become out of scope
        /// </summary>
        public void End()
        {
            if (purpose != "")
            {
                o($"{purpose} ended. {this}");
                purpose = "";
            }
        }

        public void Restart()
        {
            started = DateTime.Now;
        }

        public override string ToString()
        {
            var elapsed = Elapsed();

            string s;

            if (elapsed > 1000000)
            {
                s = $"elapsed {Pretty3Num((long)(elapsed * 0.001))}s";
            }
            else if (elapsed > 1000)
            {
                s = $"elapsed {elapsed * 0.001:F2}s".Replace(",", ".");
            }
            else
            {
                s = $"elapsed {elapsed}ms";
            }

            return s;
        }
    }
}
