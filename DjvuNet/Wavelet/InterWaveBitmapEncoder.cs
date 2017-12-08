using System;
using System.IO;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    public class InterWaveBitmapEncoder : InterWaveBitmap
    {

        public void Initialize(IBitmap bitmap, IBitmap mask)
        {
            throw new NotImplementedException();
        }

        public int EncodeChunk(Stream stream, InterWaveEncoderSettings settings)
        {
            throw new NotImplementedException();
        }

        public IDjvuElement EncodeImage(IDjvuWriter writer, int nchunks, InterWaveEncoderSettings[] settings)
        {
            throw new NotImplementedException();
        }

        public void CloseCodec()
        {
            throw new NotImplementedException();
        }
    }
}
