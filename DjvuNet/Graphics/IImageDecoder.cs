using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DjvuNet.Graphics
{
    public interface IImageDecoder<Timg> where Timg : IMap2
    {
        Timg Decode(string filePath, ImageCodecSettings settings = null, FileMode fileMode = FileMode.Create);

        Timg Decode(Stream stream, ImageCodecSettings settings = null);

        Timg Decode(IntPtr buffer, ImageCodecSettings settings = null);

        Task<Timg> DecodeAsync(string filePath, ImageCodecSettings settings = null, FileMode fileMode = FileMode.Create, CancellationToken cancellationToken = default(CancellationToken));

        Task<Timg> DecodeAsync(Stream stream, ImageCodecSettings settings = null, CancellationToken cancellationToken = default(CancellationToken));

        Task<Timg> DecodeAsync(IntPtr buffer, ImageCodecSettings settings = null, CancellationToken cancellationToken = default(CancellationToken));

    }
}
