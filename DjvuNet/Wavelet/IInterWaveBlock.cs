namespace DjvuNet.Wavelet
{
    public interface IInterWaveBlock
    {
        void ClearBlock(int n);

        InterWaveBlock Duplicate();

        short[] GetBlock(int n);

        short[] GetInitializedBlock(int n);

        short GetValue(int n);

        void SetValue(int n, int val);

        void WriteLiftBlock(short[] coeff, int bmin, int bmax);
    }
}