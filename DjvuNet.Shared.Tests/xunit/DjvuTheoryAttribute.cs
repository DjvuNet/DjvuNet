using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace DjvuNet.Tests.Xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("DjvuNet.Tests.Xunit.DjvuTheoryDiscoverer", global::AssemblyData.Name)]
    public class DjvuTheoryAttribute : FactAttribute
    {
    }
}
