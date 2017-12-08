using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DjvuNet.Graphics
{
    public interface IImageEncoder<Timg> where Timg : IMap2
    {
        void Encode(Timg image, string filePath, ImageCodecSettings settings = null, FileMode fileMode = FileMode.Create);

        void Encode(Timg image, Stream stream, ImageCodecSettings settings = null);

        void Encode(Timg image, IntPtr buffer, ImageCodecSettings settings = null);

        Task EncodeAsync(Timg image, string filePath, ImageCodecSettings settings = null, FileMode fileMode = FileMode.Create, CancellationToken cancellationToken = default(CancellationToken));

        Task EncodeAsync(Timg image, Stream stream, ImageCodecSettings settings = null, CancellationToken cancellationToken = default(CancellationToken));

        Task EncodeAsync(Timg image, IntPtr buffer, ImageCodecSettings settings = null, CancellationToken cancellationToken = default(CancellationToken));

    }
}
