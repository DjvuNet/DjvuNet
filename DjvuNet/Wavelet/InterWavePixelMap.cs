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

        public bool ImageData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return true; }
        }

        public int CrCbDelay
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _CrCbDelay; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _CrCbDelay = value; }
        }

        public bool CrCbHalf
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _CrCbHalf; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _CrCbHalf = value; }
        }

        #endregion Public Properties

        #region Constructors

        public InterWavePixelMap() { }

        #endregion Constructors

        #region Public Methods

        public void Close()
        {
            _CSlices = _CBytes = _CSerial = 0;
        }

        public IPixelMap GetPixelMap()
        {
            if (_YMap == null)
                return null;

            int area = _YMap.Width * _YMap.Height;

            int width = _YMap.Width;
            int height = _YMap.Height;
            int pixsep = 3;
            int rowsep = width * pixsep;
            sbyte[] bytes = new sbyte[height * rowsep];

            _YMap.Image(0, bytes, rowsep, pixsep, false);

            if ((_CrMap != null) && (_CbMap != null) && (_CrCbDelay >= 0))
            {
                _CbMap.Image(1, bytes, rowsep, pixsep, _CrCbHalf);
                _CrMap.Image(2, bytes, rowsep, pixsep, _CrCbHalf);
            }

            // Convert image to RGB
            IPixelMap pixelMap = new PixelMap().Init(bytes, height, width);
            
            if ((_CrMap != null) && (_CbMap != null) && (_CrCbDelay >= 0))
            {
                unsafe
                {
                    fixed(sbyte* pBuffer = bytes)
                    {
                        Pixel* pPix = (Pixel*)pBuffer;
                        InterWaveTransform.YCbCr2Rgb(pPix, width, height);
                    }
                }
            }
            else
            {
                IPixelReference pixel = pixelMap.CreateGPixelReference(0);
                for (int x = width * height; x-- > 0; pixel.IncOffset())
                    pixel.SetGray((sbyte)(127 - pixel.Blue));
            }

            return pixelMap;
        }

        public IPixelMap GetPixelMap(int subsample, Rectangle rect, IPixelMap retval)
        {
            if (_YMap == null)
                return null;

            if (retval == null)
                retval = new PixelMap();

            int width = rect.Width;
            int height = rect.Height;
            int pixsep = 3;
            int rowsep = width * pixsep;
            sbyte[] bytes = retval.Init(height, width, null).Data;

            _YMap.Image(subsample, rect, 0, bytes, rowsep, pixsep, false);

            if ((_CrMap != null) && (_CbMap != null) && (_CrCbDelay >= 0))
            {
                _CbMap.Image(subsample, rect, 1, bytes, rowsep, pixsep, _CrCbHalf);
                _CrMap.Image(subsample, rect, 2, bytes, rowsep, pixsep, _CrCbHalf);
            }

            if ((_CrMap != null) && (_CbMap != null) && (_CrCbDelay >= 0))
            {
                unsafe
                {
                    fixed (sbyte* pBuffer = bytes)
                    {
                        Pixel* pPix = (Pixel*)pBuffer;
                        InterWaveTransform.YCbCr2Rgb(pPix, width, height);
                    }
                }
            }
            else
            {
                IPixelReference pixel = retval.CreateGPixelReference(0);
                for (int x = width * height; x-- > 0; pixel.IncOffset())
                    pixel.SetGray((sbyte)(127 - pixel.Blue));
            }
           

            return retval;
        }

        #endregion Public Methods
    }
}