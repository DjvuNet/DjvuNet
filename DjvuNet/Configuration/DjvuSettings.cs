using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Configuration
{
    public class DjvuSettings
    {
        private static TraceSwitch _logLevel = new TraceSwitch(
            "DjvuNet.LogLevel", "Switch to use for setting DjvuNet library logging level", "Info");


        public static TraceSwitch LogLevel
        {
            get { return _logLevel; }

            internal set { _logLevel = value; }
        }
    }
}
