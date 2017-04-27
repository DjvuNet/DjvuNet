using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Compression
{
    public abstract class BSBaseStream : MemoryStream
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

        /// <summary>
        /// Values being coded
        /// </summary>
        protected byte[] _Context = new byte[300];

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


        /// <summary>
        /// TODO docs
        /// </summary>
        public BSBaseStream() : base()
        {
            Init((MemoryStream)this);
        }

        public BSBaseStream(int capacity) : base(capacity)
        {
            Init((MemoryStream)this);
        }

        public BSBaseStream(byte[] buffer) : base(buffer)
        {
            Init((MemoryStream)this);
        }

        //
        // Summary:
        //     Initializes a new non-resizable instance of the System.IO.MemoryStream class
        //     based on the specified byte array with the System.IO.MemoryStream.CanWrite property
        //     set as specified.
        //
        // Parameters:
        //   buffer:
        //     The array of unsigned bytes from which to create this stream.
        //
        //   writable:
        //     The setting of the System.IO.MemoryStream.CanWrite property, which determines
        //     whether the stream supports writing.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        public BSBaseStream(byte[] buffer, bool writable) : base(buffer, writable)
        {
            Init((MemoryStream)this);
        }
        //
        // Summary:
        //     Initializes a new non-resizable instance of the System.IO.MemoryStream class
        //     based on the specified region (index) of a byte array.
        //
        // Parameters:
        //   buffer:
        //     The array of unsigned bytes from which to create this stream.
        //
        //   index:
        //     The index into buffer at which the stream begins.
        //
        //   count:
        //     The length of the stream in bytes.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     index or count is less than zero.
        //
        //   T:System.ArgumentException:
        //     The buffer length minus index is less than count.
        public BSBaseStream(byte[] buffer, int index, int count) : base(buffer, index, count)
        {
            Init((MemoryStream)this);
        }
        //
        // Summary:
        //     Initializes a new non-resizable instance of the System.IO.MemoryStream class
        //     based on the specified region of a byte array, with the System.IO.MemoryStream.CanWrite
        //     property set as specified.
        //
        // Parameters:
        //   buffer:
        //     The array of unsigned bytes from which to create this stream.
        //
        //   index:
        //     The index in buffer at which the stream begins.
        //
        //   count:
        //     The length of the stream in bytes.
        //
        //   writable:
        //     The setting of the System.IO.MemoryStream.CanWrite property, which determines
        //     whether the stream supports writing.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     index or count are negative.
        //
        //   T:System.ArgumentException:
        //     The buffer length minus index is less than count.
        public BSBaseStream(byte[] buffer, int index, int count, bool writable) 
            : base(buffer, index, count, writable)
        {
            Init((MemoryStream)this);
        }
        //
        // Summary:
        //     Initializes a new instance of the System.IO.MemoryStream class based on the specified
        //     region of a byte array, with the System.IO.MemoryStream.CanWrite property set
        //     as specified, and the ability to call System.IO.MemoryStream.GetBuffer set as
        //     specified.
        //
        // Parameters:
        //   buffer:
        //     The array of unsigned bytes from which to create this stream.
        //
        //   index:
        //     The index into buffer at which the stream begins.
        //
        //   count:
        //     The length of the stream in bytes.
        //
        //   writable:
        //     The setting of the System.IO.MemoryStream.CanWrite property, which determines
        //     whether the stream supports writing.
        //
        //   publiclyVisible:
        //     true to enable System.IO.MemoryStream.GetBuffer, which returns the unsigned byte
        //     array from which the stream was created; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     index or count is negative.
        //
        //   T:System.ArgumentException:
        //     The buffer length minus index is less than count.
        public BSBaseStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) 
            : base(buffer, index, count, writable, publiclyVisible)
        {
            Init((MemoryStream)this);
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
    }
}
