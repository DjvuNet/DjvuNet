using System;
using System.IO;
using System.Runtime.CompilerServices;
using DjvuNet.Compression;
using DjvuNet.Graphics;
using DjvuNet.Interfaces;

namespace DjvuNet.Wavelet
{
    /// <summary>
    /// This class represents structured wavelette data.
    /// </summary>
    public class IWPixelMap : ICodec
    {
        #region Private Fields

        private IWCodec _CbCodec;
        private IWMap _CbMap;
        private int _cbytes;
        private int _CrCbDelay = 10;
        private bool _CrCbHalf;
        private IWCodec _CrCodec;
        private IWMap _CrMap;
        private int _CSerial;
        private int _CSlice;
        private IWCodec _YCodec;
        private IWMap _YMap;

        #endregion Private Fields

        #region Public Properties

        public int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (_YMap != null) ? _YMap.Ih : 0; }
        }

        public int Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (_YMap != null) ? _YMap.Iw : 0; }
        }

        public bool ImageData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return true; }
        }

        #endregion Public Properties

        #region Constructors

        public IWPixelMap() { }

        #endregion Constructors

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    $"{nameof(IWPixelMap)} received out of order data. Expected serial number {_CSerial}, actual {serial}");

            int nslices = _CSlice + slices;

            if (_CSerial == 0)
            {
                int major = reader.ReadByte();
                int minor = reader.ReadByte();

                if ((major & 0x7f) != IWCodec.MajorVersion)
                    throw new DjvuFormatException("File has been compressed with an incompatible codec");

                if (minor > IWCodec.MinorVersion)
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

                _YMap = new IWMap().Init(w, h);
                _YCodec = new IWCodec().Init(_YMap);

                if (_CrCbDelay >= 0)
                {
                    _CbMap = new IWMap().Init(w, h);
                    _CrMap = new IWMap().Init(w, h);
                    _CbCodec = new IWCodec().Init(_CbMap);
                    _CrCodec = new IWCodec().Init(_CrMap);
                }
            }

            ZPCodec zp = new ZPCodec().Initializa(reader.BaseStream);

            for (int flag = 1; flag != 0 && _CSlice < nslices; _CSlice++)
            {
                flag = _YCodec.CodeSlice(zp);

                if (_CrCodec != null && _CbCodec != null && _CrCbDelay <= _CSlice)
                {
                    flag |= _CbCodec.CodeSlice(zp);
                    flag |= _CrCodec.CodeSlice(zp);
                }
            }

            _CSerial++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CloseCodec()
        {
            _YCodec = _CrCodec = _CbCodec = null;
            _CSlice = _cbytes = _CSerial = 0;
        }

        public PixelMap GetPixelMap()
        {
            if (_YMap == null)
                return null;

            int w = _YMap.Iw;
            int h = _YMap.Ih;
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
            PixelMap pixelMap = new PixelMap().Init(bytes, h, w);
            PixelReference pixel = pixelMap.CreateGPixelReference(0);

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

        public PixelMap GetPixelMap(int subsample, Rectangle rect, PixelMap retval)
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

            PixelReference pixel = retval.CreateGPixelReference(0);            

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