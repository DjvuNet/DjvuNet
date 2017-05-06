using System;
using System.Collections.Generic;
using System.Text;

namespace DjvuNet.Serialization
{
    public class ElementBase : NodeBase
    {
        public NodeBase[] Children { get; set; }
    }
}
