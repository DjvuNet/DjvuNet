using System.Collections.Generic;

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

        int AddChild(IDjvuNode node);

        void ClearChildren();

        bool ContainsChild(IDjvuNode node);

        bool ContainsChild(IDjvuNode node, IEqualityComparer<IDjvuNode> comparer);

        int InsertChild(int index, IDjvuNode node);

        bool RemoveChild(IDjvuNode node);

        int RemoveChildAt(int index);
    }
}
