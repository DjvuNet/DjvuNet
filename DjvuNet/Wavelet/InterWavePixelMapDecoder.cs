using System.Runtime.CompilerServices;
using DjvuNet.Compression;
using DjvuNet.Configuration;

namespace DjvuNet.Wavelet
{
    public class InterWavePixelMapDecoder : InterWavePixelMap, IInterWavePixelMap
    {
        #region Fields

        internal InterWaveDecoder _CbDecoder;
        internal InterWaveDecoder _CrDecoder;
        internal InterWaveDecoder _YDecoder;

        #endregion Fields

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CloseCodec()
        {
            _YDecoder = _CrDecoder = _CbDecoder = null;
            Close();
        }

        public void Decode(IBinaryReader reader)
        {
            if (_YDecoder == null)
            {
                _CSlices = _CSerial = 0;
                _YMap = null;
            }

            byte serial = reader.ReadByte();
            byte slices = reader.ReadByte();

            if (serial != _CSerial)
                throw new DjvuFormatException(
                    $"{nameof(IInterWavePixelMap)} received out of order data. Expected serial number {_CSerial}, actual {serial}");

            int nslices = _CSlices + slices;

            if (_CSerial == 0)
            {
                int major = reader.ReadByte();
                int minor = reader.ReadByte();

                if ((major & 0x7f) != InterWaveCodec.MajorVersion)
                    throw new DjvuFormatException("File has been compressed with an incompatible codec");

                if (minor > InterWaveCodec.MinorVersion)
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

                _YMap = new InterWaveMap(w, h);
                _YDecoder = new InterWaveDecoder(_YMap);

                if (_CrCbDelay >= 0)
                {
                    _CbMap = new InterWaveMap(w, h);
                    _CrMap = new InterWaveMap(w, h);
                    _CbDecoder = new InterWaveDecoder(_CbMap);
                    _CrDecoder = new InterWaveDecoder(_CrMap);
                }
            }

            IDataCoder coder = DjvuSettings.Current.CoderFactory.CreateCoder(reader.BaseStream, false);

            for (int flag = 1; flag != 0 && _CSlices < nslices; _CSlices++)
            {
                flag = _YDecoder.CodeSlice(coder);

                if (_CrDecoder != null && _CbDecoder != null && _CrCbDelay <= _CSlices)
                {
                    flag |= _CbDecoder.CodeSlice(coder);
                    flag |= _CrDecoder.CodeSlice(coder);
                }
            }

            _CSerial++;
        }
    }
}
