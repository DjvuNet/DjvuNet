using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DjvuNet.Compression;
using DjvuNet.Configuration;
using DjvuNet.DataChunks;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Interfaces;
using DjvuNet.Parser;

namespace DjvuNet.Wavelet
{
    /// <summary>
    /// This class represents structured wavelet data.
    /// </summary>
    public class InterWavePixelMap : ICodec, IInterWavePixelMap
    {
        #region Internal Fields

        internal InterWaveCodec _CbCodec;
        internal InterWaveEncoder _CbEncoder;
        internal InterWaveMap _CbMap;
        internal int _CBytes;
        internal int _CrCbDelay = 10;
        internal bool _CrCbHalf;
        internal InterWaveCodec _CrCodec;
        internal InterWaveEncoder _CrEncoder;
        internal InterWaveMap _CrMap;
        internal int _CSerial;
        internal int _CSlice;
        internal InterWaveCodec _YCodec;
        internal InterWaveEncoder _YEncoder;
        internal InterWaveMap _YMap;
        internal float db_frac;

        internal const float DecibelPrune = 5.0f;

        #endregion Internal Fields

        #region Public Properties

        public int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (_YMap != null) ? _YMap.Height : 0; }
        }

        public int Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (_YMap != null) ? _YMap.Width : 0; }
        }

        public bool ImageData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return true; }
        }

        #endregion Public Properties

        #region Constructors

        public InterWavePixelMap() { }

        #endregion Constructors

        #region Public Methods

        public void Decode(IBinaryReader reader)
        {
            if (_YCodec == null)
            {
                _CSlice = _CSerial = 0;
                _YMap = null;
            }

            byte serial = reader.ReadByte();
            byte slices = reader.ReadByte();

            if (serial != _CSerial)
                throw new DjvuFormatException(
                    $"{nameof(IInterWavePixelMap)} received out of order data. Expected serial number {_CSerial}, actual {serial}");

            int nslices = _CSlice + slices;

            if (_CSerial == 0)
            {
                int major = reader.ReadByte();
                int minor = reader.ReadByte();

                if ((major & 0x7f) != InterWaveCodec.MajorVersion)
                    throw new DjvuFormatException("File has been compressed with an incompatible codec");

                if (minor > InterWaveCodec.MinorVersion)
                    throw new DjvuFormatException("File has been compressed with a more recent codec");

                int w = (reader.ReadByte() << 8);
                w |= reader.ReadByte();

                int h = (reader.ReadByte() << 8);
                h |= reader.ReadByte();

                int crcbDelay = 0;

                if ((major & 0x7f) == 1 && minor >= 2)
                {
                    crcbDelay = reader.ReadByte();
                    if (minor >= 2)
                        _CrCbDelay = (crcbDelay & 0x7f);
                }

                if (minor >= 2)
                    _CrCbHalf = ((crcbDelay & 0x80) != 0 ? false : true);

                if ((major & 0x80) != 0)
                    _CrCbDelay = -1;

                _YMap = new InterWaveMap(w, h);
                _YCodec = new InterWaveCodec().Init(_YMap);

                if (_CrCbDelay >= 0)
                {
                    _CbMap = new InterWaveMap(w, h);
                    _CrMap = new InterWaveMap(w, h);
                    _CbCodec = new InterWaveCodec().Init(_CbMap);
                    _CrCodec = new InterWaveCodec().Init(_CrMap);
                }
            }

            IDataCoder coder = DjvuSettings.Current.CoderFactory.CreateCoder(reader.BaseStream, false);

            for (int flag = 1; flag != 0 && _CSlice < nslices; _CSlice++)
            {
                flag = _YCodec.CodeSlice(coder);

                if (_CrCodec != null && _CbCodec != null && _CrCbDelay <= _CSlice)
                {
                    flag |= _CbCodec.CodeSlice(coder);
                    flag |= _CrCodec.CodeSlice(coder);
                }
            }

            _CSerial++;
        }

        /// <summary>
        /// Encodes one data chunk into output stream. Settings parameter controls
        /// how much data is generated.The chunk data is written to Stream
        /// with no IFF header.  Successive calls to EncodeChunk encode
        /// successive chunks.You must call CloseCodec after encoding the last
        /// chunk of a file.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public int EncodeChunk(Stream stream, InterWaveEncoderSettings settings)
        {
            // Check
            if (settings.Slices == 0 && settings.Bytes == 0 && settings.Decibels == 0)
                throw new DjvuArgumentException("Encoder needs stop condition", nameof(settings));

            if (_YMap == null)
                throw new DjvuInvalidOperationException($"Cannot encode! Target encoder map is null {nameof(_YMap)}");

            // Open
            if (_YEncoder == null)
            {
                _CSlice = _CSerial = _CBytes = 0;
                _YEncoder = new InterWaveEncoder(_YMap);
                if (_CrMap != null && _CbMap != null)
                {
                    _CbEncoder = new InterWaveEncoder(_CbMap);
                    _CrEncoder = new InterWaveEncoder(_CrMap);
                }
            }

            // Adjust cbytes
            _CBytes += 2; // size of primary header
            if (_CSerial == 0)
                _CBytes += 7; // sum of secondary and tertiary header sizes
             
            // Prepare zcodec slices
            int flag = 1;
            int nslices = 0;

            {
                float estdb = -1.0f;
                ZPCodec gzp = new ZPCodec(stream, true, true);
                ZPCodec zp = gzp;
                while (flag != 0)
                {
                    if (settings.Decibels > 0 && estdb >= settings.Decibels)
                        break;

                    if (settings.Bytes > 0 && stream.Position + _CBytes >= settings.Bytes)
                        break;

                    if (settings.Slices > 0 && nslices + _CSlice >= settings.Slices)
                        break;

                    flag = _YEncoder.CodeSlice(zp);

                    if (flag != 0 && settings.Decibels > 0)
                    {
                        if (_YEncoder._CurrentBand == 0 || estdb >= settings.Decibels - DecibelPrune)
                            estdb = _YEncoder.EstimateDecibel(db_frac);
                    }

                    if (_CrEncoder != null && _CbEncoder != null && _CSlice + nslices >= _CrCbDelay)
                    {
                        flag |= _CbEncoder.CodeSlice(zp);
                        flag |= _CrEncoder.CodeSlice(zp);
                    }
                    nslices++;
                }
            }

            // Write primary header
            stream.WriteByte((byte)_CSerial);
            stream.WriteByte((byte)nslices);

            // Write extended header
            if (_CSerial == 0)
            {
                byte major = InterWaveCodecBase.MajorVersion;
                if (!(_CrMap != null && _CbMap != null))
                    major |= 0x80;

                stream.WriteByte(major);
                stream.WriteByte(InterWaveCodecBase.MinorVersion);

                byte xhi = (byte)((_YMap.Width >> 8) & 0xff);
                byte xlo = (byte)(_YMap.Width & 0xff);
                byte yhi = (byte)((_YMap.Height >> 8) & 0xff);
                byte ylo = (byte)(_YMap.Height & 0xff);
                byte crCbDelay = (byte) (_CrCbHalf ? 0x00 : 0x80);
                crCbDelay |= (byte) (_CrCbDelay >= 0 ? _CrCbDelay : 0x00);

                stream.WriteByte(xhi);
                stream.WriteByte(xlo);
                stream.WriteByte(yhi);
                stream.WriteByte(ylo);
                stream.WriteByte(crCbDelay);
            }

            // Write slices
            //mbs.seek(0);
            //gbs->copy(mbs);

            // Return -> length is added for the second time ?
            _CBytes += (int)stream.Position;
            _CSlice += nslices;
            _CSerial += 1;
            return flag;        
        }

        /// <summary>
        /// Writes a color image into a DjVu IW44 file. This function creates a
        /// composite PM44Form element composed of PM44 nodes. Data for each chunk is generated with
        /// EncodeChunk using the corresponding parameters in array settings
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public DjvuFormElement EncodeImage(IDjvuWriter writer, int nchunks, InterWaveEncoderSettings[] settings,
            ChunkType formType = ChunkType.PM44Form, ChunkType nodeType = ChunkType.PM44)
        {
            if (_YCodec != null)
                throw new DjvuInvalidOperationException($"Encoder already exists or left open from previous operation.");

            int flag = 1;

            PM44Form form = (PM44Form)DjvuParser.CreateEncodedDjvuNode(writer, null, formType, 0);

            for (int i = 0; flag != 0 && i < nchunks; i++)
            {
                byte[] data = null;
                using (MemoryStream stream = new MemoryStream())
                {
                    flag = EncodeChunk(stream, settings[i]);
                    data = new byte[stream.Position];
                    Buffer.BlockCopy(stream.GetBuffer(), 0, data, 0, data.Length);
                }
                PM44Chunk chunk = (PM44Chunk)DjvuParser.CreateEncodedDjvuNode(writer, form, nodeType, data.Length);
                chunk.ChunkData = data;
                form.AddChild(chunk);
            }

            CloseEncoder();
            return form;
        }

        /// <summary>
        /// Release resources for garbage collection
        /// </summary>
        public void CloseEncoder()
        {
            _YEncoder = null;
            _CbEncoder = null;
            _CrEncoder = null;
        }

        /// <summary>
        /// Initializes an InterWavePixelMap with color image #bm#.  This constructor
        /// performs the wavelet decomposition of image #bm# and records the
        /// corresponding wavelet coefficients. Argument #mask# is an optional
        /// bilevel image specifying the masked pixels(see \Ref{ IW44Image.h}).
        /// Argument #crcbmode# specifies how the chrominance information should be
        /// encoded(see \Ref{ CRCBMode}).
        /// </summary>
        /// <param name="bm"></param>
        /// <param name="mask"></param>
        /// <param name="mode"></param>
        public unsafe void InitializeEncoder(PixelMap pm, Bitmap gmask = null, YCrCbMode crcbmode = YCrCbMode.Normal)
        {
            /* Free */
            CloseEncoder();
            /* Create */
            int w = pm.ImageWidth;
            int h = pm.ImageHeight;
            sbyte[] sBuffer = new sbyte[w * h];
            GCHandle hBuffer = GCHandle.Alloc(sBuffer, GCHandleType.Pinned);
            sbyte* buffer = (sbyte*) hBuffer.AddrOfPinnedObject();

            // Create maps
            InterWaveMap eymap = new InterWaveMap(w, h);
            _YMap = eymap;

            // Handle CRCB mode
            switch (crcbmode)
            {
                case YCrCbMode.None:
                    _CrCbHalf = true;
                    _CrCbDelay = -1;
                    break;
                case YCrCbMode.Half:
                    _CrCbHalf = true;
                    _CrCbDelay = 10;
                    break;
                case YCrCbMode.Normal:
                    _CrCbHalf = false;
                    _CrCbDelay = 10;
                    break;
                case YCrCbMode.Full:
                    _CrCbHalf = false;
                    _CrCbDelay = 0;
                    break;
            }
            // Prepare mask information
            sbyte* msk8 = (sbyte*)0;
            int mskrowsize = 0;

            Bitmap mask = gmask;
            if (mask != null)
            {
                //msk8 = (signed char const *)((*mask)[0]);
                mskrowsize = mask.GetRowSize();
            }

            GCHandle hData = GCHandle.Alloc(pm.Data, GCHandleType.Pinned);
            Pixel* pData = (Pixel*)hData.AddrOfPinnedObject();

            // Fill buffer with luminance information
            InterWaveTransform.RGB_to_Y(pData, w, h, pm.GetRowSize(), buffer, w);

            if (_CrCbDelay < 0)
            {
                // Inversion for gray images
                sbyte* e = buffer + w * h;
                for (sbyte* b = buffer; b < e; b++)
                    *b = (sbyte)(255 - *b);
            }

            // Create YMAP
            eymap.create(buffer, w, msk8, mskrowsize);

            // Create chrominance maps
            if (_CrCbDelay >= 0)
            {
                InterWaveMap ecbmap = new InterWaveMap(w, h);
                _CbMap = ecbmap;

                InterWaveMap ecrmap = new InterWaveMap(w, h);
                _CrMap = ecrmap;

                // Process CB information
                InterWaveTransform.RGB_to_Cb(pData, w, h, pm.GetRowSize(), buffer, w);
                ecbmap.create(buffer, w, msk8, mskrowsize);

                // Process CR information
                InterWaveTransform.RGB_to_Cr(pData, w, h, pm.GetRowSize(), buffer, w);
                ecrmap.create(buffer, w, msk8, mskrowsize);

                // Perform chrominance reduction (CRCBhalf)
                if (_CrCbHalf)
                {
                    ecbmap.Slashres(2);
                    ecrmap.Slashres(2);
                }
            }

            hData.Free();
            hBuffer.Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CloseCodec()
        {
            _YCodec = _CrCodec = _CbCodec = null;
            _CSlice = _CBytes = _CSerial = 0;
        }

        public IPixelMap GetPixelMap()
        {
            if (_YMap == null)
                return null;

            int w = _YMap.Width;
            int h = _YMap.Height;
            int pixsep = 3;
            int rowsep = w * pixsep;
            sbyte[] bytes = new sbyte[h * rowsep];

            _YMap.Image(0, bytes, rowsep, pixsep, false);

            if ((_CrMap != null) && (_CbMap != null) && (_CrCbDelay >= 0))
            {
                _CbMap.Image(1, bytes, rowsep, pixsep, _CrCbHalf);
                _CrMap.Image(2, bytes, rowsep, pixsep, _CrCbHalf);
            }

            // Convert image to RGB
            IPixelMap pixelMap = new PixelMap().Init(bytes, h, w);
            IPixelReference pixel = pixelMap.CreateGPixelReference(0);

            for (int i = 0; i < h; )
            {
                pixel.SetOffset(i++, 0);

                if ((_CrMap != null) && (_CbMap != null) && (_CrCbDelay >= 0))
                    pixel.Ycc2Rgb(w);
                else
                {
                    for (int x = w; x-- > 0; pixel.IncOffset())
                        pixel.SetGray((sbyte)(127 - pixel.Blue));
                }
            }

            return pixelMap;
        }

        public IPixelMap GetPixelMap(int subsample, Rectangle rect, IPixelMap retval)
        {
            if (_YMap == null)
                return null;

            if (retval == null)
                retval = new PixelMap();

            int w = rect.Width;
            int h = rect.Height;
            int pixsep = 3;
            int rowsep = w * pixsep;
            sbyte[] bytes = retval.Init(h, w, null).Data;

            _YMap.Image(subsample, rect, 0, bytes, rowsep, pixsep, false);

            if ((_CrMap != null) && (_CbMap != null) && (_CrCbDelay >= 0))
            {
                _CbMap.Image(subsample, rect, 1, bytes, rowsep, pixsep, _CrCbHalf);
                _CrMap.Image(subsample, rect, 2, bytes, rowsep, pixsep, _CrCbHalf);
            }

            IPixelReference pixel = retval.CreateGPixelReference(0);            

            for (int i = 0; i < h; )
            {
                pixel.SetOffset(i++, 0);

                if ((_CrMap != null) && (_CbMap != null) && (_CrCbDelay >= 0))
                    pixel.Ycc2Rgb(w);
                else
                {
                    for (int x = w; x-- > 0; pixel.IncOffset())
                        pixel.SetGray((sbyte)(127 - pixel.Blue));
                }
            }            

            return retval;
        }

        #endregion Public Methods
    }
}