using System;

namespace DjvuNet.Parser
{
    public interface IDjvuParser
    {
        IAbstractDocumentTree ParseDocument(IDjvuReader reader);

        event ParsedDjvuNode ParsedDjvuNodeEvent;

        event ParsedDjvuElement ParsedDjvuElementEvent;
    }

    public delegate void ParsedDjvuNode(object source, ParsedDjvuNodeEventArgs args);

    public delegate void ParsedDjvuElement(object source, ParsedDjvuElementEventArgs args);

    public class ParsedDjvuNodeEventArgs : EventArgs
    {
        public IDjvuNode Node { get; private set; }

        public ParsedDjvuNodeEventArgs(IDjvuNode node) : base()
        {
            Node = node;
        }

    }

    public class ParsedDjvuElementEventArgs : EventArgs
    {
        public IDjvuElement Element { get; set; }

        public ParsedDjvuElementEventArgs(IDjvuElement element) : base()
        {
            Element = element;
        }

    }

}
