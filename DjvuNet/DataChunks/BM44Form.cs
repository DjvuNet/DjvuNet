using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Errors;

namespace DjvuNet.DataChunks
{
    public class BM44Form : DjvuFormElement, IBM44Form
    {
        public override ChunkType ChunkType { get { return ChunkType.BM44Form; } }

        #region Constructors

        public BM44Form(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public BM44Form(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base (writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        public override void WriteData(IDjvuWriter writer, bool writeHeader = true)
        {
            if (writer == null)
                throw new DjvuArgumentNullException(nameof(writer));

            AdjustAlignment(writer);

            if (writeHeader)
            {
                writer.WriteUTF8String("FORM");

                uint length = 0;
                foreach (IDjvuNode node in Children)
                {
                    length += ((uint)node.Length + node.OffsetDiff);
                }

                writer.WriteUInt32BigEndian(length);
                writer.WriteUTF8String("BM44");
            }

            foreach (IDjvuNode node in Children)
            {
                AdjustAlignment(writer);
                node.WriteData(writer, writeHeader);
            }
        }

        #endregion Methods
    }
}
