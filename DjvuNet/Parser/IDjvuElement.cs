using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DjvuNet
{
    public interface IDjvuElement : IDjvuNode
    {
        List<IDjvuNode> Children { get; set; }

        IDjvuNode FirstChild { get; set; }

        IDjvuNode LastChild { get; set; }

        IDjvuNode PreviousSibling { get; set; }

        IDjvuNode NextSibling { get; set; }

        IDjvuNode FirstSibling { get; set; }

        IDjvuNode LastSibling { get; set; }
    }
}
