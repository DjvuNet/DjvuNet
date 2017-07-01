using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
