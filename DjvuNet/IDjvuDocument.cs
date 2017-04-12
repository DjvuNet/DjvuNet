using System.Collections.Generic;
using System.ComponentModel;
using DjvuNet.DataChunks;
using DjvuNet.DataChunks.Navigation.Interfaces;

namespace DjvuNet
{
    public interface IDjvuDocument
    {
        IDjvuPage ActivePage { get; set; }

        DirmChunk Directory { get; }

        bool IsDisposed { get; }

        IDjvuPage FirstPage { get; }

        int Identifier { get; }

        IReadOnlyList<DjviChunk> Includes { get; }

        bool IsInverted { get; set; }

        IDjvuPage LastPage { get; }

        string Location { get; }

        string Name { get; }

        INavigation Navigation { get; }

        IDjvuPage NextPage { get; }

        IReadOnlyList<IDjvuPage> Pages { get; }

        IDjvuPage PreviousPage { get; }

        FormChunk RootForm { get; }

        event PropertyChangedEventHandler PropertyChanged;

        void Dispose();

        List<T> GetRootFormChildren<T>() where T : IffChunk;

        void Load(string filePath, int identifier = 0);
    }
}