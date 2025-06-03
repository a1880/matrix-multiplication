using System;
using System.IO;

namespace akUtil
{
    /// <summary>
    /// Stream reader for StandardOutput and StandardError stream readers
    /// Runs an external BeginRead loop on the underlaying stream bypassing the stream reader.
    /// 
    /// The TextReceived sends data received on the stream in non delimited chunks. 
    /// Event subscriber can then split on newline characters etc as desired.
    /// </summary>
    /// 
    /// Taken from https://stackoverflow.com/a/40352921/1911064
    /// 
    public class AsyncStreamReader(StreamReader readerToBypass)
    {
        public delegate void EventHandler<T>(object sender, T Data);
        public event EventHandler<string> DataReceived;

        protected readonly byte[] buffer = new byte[4096];
        private readonly StreamReader reader = readerToBypass;

        /// <summary>
        ///  If AsyncStreamReader is active
        /// </summary>
        public bool Active { get; private set; } = false;

        public void Start()
        {
            if (!Active)
            {
                Active = true;
                BeginReadAsync();
            }
        }

        public void Stop()
        {
            Active = false;
        }

        protected void BeginReadAsync()
        {
            if (Active)
            {
                reader.BaseStream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadCallback), null);
            }
        }

        private void ReadCallback(IAsyncResult asyncResult)
        {
            var bytesRead = reader.BaseStream.EndRead(asyncResult);

            string data = null;

            //  Terminate async processing if callback has no bytes
            if (bytesRead > 0)
            {
                data = reader.CurrentEncoding.GetString(buffer, 0, bytesRead);
            }
            else
            {
                //  callback without data - stop async
                Active = false;
            }

            //  Send data to event subscriber - null if no longer active
            DataReceived?.Invoke(this, data);

            //  Wait for more data from stream
            BeginReadAsync();
        }
    }
}
