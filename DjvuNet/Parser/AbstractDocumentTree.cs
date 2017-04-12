using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


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
