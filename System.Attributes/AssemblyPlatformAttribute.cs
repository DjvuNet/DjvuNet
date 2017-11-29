using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public class AssemblyPlatformAttribute : System.Attribute
    {
        public string Platform { get; private set; }

        public AssemblyPlatformAttribute() : base()
        {
        }

        public AssemblyPlatformAttribute(String value)
        {
            Platform = value;
        }
    }
}

