using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DjvuNet.Parser
{
    public interface IAbstractDocumentTree
    {
        IDjvuRootElement RootForm { get; }
    }
}
