using System;

namespace DjvuNet.Graphics
{
    public static class ImageConverter
    {
        /// <summary>
        /// General method to load image data in different formats recognized by ImageConverter or it's
        /// plugins into data structures recognized by DjvyNet library.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static IMap2 Read(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            byte[] buffer = reader.ReadBytes(8);
            var header16 = (ushort)(buffer[0] << 8 | buffer[1]);

            switch (header16)
            {
                case 0xffd8:
                    // JPEG header                          -> ff d8 ff
                    if (buffer[2] == 0xff)
                    {
                        return ReadJpeg(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected JPEG file header: ff d8 {buffer[2].ToString("x")}");
                    }

                case 0x8950:
                    // PNG header                           -> 89 50 4e 47 0d 0a 1a 0a
                    if (buffer[2] == 0x4e && buffer[3] == 0x47 &&
                        buffer[4] == 0x0d && buffer[5] == 0x0a && buffer[6] == 0x1a && buffer[7] == 0x0a)
                    {
                        return ReadPng(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected PNG file header.");
                    }

                case 0x4749:
                    // GIF header                           -> 47 49 46 38 ( 37 | 39 )
                    if (buffer[2] == 0x46 && buffer[3] == 0x38 &&
                        (buffer[4] == 0x37 || buffer[4] == 0x39))
                    {
                        return ReadGif(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected GIF file header.");
                    }

                case 0x4949:
                case 0x4d4d:
                    // TIFF header                          -> ( 49 49 2a 00 | 4d 4d 00 2a )
                    if ((buffer[0] == 0x49 && buffer[1] == 0x49 && buffer[2] == 0x2a && buffer[3] == 0x00) ||
                        (buffer[0] == 0x4d && buffer[1] == 0x4d && buffer[2] == 0x00 && buffer[3] == 0x2a))
                    {
                        return ReadTiff(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected TIFF file header.");
                    }

                case 0xff4f:
                    // JPEG 2000 header                     -> ff 4f ff 51 ff 52
                    if (buffer[2] == 0xff && buffer[3] == 0x51 && buffer[4] == 0xff && buffer[5] == 0x52)
                    {
                        return ReadJpeg2k(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected JPEG 2000 file header.");
                    }

                case 0x574d:
                    // JPEG XR header                       -> 57 4d 50 48 4f 54 4f 00
                    if (buffer[2] == 0x50 && buffer[3] == 0x48 &&
                        buffer[4] == 0x4f && buffer[5] == 0x54 && buffer[6] == 0x4f && buffer[7] == 0x00)
                    {
                        return ReadJpegXr(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected JPEG XR file header.");
                    }

                case 0x762f:
                    // OpenEXR header                       -> 76 2f 31 01
                    if (buffer[2] == 0x31 && buffer[3] == 0x01)
                    {
                        return ReadOpenExr(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected OpenEXR file header.");
                    }

                case 0x424d:
                case 0x4241:
                case 0x4349:
                case 0x4350:
                case 0x4943:
                case 0x5054:
                    // BMP  header  see BITMAPFILEHEADER definition in wingdi.h

                    //      BM - Windows 3.1x, <-> NT ...   -> 42 4d
                    //      BA - OS / 2 Bitmap Array        -> 42 41
                    //      CI - OS / 2 Color Icon          -> 43 49
                    //      CP - OS / 2 Color Pointer       -> 43 50
                    //      IC - OS / 2 Icon                -> 49 43
                    //      PT - OS / 2 Pointer             -> 50 54
                    return ReadBmp(reader);

                case 0x5249:
                    // WebP header -> RIFF #### WEBP        -> 52 49 46 46 # # # # 57 45 42 50
                    reader.Position = 0;
                    buffer = reader.ReadBytes(12);
                    if (buffer[2] == 0x46 && buffer[3] == 0x46 &&
                        buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                    {
                        return ReadWebP(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected WebP file header.");
                    }

                case 0x0000:
                    // ICO | CUR header                     -> 00 00 ( 01 | 02 ) 00
                    if ((buffer[2] == 0x01 || buffer[2] == 0x02) && buffer[3] == 0x00)
                    {
                        return ReadIco(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected ICO | CUR file header.");
                    }

                case 0x5031:
                case 0x5032:
                case 0x5033:
                case 0x5034:
                case 0x5035:
                case 0x5036:
                    // PNM headers
                    //      PBM header -> P4                -> 50 34
                    //      PBM Plain header -> P1          -> 50 31

                    //      PGM header -> P5                -> 50 35
                    //      PGM Plain header -> P2          -> 50 32

                    //      PPM Raw header -> P6            -> 50 36
                    //      PPM Plain header -> P3          -> 50 33
                    return ReadPnm(reader);

                case 0x5234:
                    // RLE header                           -> 52 34
                    return ReadRle(reader);

                case 0x4250:
                    // BPG header                           -> 42 50 47 fb
                    if (buffer[2] == 0x47 && buffer[3] == 0xfb)
                    {
                        return ReadBpg(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected BPG file header.");
                    }

                case 0xab4b:
                    // https://github.com/Ericsson/ETCPACK/blob/master/source/etcpack.cxx
                    // PKM header                           -> ab 4b 54 58 20 31 31 bb 0d 0a 1a 0a
                    reader.Position = 0;
                    buffer = reader.ReadBytes(12);
                    if (buffer[2] == 0x54 && buffer[3] == 0x58 &&
                        buffer[4] == 0x20 && buffer[5] == 0x31 && buffer[6] == 0x31 && buffer[7] == 0xbb &&
                        buffer[8] == 0x0d && buffer[9] == 0x0a && buffer[10] == 0x1a && buffer[11] == 0x0a)
                    {
                        return ReadPkm(reader);
                    }
                    else
                    {
                        throw new DjvuFormatException($"Unexpected PKM file header.");
                    }

                default:
                    throw new DjvuFormatException("Unsupported format header: ");
            }

            // WMF Placeable header                 -> 07 cd c6 9a     0x9AC6CDD7
            // EMF _MAC ENHMETA_SIGNATURE           -> 0x464D4520
            // EMF ENHMETA_SIGNATURE                -> 0x20454D46

            // EPS header ->

            // PS header ->

        }

        public static IMap2 ReadJpeg(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadJpeg2k(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadJpegXr(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadOpenExr(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadPng(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadGif(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadTiff(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadBmp(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadWebP(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadIco(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadPnm(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadRle(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadBpg(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }

        public static IMap2 ReadPkm(IDjvuReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Position != 0)
            {
                reader.Position = 0;
            }

            throw new NotImplementedException();
        }
    }
}
