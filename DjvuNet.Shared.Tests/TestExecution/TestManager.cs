using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DjvuNet.Tests
{
    public abstract class TestManager : ITestManager
    {
        protected static ITestManager _Instance;
        protected String _BuildFolder;
        protected String _BuildProvider;
        protected bool _CiBuild;
        protected List<String> _TargetNames;
        protected string _Configuration;
        protected string _Platform;

        public bool IsInitialized { get; protected set; }

        public TestFramework Framework { get; set; }

        public static ITestManager Instance
        {
            get
            {
                if (_Instance != null)
                    return _Instance;
                else
                {
                    _Instance = AppVeyorTestManager.CreatInstance();
                    return _Instance;
                }
            }
            protected set
            {
                if (_Instance != null)
                    return;
                else
                    _Instance = value;
            }
        }

        protected TestManager()
        {
            _TargetNames = new List<string>();
        }

        public static ITestManager CreateTestManager<T>(bool force = true) where T : ITestManager
        {
            if (_Instance == null)
                SetInstance<T>();
            else if (typeof(T) != _Instance.GetType())
            {
                if (!force)
                    throw new InvalidOperationException(
                        $"Other Type already used to create ITestManager: {_Instance.GetType().FullName}");
                else
                    SetInstance<T>();
            }

            return _Instance;
        }

        private static void SetInstance<T>() where T : ITestManager
        {
            switch (typeof(T).Name)
            {
                case nameof(AppVeyorTestManager):
                    _Instance = AppVeyorTestManager.CreatInstance();
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported Type: {nameof(T)}");
            };
        }

        protected abstract void Initialize();

        protected abstract string GetTargetEnvName(string targetName);

        public IReadOnlyList<string> TargetNames { get { return _TargetNames; } }

        public abstract int BeforeBuildTests();

        public abstract int BuildTests();

        public abstract int AfterBuildTests();

        public abstract int BeforeRunTests();

        public abstract int RunTests(string target);

        public abstract int AfterRunTests();

        public abstract int RunTestFuzzer();

    }
}
