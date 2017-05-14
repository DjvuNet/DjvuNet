using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    public interface IInterWaveMap
    {
        InterWaveMap Duplicate();

        int GetBucketCount();

        void Image(int index, sbyte[] img8, int rowsize, int pixsep, bool fast);

        void Image(int subsample, Rectangle rect, int index, sbyte[] img8, int rowsize, int pixsep, bool fast);

    }
}