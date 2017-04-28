using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.DataChunks
{
    public class PM44Form : DjvuFormElement, IPM44Form
    {
        public override ChunkType ChunkType { get { return ChunkType.PM44Form; } }

        #region Constructors

        public PM44Form(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        public override void WriteData(IDjvuWriter writer, bool writeHeader = true)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (writeHeader)
            {
                writer.WriteUTF8String("FORM");

                uint length = 0;
                foreach (IDjvuNode node in Children)
                    length += ((uint)node.Length + node.OffsetDiff);

                writer.WriteUInt32BigEndian(length);
                writer.WriteUTF8String("PM44");
            }

            foreach (IDjvuNode node in Children)
                node.WriteData(writer, writeHeader);
        }
    }
}
