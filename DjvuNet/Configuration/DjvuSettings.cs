using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DjvuNet.Compression;

namespace DjvuNet.Configuration
{
    public class DjvuSettings
    {
        private TraceSwitch _logLevel;

        private static DjvuSettings _Current;

        public static DjvuSettings Current
        {
            get
            {
                if (_Current != null)
                    return _Current;
                else
                {
                    _Current = new DjvuSettings();
                    return _Current;
                }
            }
        }

        private DjvuSettings()
        {
            _logLevel = new TraceSwitch(
                        "DjvuNet.LogLevel", "Switch to use for setting DjvuNet library classic logging level", "Info");
            CoderFactory = new ZPCoderFactory();
        }

        public TraceSwitch LogLevel
        {
            get { return _logLevel; }

            internal set { _logLevel = value; }
        }

        public IDataCoderFactory CoderFactory { get; private set; }

    }
}
