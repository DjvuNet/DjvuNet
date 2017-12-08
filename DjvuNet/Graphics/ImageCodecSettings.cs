using System;
using System.Collections.Generic;

namespace DjvuNet.Graphics
{
    public abstract class ImageCodecSettings
    {
        public abstract String Codec { get; }

        public abstract String HeaderSignature { get; }

        public abstract IReadOnlyDictionary<String, IImageCodecProperty[]> Properties { get; }

        public abstract IReadOnlyList<String> PropertyNames { get; }

        public abstract IReadOnlyList<Type> PropertyTypes { get; }

        public abstract void SetProperty(String name, params IImageCodecProperty[] parameters);

    }


}
