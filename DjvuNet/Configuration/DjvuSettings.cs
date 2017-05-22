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
            CoderFactory = new ZPCoderFactory();
        }

        public IDataCoderFactory CoderFactory { get; private set; }

    }
}
