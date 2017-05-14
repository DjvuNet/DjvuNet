using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Errors;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    public class InterWaveImage
    {
        #region Fields

        internal float _dBFrac;
        internal InterWaveMap _YMap;
        internal InterWaveMap _CbMap;
        internal InterWaveMap _CrMap;
        internal int _CSerial;
        internal int _CSlice;
        internal int _CBytes;

        #endregion Fields

        #region Constructors

        public InterWaveImage()
        {
            _dBFrac = 1.0f;
        }

        #endregion Constructors

        #region Methods

        public static InterWaveImage CreateDecoder(ImageType imgType = ImageType.Color)
        {
            switch (imgType)
            {
                case ImageType.Color:
                    return new InterWavePixelMap();
                case ImageType.Gray:
                    return new InterWaveBitmap();
                default:
                    throw new DjvuArgumentException($"Unsupported image type {imgType}", nameof(imgType));
            }
        }

        public static InterWaveImage CreateEncoder(ImageType imgType = ImageType.Color)
        {
            throw new NotImplementedException();
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
