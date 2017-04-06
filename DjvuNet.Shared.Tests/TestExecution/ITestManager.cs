using System.Collections.Generic;

namespace DjvuNet.Tests
{
    public interface ITestManager
    {
        bool IsInitialized { get; }

        TestFramework Framework { get; set; }

        IReadOnlyList<string> TargetNames { get; }

        int AfterBuildTests();

        int AfterRunTests();

        int BeforeBuildTests();

        int BeforeRunTests();

        int BuildTests();

        int RunTestFuzzer();

        int RunTests(string target);
    }

    public enum TestFramework
    {
        Unknown = 0,
        MSTest = 1,
        xUnit = 2,
        NUnit = 3,
        MSpec = 4,
    }
}