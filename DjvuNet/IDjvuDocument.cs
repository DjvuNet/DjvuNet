using System;
using System.Collections.Generic;
using System.ComponentModel;
using DjvuNet.DataChunks;

namespace DjvuNet
{
    public interface IDjvuDocument : IDisposable
    {
        IDjvuPage ActivePage { get; set; }

        DirmChunk Directory { get; }

        bool IsDisposed { get; }

        IDjvuPage FirstPage { get; }

        int Identifier { get; }

        IReadOnlyList<IDjviChunk> Includes { get; }

        bool IsInverted { get; set; }

        IDjvuPage LastPage { get; }

        string Location { get; }

        string Name { get; }

        INavigation Navigation { get; }

        IDjvuPage NextPage { get; }

        IReadOnlyList<IDjvuPage> Pages { get; }

        IDjvuPage PreviousPage { get; }

        DjvuFormElement RootForm { get; }

        event PropertyChangedEventHandler PropertyChanged;

        List<T> GetRootFormChildren<T>() where T : DjvuNode;

        void Load(string filePath, int identifier = 0);
    }
}