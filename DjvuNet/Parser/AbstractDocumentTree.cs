using System;

namespace DjvuNet.Parser
{
    public class AbstractDocumentTree : IAbstractDocumentTree
    {
        public IDjvuRootElement RootForm
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
