using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace DjvuNet.Diagnostics.Tests
{
    public class TestEventListener : EventListener
    {
        public string TargetSourceName { get; }
        public ConcurrentQueue<EventWrittenEventArgs> Events { get; } = new ConcurrentQueue<EventWrittenEventArgs>();

        public TestEventListener(string sourceName)
        {
            TargetSourceName = sourceName;
            
            foreach (var source in EventSource.GetSources())
            {
                if (source.Name == TargetSourceName)
                {
                    EnableEvents(source, EventLevel.Verbose, EventKeywords.All);
                }
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (TargetSourceName != null && eventSource.Name == TargetSourceName)
            {
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Events.Enqueue(eventData);
        }
    }
}
