using System;
using System.Diagnostics.Tracing;

namespace DjvuNet.Build.Tasks
{
    [EventSource(Name = "DjvuNet.Build.Tasks", Guid = "A158DB34-CC82-416E-BA3A-93F9F172CCF7")]
    public sealed class DjvuNetBuildEventSource : EventSource
    {
        public static readonly DjvuNetBuildEventSource Log = new DjvuNetBuildEventSource();

        public const byte v1 = 1;

        public class Tasks
        {
            public const EventTask BuildMajorVersion = (EventTask)1;
            public const EventTask GetLastCommit = (EventTask)2;
            public const EventTask InstantiateRepository = (EventTask)3;
            public const EventTask IterateCommits = (EventTask)4;
            public const EventTask RetrieveStatus = (EventTask)5;
        }

        // BuildMajorVersion Task Execution
        [Event(1, Task = Tasks.BuildMajorVersion, Opcode = EventOpcode.Start, Level = EventLevel.Informational, Version = v1)]
        public void BuildMajorVersionStart() { if (IsEnabled()) WriteEvent(1); }

        [Event(2, Task = Tasks.BuildMajorVersion, Opcode = EventOpcode.Stop, Level = EventLevel.Informational, Version = v1)]
        public void BuildMajorVersionStop() { if (IsEnabled()) WriteEvent(2); }

        // GetLastCommit Task Execution
        [Event(3, Task = Tasks.GetLastCommit, Opcode = EventOpcode.Start, Level = EventLevel.Informational, Version = v1)]
        public void GetLastCommitStart() { if (IsEnabled()) WriteEvent(3); }

        [Event(4, Task = Tasks.GetLastCommit, Opcode = EventOpcode.Stop, Level = EventLevel.Informational, Version = v1)]
        public void GetLastCommitStop() { if (IsEnabled()) WriteEvent(4); }

        // Repository Instantiation
        [Event(5, Task = Tasks.InstantiateRepository, Opcode = EventOpcode.Start, Level = EventLevel.Verbose, Version = v1)]
        public void InstantiateRepositoryStart() { if (IsEnabled()) WriteEvent(5); }

        [Event(6, Task = Tasks.InstantiateRepository, Opcode = EventOpcode.Stop, Level = EventLevel.Verbose, Version = v1)]
        public void InstantiateRepositoryStop() { if (IsEnabled()) WriteEvent(6); }

        // Commit Iteration
        [Event(7, Task = Tasks.IterateCommits, Opcode = EventOpcode.Start, Level = EventLevel.Verbose, Version = v1)]
        public void IterateCommitsStart() { if (IsEnabled()) WriteEvent(7); }

        [Event(8, Task = Tasks.IterateCommits, Opcode = EventOpcode.Stop, Level = EventLevel.Verbose, Version = v1)]
        public void IterateCommitsStop(int count) { if (IsEnabled()) WriteEvent(8, count); }

        // Retrieve Status
        [Event(9, Task = Tasks.RetrieveStatus, Opcode = EventOpcode.Start, Level = EventLevel.Verbose, Version = v1)]
        public void RetrieveStatusStart() { if (IsEnabled()) WriteEvent(9); }

        [Event(10, Task = Tasks.RetrieveStatus, Opcode = EventOpcode.Stop, Level = EventLevel.Verbose, Version = v1)]
        public void RetrieveStatusStop(bool isDirty) { if (IsEnabled()) WriteEvent(10, isDirty); }
    }
}
