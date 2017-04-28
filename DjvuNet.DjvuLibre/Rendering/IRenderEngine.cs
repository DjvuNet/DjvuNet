using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.DjvuLibre
{
    public interface IRenderEngine
    {
        int X { get; set; }

        int Y { get; set; }

        int Width { get; set; }

        int Height { get; set; }

        int Dpi { get; set; }

        FormatStyle FormatStyle { get; set; }

        RenderMode Mode { get; set; }

        IntPtr Buffer { get; set; }

        void CreateBuffer();

        IntPtr Render();

        void ReleaseBuffer();

    }
}
