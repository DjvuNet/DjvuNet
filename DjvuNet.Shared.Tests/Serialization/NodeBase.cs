using System;
using System.Collections.Generic;
using System.Text;

namespace DjvuNet.Serialization
{
    public class NodeBase
    {
        public string ID { get; set; }

        public int NodeOffset { get; set; }

        public int Size { get; set; }
    }

    public class NodeBaseDesc : NodeBase
    {
        public string Description { get; set; }
    }

    public class WaveletNodeBase : NodeBaseDesc
    {
        public int Slices { get; set; }

        public double Version { get; set; }

        public string Color { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }
}
