using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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
            DjvuDocument doc = new DjvuDocument(@"Mcguffey's_Primer.djvu");

            var page = doc.Pages[0];

            page
                .BuildPageImage()
                .Save("TestImage1.png", ImageFormat.Png);

            page.IsInverted = true;

            page
                .BuildPageImage()
                .Save("TestImage2.png", ImageFormat.Png);
        }
    }
}
