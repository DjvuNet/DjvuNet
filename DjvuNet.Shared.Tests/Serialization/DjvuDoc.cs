using System;
using System.Collections.Generic;
using System.Text;

namespace DjvuNet.Serialization
{
    public class DjvuDoc
    {
        public string File { get; set; }

        public ElementBase DjvuData { get; set; }

        public string Hash { get; set; }

        public string Algorithm { get; set; }
    }
}
