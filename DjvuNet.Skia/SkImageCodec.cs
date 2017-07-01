using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DjvuNet.Graphics;
using SkiaSharp;

namespace DjvuNet.Skia
{
    public class SkImageCodec<T> where T : IMap2, IImageDecoder<T>, IImageEncoder<T>
    {
        public T Decode(string filePath, ImageCodecSettings settings = null, FileMode fileMode = FileMode.Create)
        {
            throw new NotImplementedException();
        }

        public T Decode(Stream stream, ImageCodecSettings settings = null)
        {
            using (SKData data = SKData.Create(stream))
            {
                SKCodec codec = SKCodec.Create(data);
                var format = codec.EncodedFormat;
                var info = codec.Info;
                SKImageInfo decInfo = new SKImageInfo
                {
                    ColorType = info.ColorType,
                    Height = info.Height,
                    Width = info.Width
                };
                byte[] buffer = new byte[info.Width * info.Height * 4];
                GCHandle hData = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                var result = codec.GetPixels(decInfo, hData.AddrOfPinnedObject());
                if (hData.IsAllocated)
                    hData.Free();

                IPixelMap pixMap = new PixelMap();
                return (T) pixMap;
            }
        }

        public T Decode(IntPtr buffer, ImageCodecSettings settings = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> DecodeAsync(string filePath, ImageCodecSettings settings = null, FileMode fileMode = FileMode.Create, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<T> DecodeAsync(Stream stream, ImageCodecSettings settings = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<T> DecodeAsync(IntPtr buffer, ImageCodecSettings settings = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public void Encode(T image, string filePath, ImageCodecSettings settings = null, FileMode fileMode = FileMode.Create)
        {
            throw new NotImplementedException();
        }

        public void Encode(T image, Stream stream, ImageCodecSettings settings = null)
        {
            throw new NotImplementedException();
        }

        public void Encode(T image, IntPtr buffer, ImageCodecSettings settings = null)
        {
            throw new NotImplementedException();
        }

        public Task EncodeAsync(T image, string filePath, ImageCodecSettings settings = null, FileMode fileMode = FileMode.Create, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task EncodeAsync(T image, Stream stream, ImageCodecSettings settings = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task EncodeAsync(T image, IntPtr buffer, ImageCodecSettings settings = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
