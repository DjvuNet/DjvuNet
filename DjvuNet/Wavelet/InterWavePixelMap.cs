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
    public class InterWavePixelMap : InterWaveImage
    {
        #region Internal Fields

        internal int _CrCbDelay = 10;
        internal bool _CrCbHalf;

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

        public void Close()
        {
            _CSlice = _CBytes = _CSerial = 0;
        }

        public IPixelMap GetPixelMap()
        {
            if (_YMap == null)
                return null;

            int area = _YMap.Width * _YMap.Height;

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

            //h = h - 3;
            //w = area / h;

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