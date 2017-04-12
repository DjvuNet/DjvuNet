using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DjvuNet
{
    public interface IDjvuElement : IDjvuNode
    {
        IReadOnlyList<IDjvuNode> Children { get; }

        IDjvuNode FirstChild { get; }

        IDjvuNode LastChild { get; }

        IDjvuNode PreviousSibling { get; }

        IDjvuNode NextSibling { get; }

        IDjvuNode FirstSibling { get; }

        IDjvuNode LastSibling { get; }

        int AddNode(IDjvuNode node);

        void ClearChildren();

        void InsertNode(IDjvuNode node, int index);

        bool RemoveNode(IDjvuNode node);

        void RemoveNodeAt(int index);


    }
}
