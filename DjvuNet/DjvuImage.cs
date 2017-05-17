using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Errors;

namespace DjvuNet
{
    public class DjvuImage
    {
        private IDjvuPage _Page;
        private IDjvuDocument _Document;

        public IDjvuPage Page { get { return _Page; } }

        public IDjvuDocument Document { get { return _Document; } }

        public DjvuImage() { }

        public DjvuImage(IDjvuPage page)
        {
            if (page == null)
                throw new DjvuArgumentNullException(nameof(page));

            _Page = page;
            _Document = page.Document;
        }

        public System.Drawing.Bitmap CreateBlankImage(Brush imageColor)
        {
            return CreateBlankImage(imageColor, _Page.Width, _Page.Height);
        }

        /// <summary>
        /// Creates a blank image with the given color
        /// </summary>
        /// <param name="imageColor"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap CreateBlankImage(Brush imageColor, int width, int height)
        {
            System.Drawing.Bitmap newBackground = new System.Drawing.Bitmap(width, height);

            // Fill the whole image with white
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newBackground))
                g.FillRegion(imageColor, new Region(new System.Drawing.Rectangle(0, 0, width, height)));

            return newBackground;
        }
    }
}
