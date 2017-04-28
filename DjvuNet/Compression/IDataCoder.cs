using System;
using System.IO;

namespace DjvuNet.Compression
{
    public interface IDataCoder : IDisposable
    {
        ZPTable[] DefaultTable { get; }

        bool DjvuCompat { get; }

        Stream DataStream { get; }

        bool Encoding { get; }

        int Decoder();

        int Decoder(ref byte ctx);

        int DecoderNoLearn(ref byte ctx);

        void Encoder(int bit);

        void Encoder(int bit, ref byte ctx);

        void EncoderNoLearn(int bit, ref byte ctx);

        void Flush();

        int IWDecoder();

        void IWEncoder(bool bit);

        void NewTable(ZPTable[] table);

        byte State(float prob1);
    }
}