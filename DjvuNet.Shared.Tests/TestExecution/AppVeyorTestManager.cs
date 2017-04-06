using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DjvuNet.Tests
{
    public class AppVeyorTestManager : TestManager
    {
        public const string BuildFolderEnv = "APPVEYOR_BUILD_FOLDER";
        public const string BuildProviderEnv = "APPVEYOR";
        public const string CiBuild = "CI";
        public const string WorkerImage = "APPVEYOR_BUILD_WORKER_IMAGE";

        protected string _WorkerImage;

        protected AppVeyorTestManager()
        {
            Initialize();
        }

        protected override void Initialize()
        {
            _BuildFolder = Environment.GetEnvironmentVariable(BuildFolderEnv);
            _BuildProvider = bool.Parse(Environment.GetEnvironmentVariable(BuildFolderEnv)) ? "AppVeyor" : "Unknown";
            _CiBuild = bool.Parse(Environment.GetEnvironmentVariable(CiBuild));
            _WorkerImage = Environment.GetEnvironmentVariable(WorkerImage);
            Framework = TestFramework.xUnit;
            _TargetNames.AddRange( new string[] { "DjvuNet.Tests", "DjvuNet.DjvuLibre.Tests" });
        }

        internal static AppVeyorTestManager CreatInstance()
        {
            return new AppVeyorTestManager();
        }

        public override int AfterBuildTests()
        {
            throw new NotImplementedException();
        }

        public override int AfterRunTests()
        {
            throw new NotImplementedException();
        }

        public override int BeforeBuildTests()
        {
            throw new NotImplementedException();
        }

        public override int BeforeRunTests()
        {
            throw new NotImplementedException();
        }

        public override int BuildTests()
        {
            throw new NotImplementedException();
        }

        public override int RunTestFuzzer()
        {
            throw new NotImplementedException();
        }

        public override int RunTests(string target)
        {
            int result = 0;
            string dirSep = Path.DirectorySeparatorChar.ToString();


            string envName = GetTargetEnvName(target);
            string targetPath = $"{_BuildFolder}{dirSep}{target}{dirSep}bin{dirSep}{_Platform}{dirSep}{_Configuration}{dirSep}{target}.dll";
            if (File.Exists(target) && Environment.GetEnvironmentVariable(envName) != null)
            {
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = _BuildFolder + @"\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe",
                    Arguments = targetPath + " -appveyor -notrait \"Category=Skip\"",
                };
                    
                Process testProc = null;
                try
                {
                    testProc = Process.Start(info);
                    testProc.WaitForExit(10 * 60 * 1000);
                    result = testProc.ExitCode;
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    result--;
                }
                finally
                {
                    testProc?.Close();
                }
            }
            

            return result;
        }

        protected override string GetTargetEnvName(string targetName)
        {
            return targetName.ToUpper().Replace('.', '_');
        }
    }
}
