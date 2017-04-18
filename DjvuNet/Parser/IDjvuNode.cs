using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks;


namespace DjvuNet
{
    public interface IDjvuNode
    {
        ChunkType ChunkType { get; }

        string ChunkID { get; set; }

        long DataOffset { get; set; }

        long NodeOffset { get; set; }

        long Length { get; set; }

        void Initialize();

        void Initialize(IDjvuReader reader);

        bool IsInitialized { get; }

        byte[] ChunkData { get; set; }

        string Name { get; }

        IDjvuElement Parent { get; set; }
        
        IDjvuRootElement RootElement { get; set; }
        
        IDjvuDocument Document { get; set; }
        
        IDjvuReader Reader { get; set; }

        void ReadData(IDjvuReader reader);

        void WriteData(IDjvuWriter writer, bool writeHeader); 
    }
}
