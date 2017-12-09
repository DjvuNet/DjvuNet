namespace DjvuNet.Wavelet
{
    public interface IInterWaveCodec
    {
        InterWaveCodec Init(InterWaveMap map);

        bool IsNullSlice(int bit, int band);

        int NextQuant();
    }
}
