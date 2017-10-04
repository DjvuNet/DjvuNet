using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace DjvuNet.Git.Tasks.Tests.Moqs
{
    public class BuildEngineMoq : IBuildEngine, IBuildEngine2, IBuildEngine3, IBuildEngine4, IBuildEngine5
    {
        public bool ContinueOnError => true;

        public int LineNumberOfTaskNode => -1;

        public int ColumnNumberOfTaskNode => -1;

        public string ProjectFileOfTaskNode => "MoqFile.proj";

        public bool IsRunningMultipleNodes => throw new NotImplementedException();

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            return true;
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs, string toolsVersion)
        {
            return true;
        }

        public bool BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IDictionary[] targetOutputsPerProject, string[] toolsVersion, bool useResultsCache, bool unloadProjectsOnCompletion)
        {
            return true;
        }

        public BuildEngineResult BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IList<string>[] removeGlobalProperties, string[] toolsVersion, bool returnTargetOutputs)
        {
            return default(BuildEngineResult);
        }

        public object GetRegisteredTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            return null;
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
        }

        public void LogTelemetry(string eventName, IDictionary<string, string> properties)
        {
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
        }

        public void Reacquire()
        {
        }

        public void RegisterTaskObject(object key, object obj, RegisteredTaskObjectLifetime lifetime, bool allowEarlyCollection)
        {
        }

        public object UnregisterTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            return null;
        }

        public void Yield()
        {
        }
    }
}
