using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Compression;
using DjvuNet.DataChunks;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Parser;

namespace DjvuNet.Wavelet
{
    public class InterWavePixelMapEncoder : InterWavePixelMap
    {
        internal const float DecibelPrune = 5.0f;

        #region Fields

        internal InterWaveEncoder _CbEncoder;
        internal InterWaveEncoder _CrEncoder;
        internal InterWaveEncoder _YEncoder;

        #endregion Fields

        #region Methods

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

            // Prepare zcodec slices
            int flag = 1;
            int nslices = 0;

            {
                // Write primary header
                stream.WriteByte((byte)_CSerial);
                stream.WriteByte((byte)nslices);

                // Write extended header
                if (_CSerial == 0)
                {
                    byte major = InterWaveCodec.MajorVersion;
                    if (!(_CrMap != null && _CbMap != null))
                        major |= 0x80;

                    stream.WriteByte(major);
                    stream.WriteByte(InterWaveCodec.MinorVersion);

                    byte xhi = (byte)((_YMap.Width >> 8) & 0xff);
                    byte xlo = (byte)(_YMap.Width & 0xff);
                    byte yhi = (byte)((_YMap.Height >> 8) & 0xff);
                    byte ylo = (byte)(_YMap.Height & 0xff);
                    byte crCbDelay = (byte)(_CrCbHalf ? 0x00 : 0x80);
                    crCbDelay |= (byte)(_CrCbDelay >= 0 ? _CrCbDelay : 0x00);

                    stream.WriteByte(xhi);
                    stream.WriteByte(xlo);
                    stream.WriteByte(yhi);
                    stream.WriteByte(ylo);
                    stream.WriteByte(crCbDelay);
                }

                float estdb = -1.0f;
                ZPCodec zp = new ZPCodec(stream, true, true);

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
                            estdb = _YEncoder.EstimateDecibel(_dBFrac);
                    }

                    if (_CrEncoder != null && _CbEncoder != null && _CSlice + nslices >= _CrCbDelay)
                    {
                        flag |= _CbEncoder.CodeSlice(zp);
                        flag |= _CrEncoder.CodeSlice(zp);
                    }

                    nslices++;
                }

                zp.Flush();
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
            if (_YEncoder != null)
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
            Close();
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
        public unsafe void InitializeEncoder(IPixelMap pm, Bitmap gmask = null, YCrCbMode crcbmode = YCrCbMode.Normal)
        {
            /* Free */
            CloseEncoder();
            /* Create */
            int w = pm.ImageWidth;
            int h = pm.ImageHeight;
            sbyte[] sBuffer = new sbyte[w * h];
            GCHandle hBuffer = GCHandle.Alloc(sBuffer, GCHandleType.Pinned);
            sbyte* buffer = (sbyte*)hBuffer.AddrOfPinnedObject();

            // Create maps
            InterWaveMapEncoder eymap = new InterWaveMapEncoder(w, h);
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
            GCHandle hMask = default(GCHandle);

            if (mask != null)
            {
                hMask = GCHandle.Alloc(mask.Data, GCHandleType.Pinned);
                msk8 = (sbyte*)hMask.AddrOfPinnedObject();
                mskrowsize = mask.GetRowSize();
            }

            GCHandle hData = GCHandle.Alloc(pm.Data, GCHandleType.Pinned);
            Pixel* pData = (Pixel*)hData.AddrOfPinnedObject();

            // Fill buffer with luminance information
            InterWaveTransform.Rgb2Y(pData, w, h, pm.GetRowSize(), buffer, w);

            if (_CrCbDelay < 0)
            {
                // Inversion for gray images
                sbyte* e = buffer + w * h;
                for (sbyte* b = buffer; b < e; b++)
                    *b = (sbyte)(255 - *b);
            }

            // Create YMAP
            eymap.Create(buffer, w, msk8, mskrowsize);

            // Create chrominance maps
            if (_CrCbDelay >= 0)
            {
                InterWaveMapEncoder ecbmap = new InterWaveMapEncoder(w, h);
                _CbMap = ecbmap;

                InterWaveMapEncoder ecrmap = new InterWaveMapEncoder(w, h);
                _CrMap = ecrmap;

                // Process CB information
                InterWaveTransform.Rgb2Cb(pData, w, h, pm.GetRowSize(), buffer, w);
                ecbmap.Create(buffer, w, msk8, mskrowsize);

                // Process CR information
                InterWaveTransform.Rgb2Cr(pData, w, h, pm.GetRowSize(), buffer, w);
                ecrmap.Create(buffer, w, msk8, mskrowsize);

                // Perform chrominance reduction (CRCBhalf)
                if (_CrCbHalf)
                {
                    ecbmap.Slashres(2);
                    ecrmap.Slashres(2);
                }
            }

            if (hMask.IsAllocated)
                hMask.Free();

            hData.Free();
            hBuffer.Free();
        }

        #endregion Methods

    }
}
