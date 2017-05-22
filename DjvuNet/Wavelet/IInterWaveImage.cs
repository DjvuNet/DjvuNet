namespace DjvuNet.Wavelet
{
    public interface IInterWaveImage
    {
        int Bytes { get; }

        float DbFrac { get; set; }

        int Height { get; }

        int Serial { get; }

        int Slices { get; }

        int Width { get; }
    }
}