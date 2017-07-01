using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Graphics
{
    public class ImageConverter
    {
        /// <summary>
        /// General method to load image data in different formats recognized by ImageConverter or it's
        /// plugins into data structures recognized by DjvyNet library.  
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="fileNameExtension"></param>
        /// <returns></returns>
        public static IMap2 Deserialize(IDjvuReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Encoding enc = new ASCIIEncoding();
            byte[] buffer = reader.ReadBytes(8);
            ushort header16 = (ushort)(buffer[0] << 8 | buffer[1]);

            // JPEG header -> ff d8 ff
            if (buffer[0] == 0xff && buffer[1] == 0xd8 && buffer[2] == 0xff)
                return DeserializeJpeg(reader);

            // PNG header -> 89 50 4e 47 0d 0a 1a 0a
            if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4e && buffer[3] == 0x47 && 
                buffer[4] == 0x0d && buffer[5] == 0x0a && buffer[6] == 0x1a && buffer[7] == 0x0a)
                return DeserializePng(reader);

            // GIF header -> 47 49 46 38 (37 | 39)
            if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38 &&
                (buffer[4] == 0x37 || buffer[5] == 0x39))
                return DeserializeGif(reader);

            // TIFF header -> ( 49 49 2a 00 | 4d 4d 00 2a)
            if ((buffer[0] == 0x49 && buffer[1] == 0x49 && buffer[2] == 0x2a && buffer[3] == 0x00) ||
                (buffer[4] == 0x4d && buffer[5] == 0x4d && buffer[6] == 0x00 && buffer[7] == 0x2a))
                return DeserializeTiff(reader);

            // JPEG 2000 header ->  ff 4f ff 51 ff 52
            if (buffer[0] == 0xff && buffer[1] == 0x4f && buffer[2] == 0xff && buffer[3] == 0x51 && 
                buffer[4] == 0xff && buffer[5] == 0x52)
                return DeserializeJpeg2k(reader);

            // JPEG XR header -> 57 4d 50 48 4f 54 4f 00
            if (buffer[0] == 0x57 && buffer[1] == 0x4d && buffer[2] == 0x50 && buffer[3] == 0x48 &&
                buffer[4] == 0x4f && buffer[5] == 0x54 && buffer[6] == 0x4f && buffer[7] == 0x00)
                return DeserializeJpegXr(reader);

            // OpenEXR header -> 76 2f 31 01
            if (buffer[0] == 0x76 && buffer[1] == 0x2f && buffer[2] == 0x31 && buffer[3] == 0x01)
                return DeserializeExr(reader);


            // JPEG header                          -> ff d8 ff

            // PNG header                           -> 89 50 4e 47 0d 0a 1a 0a

            // GIF header                           -> 47 49 46 38 ( 37 | 39 )

            // TIFF header                          -> ( 49 49 2a 00 | 4d 4d 00 2a )

            // JPEG 2000 header                     -> ff 4f ff 51 ff 52

            // JPEG XR header                       -> 57 4d 50 48 4f 54 4f 00

            // OpenEXR header                       -> 76 2f 31 01

            // WMF Placeable header                 -> 07 cd c6 9a     0x9AC6CDD7
            // EMF _MAC ENHMETA_SIGNATURE           -> 0x464D4520
            // EMF ENHMETA_SIGNATURE                -> 0x20454D46

            // BMP  header  see BITMAPFILEHEADER definition in wingdi.h

            //      BM - Windows 3.1x, <-> NT ...   -> 42 4d
            //      BA - OS / 2 Bitmap Array        -> 42 41
            //      CI - OS / 2 Color Icon          -> 43 49
            //      CP - OS / 2 Color Pointer       -> 43 50
            //      IC - OS / 2 Icon                -> 49 43
            //      PT - OS / 2 Pointer             -> 50 54

            // WebP header -> RIFF #### WEBP        -> 52 49 46 46 # # # # 57 45 42 50

            // ICO | CUR header                     -> 00 00 ( 01 | 02 ) 00

            // EPS header -> 

            // PS header ->

            // PNM headers
            //      PBM header -> P4                -> 50 34 
            //      PBM Plain header -> P1          -> 50 31

            //      PGM header -> P5                -> 50 35
            //      PGM Plain header -> P2          -> 50 32

            //      PPM Raw header -> P6            -> 50 36
            //      PPM Plain header -> P3          -> 50 33

            // RLE header                           -> 52 34

            // BPG header                           -> 42 50 47 fb

            // https://github.com/Ericsson/ETCPACK/blob/master/source/etcpack.cxx
            // PKM header                           -> ab 4b 54 58 20 31 31 bb 0d 0a 1a 0a

            throw new NotImplementedException("Code reached part of function which is still not implemented.");
        }

        public static IMap2 DeserializeJpeg(IDjvuReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (reader.Position != 0)
                reader.Position = 0;

            throw new NotImplementedException();
        }

        public static IMap2 DeserializeJpeg2k(IDjvuReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (reader.Position != 0)
                reader.Position = 0;

            throw new NotImplementedException();
        }

        public static IMap2 DeserializeJpegXr(IDjvuReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (reader.Position != 0)
                reader.Position = 0;

            throw new NotImplementedException();
        }

        public static IMap2 DeserializeExr(IDjvuReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (reader.Position != 0)
                reader.Position = 0;

            throw new NotImplementedException();
        }

        public static IMap2 DeserializePng(IDjvuReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (reader.Position != 0)
                reader.Position = 0;

            throw new NotImplementedException();
        }

        public static IMap2 DeserializeGif(IDjvuReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (reader.Position != 0)
                reader.Position = 0;

            throw new NotImplementedException();
        }

        public static IMap2 DeserializeTiff(IDjvuReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (reader.Position != 0)
                reader.Position = 0;

            throw new NotImplementedException();
        }
    }
}
