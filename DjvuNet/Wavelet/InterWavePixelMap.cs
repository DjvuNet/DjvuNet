using System;
using System.Runtime.CompilerServices;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Utilities;

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

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public PixelMap GetPixelMap()
        {
            if (_YMap == null)
                return null;

            int area = _YMap.Width * _YMap.Height;

            int width = _YMap.Width;
            int height = _YMap.Height;
            int pixSep = 3;
            long rowSep = (long)width * pixSep;
            long totalBytes = height * rowSep;

            if (totalBytes > Array.MaxLength)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Image dimensions result in an invalid Data buffer size (exceeding Array.MaxLength).");
            }

            sbyte[] bytes = new sbyte[(int)totalBytes];

            _YMap.Image(0, bytes, (int)rowSep, pixSep, false);

            if ((_CrMap != null) && (_CbMap != null) && (_CrCbDelay >= 0))
            {
                _CbMap.Image(1, bytes, (int)rowSep, pixSep, _CrCbHalf);
                _CrMap.Image(2, bytes, (int)rowSep, pixSep, _CrCbHalf);
            }

            // Convert image to RGB
            PixelMap pixelMap = new PixelMap().Init(bytes, height, width);

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

        public PixelMap GetPixelMap(int subSample, Rectangle rect, PixelMap retVal)
        {
            Verify.SubsampleRange(subSample);

            if (_YMap == null)
                return null;

            if (retVal == null)
            {
                retVal = new PixelMap();
            }

            int width = rect.Width;
            int height = rect.Height;
            int pixSep = 3;
            long rowSep = (long)width * pixSep;
            long totalBytes = height * rowSep;

            if (totalBytes > Array.MaxLength)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Image dimensions result in an invalid Data buffer size (exceeding Array.MaxLength).");
            }

            sbyte[] bytes = retVal.Init(height, width).Data;

            _YMap.Image(subSample, rect, 0, bytes, (int)rowSep, pixSep, false);

            if ((_CrMap != null) && (_CbMap != null) && (_CrCbDelay >= 0))
            {
                _CbMap.Image(subSample, rect, 1, bytes, (int)rowSep, pixSep, _CrCbHalf);
                _CrMap.Image(subSample, rect, 2, bytes, (int)rowSep, pixSep, _CrCbHalf);
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
                IPixelReference pixel = retVal.CreateGPixelReference(0);
                for (int x = width * height; x-- > 0; pixel.IncOffset())
                    pixel.SetGray((sbyte)(127 - pixel.Blue));
            }

            return retVal;
        }

        #endregion Public Methods
    }
}
