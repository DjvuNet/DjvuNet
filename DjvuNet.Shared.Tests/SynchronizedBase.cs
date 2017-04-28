using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Xunit;

namespace DjvuNet.Tests
{
    /// <summary>
    /// SynchronizedBase allows derived classes to implement sequential
    /// synchronization between unit tests. Every time test is executed
    /// new SynchronizedBase derived object is instantiated and automatically
    /// it's test method execution is synchronized via mutex.
    /// </summary>
    public class SynchronizedBase : IDisposable
    {
        public static Mutex Mutex;
        public const int TimeOut = 30000;
        private bool _EnteredMutex;

        static SynchronizedBase()
        {
            Process proc = Process.GetCurrentProcess();
            string name = $"{proc.Id}{proc.ProcessName}";
            if (Mutex == null)
                Mutex = new Mutex(true, name);

            AppDomain.CurrentDomain.DomainUnload += AppDomainUnload;
        }

        private static void AppDomainUnload(object sender, EventArgs e)
        {
            Mutex?.Dispose();
        }

        public SynchronizedBase()
        {
            _EnteredMutex = Mutex.WaitOne(TimeOut);
        }

        public void Dispose()
        {
            if (_EnteredMutex)
                Mutex?.ReleaseMutex();
        }
    }
}
