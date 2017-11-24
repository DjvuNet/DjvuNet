using System.ComponentModel;
using System.Drawing;

namespace DjvuNet
{
    public interface IDjvuImage
    {
        bool Disposed { get; }

        IDjvuDocument Document { get; }

        Bitmap Image { get; }

        bool IsInverted { get; set; }

        bool IsPageImageCached { get; set; }

        IDjvuPage Page { get; }

        event PropertyChangedEventHandler PropertyChanged;

        void ClearImage();

        void Preload();

        Bitmap ResizeImage(int newWidth, int newHeight);
    }
}
