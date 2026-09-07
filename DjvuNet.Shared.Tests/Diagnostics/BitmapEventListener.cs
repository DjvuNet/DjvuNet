using System.Diagnostics.Tracing;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Text.Json;

namespace DjvuNet.Diagnostics.Tests
{
    public record struct BitmapEventRecord
    {
        public string EventName { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public long SizeBytes { get; init; }
        public int Border { get; init; }
        public bool Uninitialized { get; init; }
        public string CallSite { get; init; }
        public string DjbzFile { get; init; }
        public string SjbzFile { get; init; }
        public string Phase { get; init; }
    }

    public class BitmapEventListener : EventListener
    {
        public static ConcurrentBag<BitmapEventRecord> Records = new ConcurrentBag<BitmapEventRecord>();
        
        public static AsyncLocal<string> CurrentDjbz = new AsyncLocal<string>();
        public static AsyncLocal<string> CurrentSjbz = new AsyncLocal<string>();
        public static AsyncLocal<string> CurrentPhase = new AsyncLocal<string>();
        
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "DjvuNet.Graphics.GraphicsEventSource")
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }
        
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventId == 1)
            {
                var p = eventData.Payload;
                Records.Add(new BitmapEventRecord {
                    EventName = eventData.EventName,
                    Width = (int)p[0], Height = (int)p[1], SizeBytes = (long)p[2], 
                    Border = (int)p[3], Uninitialized = (bool)p[4], CallSite = (string)p[5], 
                    DjbzFile = CurrentDjbz.Value, SjbzFile = CurrentSjbz.Value, Phase = CurrentPhase.Value
                });
            }
            else if (eventData.EventId == 2)
            {
                var p = eventData.Payload;
                Records.Add(new BitmapEventRecord {
                    EventName = eventData.EventName,
                    Width = 0, Height = 0, SizeBytes = (long)p[0], 
                    Border = 0, Uninitialized = false, CallSite = (string)p[1], 
                    DjbzFile = CurrentDjbz.Value, SjbzFile = CurrentSjbz.Value, Phase = CurrentPhase.Value
                });
            }
        }
        
        public static void ExportToJsonl(string filePath)
        {
            using var writer = new StreamWriter(filePath, false);
            foreach (var r in Records)
            {
                writer.WriteLine(JsonSerializer.Serialize(r));
            }
        }
    }
}
