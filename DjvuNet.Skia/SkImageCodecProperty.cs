using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Graphics;

namespace DjvuNet.Skia
{
    public abstract class SkImageCodecProperty<T> : IImageCodecProperty<T>
    {
        public T Value { get; set; }

        public abstract T MaximumAllowed { get; }

        public abstract T MinimumAllowed { get; }

        public abstract T DefaultValue { get; }

        public string Name { get; set; }

        public Type Type { get { return (typeof(T)); } }
    }
}
