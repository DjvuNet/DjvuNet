using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DjvuNet.Parser
{
    public class DjvuParser : IDjvuParser
    {
        public event ParsedDjvuElement ParsedDjvuElementEvent;
        public event ParsedDjvuNode ParsedDjvuNodeEvent;

        public IAbstractDocumentTree ParseDocument(IDjvuReader reader)
        {
            throw new NotImplementedException();
        }

        protected void OnParsedDjvuNode(IDjvuNode node)
        {
            if (ParsedDjvuNodeEvent != null)
                ParsedDjvuNodeEvent(this, new ParsedDjvuNodeEventArgs(node));
        }

        protected void OnParsedDjvuElement(IDjvuElement element)
        {
            if (ParsedDjvuElementEvent != null)
                ParsedDjvuElementEvent(this, new ParsedDjvuElementEventArgs(element));
        }
    }
}
