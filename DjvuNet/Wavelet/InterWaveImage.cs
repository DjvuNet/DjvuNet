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
            get { return _CBytes; }
            internal set { _CBytes = value; }
        }

        public int Serial
        {
            get { return _CSerial; }
            internal set { _CSerial = value; }
        }

        public int Slices
        {
            get { return _CSlices; }
            internal set { _CSlices = value; }
        }

        public float DbFrac
        {
            get { return _dBFrac; }
            set { _dBFrac = value; }
        }

        public int Height
        {
            get { return (_YMap != null) ? _YMap.Height : 0; }
        }

        public int Width
        {
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
