using System;
using System.Runtime.CompilerServices;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Utilities;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;

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
        public PixelMap GetPixelMapStaged()
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

            sbyte[] bytes = GC.AllocateUninitializedArray<sbyte>((int)totalBytes);

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

        public PixelMap GetPixelMapStaged(int subSample, Rectangle rect, PixelMap retVal)
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
        public unsafe PixelMap GetPixelMap(int subSample, Rectangle rect, PixelMap retVal)
        {
            Verify.SubsampleRange(subSample);
            if (_YMap == null) return null;

            if (retVal == null)
            {
                retVal = new PixelMap();
            }

            int width = rect.Width;
            int height = rect.Height;
            int pixSep = 3;
            long rowSizeInBytes = (long)width * pixSep;
            long totalBytes = height * rowSizeInBytes;

            if (totalBytes > Array.MaxLength || totalBytes < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Image dimensions result in an invalid Data buffer size (less than 0 or exceeding Array.MaxLength).");
            }

            sbyte[] imageData = retVal.Init(height, width).Data;

            short[] dataY = _YMap.DecodeToSpatial(subSample, rect, false, out Rectangle nrectY, out int datawY, out int targetWidthY);

            if (_CrMap != null && _CbMap != null && _CrCbDelay >= 0)
            {
                short[] dataCb = _CbMap.DecodeToSpatial(subSample, rect, _CrCbHalf, out Rectangle nrectCb, out int datawCb, out int targetWidthCb);
                short[] dataCr = _CrMap.DecodeToSpatial(subSample, rect, _CrCbHalf, out Rectangle nrectCr, out int datawCr, out int targetWidthCr);

                fixed (short* pDataY = dataY, pDataCb = dataCb, pDataCr = dataCr)
                fixed (sbyte* pImg8 = imageData)
                {
                    short* pY = pDataY + (nrectY.YMin * datawY) + nrectY.XMin;
                    short* pCb = pDataCb + (nrectCb.YMin * datawCb) + nrectCb.XMin;
                    short* pCr = pDataCr + (nrectCr.YMin * datawCr) + nrectCr.XMin;

                    InterWaveTransform.YCbCr2Rgb(pY, pCb, pCr, pImg8, targetWidthY, nrectY.Height, (int)rowSizeInBytes, datawY);
                }
            }
            else
            {
                fixed (short* pDataY = dataY)
                fixed (sbyte* pImg8 = imageData)
                {
                    short* pY = pDataY + (nrectY.YMin * datawY) + nrectY.XMin;
                    InterWaveTransform.YGray2Rgb(pY, pImg8, targetWidthY, nrectY.Height, (int)rowSizeInBytes, datawY);
                }
            }

            return retVal;
        }

        public unsafe PixelMap GetPixelMap()
        {
            if (_YMap == null) return null;

            int width = _YMap.Width;
            int height = _YMap.Height;
            int pixSep = 3;
            long rowSizeInBytes = (long)width * pixSep;
            long totalBytes = height * rowSizeInBytes;

            if (totalBytes > Array.MaxLength || totalBytes < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Image dimensions result in an invalid Data buffer size (less than 0 or exceeding Array.MaxLength).");
            }

            sbyte[] imageData = GC.AllocateUninitializedArray<sbyte>((int)totalBytes);
            
            // 1. Decode all spatial layers
            short[] dataY = _YMap.DecodeToSpatial(false);
            
            if (_CrMap != null && _CbMap != null && _CrCbDelay >= 0)
            {
                short[] dataCb = _CbMap.DecodeToSpatial(_CrCbHalf);
                short[] dataCr = _CrMap.DecodeToSpatial(_CrCbHalf);

                // 2. Execute fused YCbCr to BGR Scalar Pigeon Transform
                fixed (short* pY = dataY, pCb = dataCb, pCr = dataCr)
                fixed (sbyte* pImg8 = imageData)
                {
                    InterWaveTransform.YCbCr2Rgb(pY, pCb, pCr, pImg8, width, height, (int)rowSizeInBytes, _YMap.BlockWidth);
                }
            }
            else
            {
                // Fused grayscale path
                fixed (short* pY = dataY)
                fixed (sbyte* pImg8 = imageData)
                {
                    InterWaveTransform.YGray2Rgb(pY, pImg8, width, height, (int)rowSizeInBytes, _YMap.BlockWidth);
                }
            }

            return new PixelMap().Init(imageData, height, width);
        }

        #endregion Public Methods
    }
}
