using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Parser
{
    public class AbstractDocumentTree : IAbstractDocumentTree
    {
        public IDocumentRootElement RootForm
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
