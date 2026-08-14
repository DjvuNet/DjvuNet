using System.Collections;
using System.Drawing;

namespace DjvuNet.Graphics
{
    public interface IMap
    {
        int BytesPerPixel { get; }

        sbyte[] Data { get; }

        int Height { get; }

        int Width { get; }

    }
}
