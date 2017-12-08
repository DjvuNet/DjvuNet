namespace DjvuNet.Graphics
{
    public interface IBitmap : IMap2
    {
        int Border { get; set; }

        int BytesPerRow { get; }

        int Grays { get; set; }

        int MinimumBorder { set; }

        Pixel[] Ramp { get; }

        bool Blit(IBitmap bm, int xh, int yh, int subsample);

        Rectangle ComputeBoundingBox();

        IBitmap Duplicate();

        void Fill(short value);

        bool GetBooleanAt(int offset);

        int GetByteAt(int offset);

        IBitmap Init(IBitmap source, int border = 0);

        IBitmap Init(IBitmap source, Rectangle rect, int border);

        IBitmap Init(int height, int width, int border);

        bool InsertMap(IBitmap bit, int dx, int dy, bool doBlit);

        void SetByteAt(int offset, sbyte value);

    }
}
