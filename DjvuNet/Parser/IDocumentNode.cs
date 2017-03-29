using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DataChunks.Enums;

namespace DjvuNet.Parser
{
    public interface IDocumentNode
    {
        ChunkType ChunkType { get; }

        string ChunkID { get; }

        long Offset { get; set; }

        long Length { get; set; }

        bool IsInitialized { get; }

        byte[] Data { get; set; }

        IDocumentNode Parent { get; set; }

        List<IDocumentNode> Children { get; set; }
        
        IDocumentNode FirstChild { get; set; }
        
        IDocumentNode LastChild { get; set; }
        
        IDocumentNode PreviousSibling { get; set; }
        
        IDocumentNode NextSibling { get; set; }
        
        IDocumentNode FirstSibling { get; set; }
        
        IDocumentNode LastSibling { get; set; } 
    }
}
