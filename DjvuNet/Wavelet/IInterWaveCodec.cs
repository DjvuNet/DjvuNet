using DjvuNet.Compression;

namespace DjvuNet.Wavelet
{
    public interface IInterWaveCodec
    {
        int CodeSlice(ZPCodec zp);

        void DecodeBuckets(ZPCodec zp, int bit, int band, InterWaveBlock blk, int fbucket, int nbucket);

        InterWaveCodec Init(InterWaveMap map);

        int IsNullSlice(int bit, int band);

        int NextQuant();
    }
}