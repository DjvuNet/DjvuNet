using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet;
using DjvuNet.Graphics;

namespace DjvuNetTest
{
    class Program
    {
        public static void Main(string[] args)
        {
            using (DjvuDocument doc = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                string fileName = Path.GetFileNameWithoutExtension(doc.Location);
                var page = doc.Pages[0];

                page.BuildPageImage()
                    .Save(fileName + "_1.png", ImageFormat.Png);

                page.IsInverted = true;

                page.BuildPageImage()
                    .Save(fileName + "_2.png", ImageFormat.Png);
            }

            using (DjvuDocument doc = new DjvuDocument("..\\..\\..\\artifacts\\test003.djvu"))
            {
                string fileName = Path.GetFileNameWithoutExtension(doc.Location);
                var page = doc.Pages[0];

                page.BuildPageImage()
                    .Save(fileName + "_1.png", ImageFormat.Png);

                page.IsInverted = true;

                page.BuildPageImage()
                    .Save(fileName + "_2.png", ImageFormat.Png);
            }
        }
    }
}
