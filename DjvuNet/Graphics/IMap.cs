using System.Collections;
using System.Drawing;

namespace DjvuNet.Graphics
{
    public interface IMap
    {
        sbyte[] Data { get; }

        int Height { get; }

        int Width { get; }

    }
}
