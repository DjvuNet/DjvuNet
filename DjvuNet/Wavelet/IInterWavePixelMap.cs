using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    public interface IInterWavePixelMap
    {
        int Height { get; }

        bool ImageData { get; }

        int Width { get; }

        void CloseCodec();

        void Decode(IBinaryReader reader);

        PixelMap GetPixelMap();

        PixelMap GetPixelMap(int subsample, Rectangle rect, PixelMap retval);
    }
}