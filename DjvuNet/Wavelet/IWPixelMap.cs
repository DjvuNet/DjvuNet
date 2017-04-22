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
        #region Protected Members

        private IWCodec _cbcodec;
        private IWMap _cbmap;
        private int _cbytes;
        private int _crcbDelay = 10;
        private bool _crcbHalf;
        private IWCodec _crcodec;
        private IWMap _crmap;
        private int _cserial;
        private int _cslice;
        private IWCodec _ycodec;
        private IWMap _ymap;

        #endregion Protected Members

        #region Public Properties

        //public const int CRCBfull = 5;
        //public const int CRCBhalf = 3;
        //public const int CRCBMode = 1;
        //public const int CRCBnone = 2;
        //public const int CRCBnormal = 4;

        //public static readonly float[][] rgb_to_ycc = new[]
        //                                                  {
        //                                                      new[] {0.304348F, 0.608696F, 0.086956F},
        //                                                      new[] {0.463768F, -0.405797F, -0.057971F},
        //                                                      new[] {-0.173913F, -0.347826F, 0.521739F}
        //                                                  };

        #region Height

        public virtual int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (_ymap != null) ? _ymap.Ih : 0; }
        }

        #endregion Height

        #region Width

        public virtual int Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (_ymap != null) ? _ymap.Iw : 0; }
        }

        #endregion Width

        #region ImageData

        public virtual bool ImageData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return true; }
        }

        #endregion ImageData

        #endregion Public Properties

        #region Constructors

        #endregion Constructors

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Decode(IBinaryReader bs)
        {
            if (_ycodec == null)
            {
                _cslice = _cserial = 0;
                _ymap = null;
            }

            byte serial = bs.ReadByte();
            if (serial != _cserial)
                throw new DjvuFormatException("Chunk does not bear expected serial number");

            int nslices = _cslice + bs.ReadByte();

            if (_cserial == 0)
            {
                int major = bs.ReadByte();
                int minor = bs.ReadByte();

                if ((major & 0x7f) != 1)
                    throw new DjvuFormatException("File has been compressed with an incompatible Codec");

                if (minor > 2)
                    throw new DjvuFormatException("File has been compressed with a more recent Codec");

                //int header3size = 5;

                //if (minor < 2)
                //{
                //    header3size = 4;
                //}

                int w = (bs.ReadByte() << 8);
                w |= bs.ReadByte();

                int h = (bs.ReadByte() << 8);
                h |= bs.ReadByte();
                _crcbDelay = 0;
                _crcbHalf = false;

                int b = bs.ReadByte();

                if (minor >= 2)
                    _crcbDelay = 0x7f & b;

                if (minor >= 2)
                    _crcbHalf = ((0x80 & b) == 0);

                if ((major & 0x80) != 0)
                    _crcbDelay = -1;

                _ymap = new IWMap().Init(w, h);
                _ycodec = new IWCodec().Init(_ymap);

                if (_crcbDelay >= 0)
                {
                    _cbmap = new IWMap().Init(w, h);
                    _crmap = new IWMap().Init(w, h);
                    _cbcodec = new IWCodec().Init(_cbmap);
                    _crcodec = new IWCodec().Init(_crmap);
                }
            }

            ZPCodec zp = new ZPCodec().Initializa(bs.BaseStream);

            for (int flag = 1; (flag != 0) && (_cslice < nslices); _cslice++)
            {
                flag = _ycodec.CodeSlice(zp);

                if ((_crcodec != null) && (_cbcodec != null) && (_crcbDelay <= _cslice))
                {
                    flag |= _cbcodec.CodeSlice(zp);
                    flag |= _crcodec.CodeSlice(zp);
                }
            }

            _cserial++;

            //    return nslices;
        }

        public virtual void CloseCodec()
        {
            _ycodec = _crcodec = _cbcodec = null;
            _cslice = _cbytes = _cserial = 0;

            GC.Collect();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Graphics.PixelMap GetPixmap()
        {
            if (_ymap == null)
                return null;

            int w = _ymap.Iw;
            int h = _ymap.Ih;
            int pixsep = 3;
            int rowsep = w * pixsep;
            sbyte[] bytes = new sbyte[h * rowsep];

            _ymap.Image(0, bytes, rowsep, pixsep, false);

            if ((_crmap != null) && (_cbmap != null) && (_crcbDelay >= 0))
            {
                _cbmap.Image(1, bytes, rowsep, pixsep, _crcbHalf);
                _crmap.Image(2, bytes, rowsep, pixsep, _crcbHalf);
            }

            // Convert image to RGB
            Graphics.PixelMap ppm = new Graphics.PixelMap().Init(bytes, h, w);
            PixelReference pixel = ppm.CreateGPixelReference(0);

            for (int i = 0; i < h; )
            {
                pixel.SetOffset(i++, 0);

                if ((_crmap != null) && (_cbmap != null) && (_crcbDelay >= 0))
                    pixel.Ycc2Rgb(w);
                else
                {
                    for (int x = w; x-- > 0; pixel.IncOffset())
                        pixel.SetGray((sbyte)(127 - pixel.Blue));
                }
            }

            return ppm;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Graphics.PixelMap GetPixmap(int subsample, Rectangle rect, Graphics.PixelMap retval)
        {
            if (_ymap == null)
                return null;

            if (retval == null)
                retval = new Graphics.PixelMap();

            int w = rect.Width;
            int h = rect.Height;
            int pixsep = 3;
            int rowsep = w * pixsep;
            sbyte[] bytes = retval.Init(h, w, null).Data;

            _ymap.Image(subsample, rect, 0, bytes, rowsep, pixsep, false);

            if ((_crmap != null) && (_cbmap != null) && (_crcbDelay >= 0))
            {
                _cbmap.Image(subsample, rect, 1, bytes, rowsep, pixsep, _crcbHalf);
                _crmap.Image(subsample, rect, 2, bytes, rowsep, pixsep, _crcbHalf);
            }

            PixelReference pixel = retval.CreateGPixelReference(0);            

            for (int i = 0; i < h; )
            {
                pixel.SetOffset(i++, 0);

                if ((_crmap != null) && (_cbmap != null) && (_crcbDelay >= 0))
                    pixel.Ycc2Rgb(w);
                else
                {
                    for (int x = w; x-- > 0; pixel.IncOffset())
                        pixel.SetGray((sbyte)(127 - pixel.Blue));
                }
            }            

            return retval;
        }

        ///// <summary> Set the CRCB Delay
        /////
        ///// </summary>
        ///// <param name="value">the new CRCB delay value.
        /////
        ///// </param>
        ///// <returns> the CRCB delay value
        ///// </returns>
        //public virtual int SetCrcbDelay(int value_Renamed)
        //{
        //    if (value_Renamed >= 0)
        //    {
        //        CrcbDelay = value_Renamed;
        //    }

        //    return CrcbDelay;
        //}

        #endregion Public Methods
    }
}