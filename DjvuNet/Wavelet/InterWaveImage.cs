using System;
using System.Runtime.CompilerServices;
using DjvuNet.Errors;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    public class InterWaveImage : IInterWaveImage
    {
        #region Fields

        internal float _dBFrac;
        internal InterWaveMap _YMap;
        internal InterWaveMap _CbMap;
        internal InterWaveMap _CrMap;
        internal int _CSerial;
        internal int _CSlices;
        internal int _CBytes;

        #endregion Fields

        #region Properties

        public int Bytes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _CBytes; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set { _CBytes = value; }
        }

        public int Serial
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _CSerial; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set { _CSerial = value; }
        }

        public int Slices
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _CSlices; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set { _CSlices = value; }
        }

        public float DbFrac
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _dBFrac; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _dBFrac = value; }
        }

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

        #endregion Properties

        #region Constructors

        public InterWaveImage()
        {
            _dBFrac = 1.0f;
        }

        #endregion Constructors

        #region Methods

        public static InterWaveImage CreateDecoder(ImageType imageType = ImageType.Color)
        {
            switch (imageType)
            {
                case ImageType.Color:
                    return new InterWavePixelMapDecoder();
                case ImageType.Gray:
                    return new InterWaveBitmapDecoder();
                default:
                    throw new DjvuArgumentException($"Unsupported image type {imageType}", nameof(imageType));
            }
        }

        public static InterWaveImage CreateEncoder(ImageType imageType = ImageType.Color)
        {
            switch (imageType)
            {
                case ImageType.Color:
                    return new InterWavePixelMapEncoder();
                case ImageType.Gray:
                    return new InterWaveBitmapEncoder();
                default:
                    throw new DjvuArgumentException($"Unsupported image type {imageType}", nameof(imageType));
            }
        }

        public static InterWaveImage CreateEncoder(IBitmap bm, IBitmap mask = null, YCrCbMode mode = YCrCbMode.Normal)
        {
            throw new NotImplementedException();
        }

        public static InterWaveImage CreateEncoder(IPixelMap map, IBitmap mask = null, YCrCbMode mode = YCrCbMode.Normal)
        {
            throw new NotImplementedException();
        }

        #endregion Methods
    }
}
