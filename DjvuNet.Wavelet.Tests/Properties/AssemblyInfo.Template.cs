using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit.Sdk;
using DjvuNet.Graphics;
using DjvuNet.JB2;
using DjvuNet.Tests;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("DjvuNet.Wavelet.Tests")]
[assembly: AssemblyDescription("DjvuNet.Wavelet test harness build into DjvuNet.Wavelet.Tests assembly")]
[assembly: AssemblyConfiguration("__LIBRARY_CONFIGURATION__")]
[assembly: AssemblyPlatform("__LIBRARY_PLATFORM__")]
[assembly: AssemblyCulture("")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("DD662464-55FF-40D0-A7DD-71ACBE150102")]

[assembly: RegisterXunitSerializer(typeof(JsonXunitSerializer), typeof(JB2Shape))]
[assembly: RegisterXunitSerializer(typeof(JsonXunitSerializer), typeof(Bitmap))]

internal class AssemblyData
{
    public const string Name = "DjvuNet.Wavelet.Tests";
}
