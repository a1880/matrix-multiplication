using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;

//  http://psycodedeveloper.wordpress.com/2013/01/28/how-to-suspend-and-resume-processes-in-c/

namespace akUtil
{
    /// <summary>Suspend/resume a <see cref="Process"/> 
    /// by suspending or resuming all of its threads.</summary>
    /// <remarks><para>
    /// The threads are suspended/resumed in parallel for no particular reason, other than the 
    /// hope that it may be better to do so as close to all at once as we can.</para></remarks>
    public static class ProcessExtensions
    {
        //  Kemper 12-Jul-2014
        private static ProcessThread[] GetThreads(Process process)
        {
            ProcessThread[] threads = new ProcessThread[process.Threads.Count];

            process.Threads.CopyTo(threads, 0);

            return threads;
        }

        public static void Suspend(this Process process)
        {
            static void suspend(ProcessThread pt)
            {
                var threadHandle = NativeMethods.OpenThread(
                    ThreadAccess.SUSPEND_RESUME, false, (uint)pt.Id);

                if (threadHandle != IntPtr.Zero)
                {
                    try { NativeMethods.SuspendThread(threadHandle); }
                    finally { NativeMethods.CloseHandle(threadHandle); }
                }
            }

            //  Kemper 12-Jul-2014
            //  var threads = process.Threads.ToArray<ProcessThread>();
            var threads = GetThreads(process);

            if (threads.Length > 1)
            {
                Parallel.ForEach(threads,
                    new ParallelOptions { MaxDegreeOfParallelism = threads.Length },
                    pt => suspend(pt));
            }
            else suspend(threads[0]);
        }

        public static void Resume(this Process process)
        {
            static void resume(ProcessThread pt)
            {
                var threadHandle = NativeMethods.OpenThread(
                    ThreadAccess.SUSPEND_RESUME, false, (uint)pt.Id);

                if (threadHandle != IntPtr.Zero)
                {
                    try { NativeMethods.ResumeThread(threadHandle); }
                    finally { NativeMethods.CloseHandle(threadHandle); }
                }
            }

            //  Kemper 12-Jul-2014
            //  var threads = process.Threads.ToArray<ProcessThread>();
            var threads = GetThreads(process);

            if (threads.Length > 1)
            {
                Parallel.ForEach(threads,
                    new ParallelOptions { MaxDegreeOfParallelism = threads.Length },
                    pt => resume(pt));
            }
            else resume(threads[0]);
        }

        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenThread(
                ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

            [DllImport("kernel32.dll")]
            public static extern uint SuspendThread(IntPtr hThread);

            [DllImport("kernel32.dll")]
            public static extern uint ResumeThread(IntPtr hThread);
        }

        [Flags]
        private enum ThreadAccess // : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }
    }
}
