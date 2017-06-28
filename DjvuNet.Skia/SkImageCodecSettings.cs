using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Graphics;

namespace DjvuNet.Skia
{
    public abstract class SkImageCodecSettings : ImageCodecSettings
    {
        public abstract override string Codec { get; }

        public abstract override string HeaderSignature { get; }

        public abstract override IReadOnlyDictionary<string, IImageCodecProperty[]> Properties { get; }

        public abstract override IReadOnlyList<string> PropertyNames { get; }

        public abstract override IReadOnlyList<Type> PropertyTypes { get; }

        public abstract override void SetProperty(string name, params IImageCodecProperty[] parameters);
    }
}
