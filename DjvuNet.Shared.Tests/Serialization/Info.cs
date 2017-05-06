using System;
using System.Collections.Generic;
using System.Text;

namespace DjvuNet.Serialization
{
    public class Info : NodeBase
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public int Version { get; set; }

        public int DPI { get; set; }

        public double Gamma { get; set; }

        public int Orientation { get; set; }
    }
}
