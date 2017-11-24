using System.ComponentModel;

namespace DjvuNet
{
    public interface IDjvuImage
    {
        bool Disposed { get; }

        IDjvuDocument Document { get; }

        System.Drawing.Bitmap Image { get; }

        bool IsInverted { get; set; }

        bool IsPageImageCached { get; set; }

        IDjvuPage Page { get; }

        event PropertyChangedEventHandler PropertyChanged;

        void ClearImage();

        void Preload();

        System.Drawing.Bitmap ResizeImage(int newWidth, int newHeight);
    }
}
