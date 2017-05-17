using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Errors;

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

        public PM44Form(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        public override void WriteData(IDjvuWriter writer, bool writeHeader = true)
        {
            if (writer == null)
                throw new DjvuArgumentNullException(nameof(writer));

            if (writeHeader)
            {
                writer.WriteUTF8String("AT&T");
                writer.WriteUTF8String("FORM");

                uint length = 0;
                for (int i = 0; i < Children.Count; i++)
                {
                    IDjvuNode node = Children[i];
                    uint tempLength = (uint)node.Length;
                    length += (tempLength + node.OffsetDiff);
                    if (i + 1 < Children.Count)
                        length += tempLength % 2;
                }

                writer.WriteUInt32BigEndian(length + 4);
                writer.WriteUTF8String("PM44");
            }

            for (int i = 0; i < Children.Count; i++)
            {
                IDjvuNode node = Children[i];
                node.WriteData(writer, writeHeader);
            }
        }

        #endregion Methods
    }
}
