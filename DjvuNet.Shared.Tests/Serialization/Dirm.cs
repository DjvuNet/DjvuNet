using System;
using System.Collections.Generic;
using System.Text;

namespace DjvuNet.Serialization
{
    public class Dirm : NodeBaseDesc
    {
        public string DocumentType { get; set; }

        public int FileCount { get; set; }

        public int PageCount { get; set; }
    }
}
