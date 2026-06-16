using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Build.Framework;

namespace DjvuNet.Build.Tasks
{
    public class DjvuNetBuildTaskFactory : ITaskFactory2
    {
        private IBuildEngine _loggingEngine;
        public string FactoryName => nameof(DjvuNetBuildTaskFactory);
        public Type TaskType { get; private set; }

        public bool Initialize(string taskName, IDictionary<string, string> factoryIdentityParameters, IDictionary<string, TaskPropertyInfo> parameterGroup, string taskBody, IBuildEngine taskFactoryLoggingHost)
        {
            // If factoryIdentityParameters is null, or "Runtime" is missing, runtime string will be null
            string runtime = null;
            if (factoryIdentityParameters != null)
            {
                factoryIdentityParameters.TryGetValue("Runtime", out runtime);
            }

            if (!IsSupportedRuntime(runtime))
            {
                string reportedRuntime = string.IsNullOrEmpty(runtime) ? "Unspecified/Any" : runtime;
                taskFactoryLoggingHost.LogErrorEvent(new BuildErrorEventArgs(
                    "TaskFactory", "DTF03", "", 0, 0, 0, 0, 
                    $"The requested MSBuild runtime '{reportedRuntime}' is not supported. DjvuNet.Build.Tasks utilize APIs which require .NET 5.0+ (Runtime=\"NET\").", 
                    "Help", "DjvuNetTaskFactory"));
                return false;
            }

            return Initialize(taskName, parameterGroup, taskBody, taskFactoryLoggingHost);
        }

        private bool IsSupportedRuntime(string runtime)
        {
#if NETCOREAPP
            if (string.IsNullOrWhiteSpace(runtime)) 
            {
                // If the user didn't specify a runtime, MSBuild defaults to "Any". 
                // We cannot guarantee the host is .NET 5.0+, so we must reject it.
                return false; 
            }

            // Only explicitly modern .NET MSBuild runtimes are safe for our APIs.
            return runtime.Equals("NET", StringComparison.OrdinalIgnoreCase) ||
                   runtime.Equals("Core", StringComparison.OrdinalIgnoreCase);
#else
            // .NET Framework execution implies Visual Studio in-process hosting.
            // We explicitly bypass the modern runtime check because the net472 polyfills are active.
            return true;
#endif
        }

        public bool Initialize(string taskName, IDictionary<string, TaskPropertyInfo> parameterGroup, string taskBody, IBuildEngine taskFactoryLoggingHost)
        {
            _loggingEngine = taskFactoryLoggingHost;

            Assembly currentAsm = Assembly.GetExecutingAssembly();
            TaskType = currentAsm.GetType(taskName);

            if (TaskType == null)
            {
                TaskType = currentAsm.GetType($"DjvuNet.Build.Tasks.{taskName}");
                if (TaskType == null)
                {
                    TaskType = currentAsm.GetType($"MSBuild.Community.Tasks.{taskName}");
                }
            }

            if (TaskType == null)
            {
                _loggingEngine.LogErrorEvent(new BuildErrorEventArgs("TaskFactory", "DTF01", "", 0, 0, 0, 0, $"Could not resolve task type for {taskName}", "Help", "DjvuNetTaskFactory"));
                return false;
            }

            Init.Initialize(_loggingEngine);
            return true;
        }

        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost, IDictionary<string, string> taskIdentityParameters)
        {
            return CreateTask(taskFactoryLoggingHost);
        }

        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost)
        {
            return (ITask)Activator.CreateInstance(TaskType);
        }

        public TaskPropertyInfo[] GetTaskParameters()
        {
            var props = new List<TaskPropertyInfo>();
            foreach (var p in TaskType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                bool isOutput = p.GetCustomAttribute<OutputAttribute>() != null;
                bool isRequired = p.GetCustomAttribute<RequiredAttribute>() != null;
                props.Add(new TaskPropertyInfo(p.Name, p.PropertyType, isOutput, isRequired));
            }
            return props.ToArray();
        }

        public void CleanupTask(ITask task)
        {
            if (task is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}