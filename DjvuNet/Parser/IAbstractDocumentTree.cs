﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Parser
{
    public interface IAbstractDocumentTree
    {
        IDocumentRootElement RootForm { get; }
    }
}
