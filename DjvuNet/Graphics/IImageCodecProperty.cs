using System;

namespace DjvuNet.Graphics
{

    public interface IImageCodecProperty
    {
        String Name { get; set; }

        Type Type { get; }
    }

    public interface IImageCodecProperty<T> : IImageCodecProperty
    {
        T Value { get; set; }

        T MaximumAllowed { get; }

        T MinimumAllowed { get; }

        T DefaultValue { get; }
    }
}
