using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Compression
{
    public abstract class BSBaseStream : Stream
    {

        protected const int Overflow = 32;

        /// <summary>
        /// Minimum block size
        /// </summary>
        protected const int MinBlock = 10;

        /// <summary>
        /// Maximum block size
        /// </summary>
        protected const int MaxBlock = 4096;

        /// <summary>
        /// Sorting threshold
        /// </summary>
        protected const int FreqMax = 4;

        /// <summary>
        /// Sorting threshold
        /// </summary>
        protected const int CTXIDS = 3;

        protected const int FreqS0 = 100000;

        protected const int FreqS1 = 1000000;

        /// <summary>
        /// Decoder to use
        /// </summary>
        internal IDataCoder Coder;

        public Stream BaseStream { get; internal set; }

        /// <summary>
        /// Values being coded
        /// </summary>
        protected byte[] _Cxt = new byte[300];

        /// <summary>
        /// Decoded data
        /// </summary>
        protected byte[] _Data;

        /// <summary>
        /// True if the EOF has been read
        /// </summary>
        protected bool _Eof;

        /// <summary>
        /// Block size of the data
        /// </summary>
        protected int _BlockSize;

        private int _BlockOffset;

        /// <summary>
        /// Offset into the data
        /// </summary>
        protected int BlockOffset
        {
            get { return _BlockOffset; }
            set
            {
                _BlockOffset = value;
            }
        }

        /// <summary>
        /// Size of the data read
        /// </summary>
        protected int _Size;

        protected long _Offset;

        public override bool CanRead
        {
            get { return BaseStream != null ? BaseStream.CanRead : false; }
        }

        public override bool CanSeek
        {
            get { return BaseStream != null ? BaseStream.CanSeek : false; }
        }

        public override bool CanWrite
        {
            get { return BaseStream != null ? BaseStream.CanWrite : false; }
        }

        public override long Length
        {
            get { return BaseStream != null ? BaseStream.Length : 0; }
        }

        public override long Position
        {
            get { return BaseStream != null ? BaseStream.Position : 0; }
            set
            {
                if (BaseStream != null)
                    BaseStream.Position = value;
                else
                    throw new InvalidOperationException();
            }
        }


        /// <summary>
        /// TODO docs
        /// </summary>
        public BSBaseStream() : base()
        {
            Init(new MemoryStream());
        }

        /// <summary>
        /// TODO docs
        /// </summary>
        /// <param name="input"></param>
        public BSBaseStream(Stream input)
        {
            Init(input);
        }

        public abstract BSBaseStream Init(Stream input);

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (BaseStream != null)
                return BaseStream.Seek(offset, origin);
            else
                throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            if (BaseStream != null)
                BaseStream.SetLength(value);
            else
                throw new InvalidOperationException();
        }
    }
}
