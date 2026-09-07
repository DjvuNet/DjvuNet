using System;
using System.Diagnostics.Tracing;
using System.IO;

using System.Threading;

namespace DjvuNet.Diagnostics.Tests
{
    public class MemoryEventListener : EventListener
    {
        private readonly TextWriter _csvWriter;
        private readonly Lock _lock = new Lock();
        private bool _isDisposed;

        [ThreadStatic]
        private static bool _isProcessingEvent;

        public MemoryEventListener(string path) : this(new StreamWriter(path, false))
        {
        }

        public MemoryEventListener(TextWriter writer)
        {
            _csvWriter = writer;
            _csvWriter.WriteLine("Type,Width,Height,SizeBytes,Border,Uninitialized,CallSite");
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "DjvuNet.Graphics.GraphicsEventSource")
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            else if (eventSource.Name == "System.Buffers.ArrayPoolEventSource")
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }

        private enum BufferAllocatedReason
        {
            Pooled = 0,
            OverMaximumSize = 1,
            PoolExhausted = 2
        }

        private enum BufferDroppedReason
        {
            Full = 0,
            OverMaximumSize = 1
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (_isProcessingEvent) return;
            _isProcessingEvent = true;

            try
            {
                lock (_lock)
                {
                    if (_isDisposed) return;

                    if (eventData.EventSource.Name == "System.Buffers.ArrayPoolEventSource")
                    {
                        if (eventData.EventId == 2) // BufferAllocated
                        {
                            // Payload: bufferId, bufferSize, poolId, bucketId, reason
                            _csvWriter.Write("NativePoolAllocated,0,0,");
                            _csvWriter.Write(eventData.Payload[1]);
                            _csvWriter.Write(",0,0,Reason:");
                            _csvWriter.WriteLine((BufferAllocatedReason)eventData.Payload[4]);
                        }
                        else if (eventData.EventId == 4) // BufferDropped
                        {
                            // Payload: bufferId, bufferSize, poolId, bucketId, reason
                            _csvWriter.Write("NativePoolDropped,0,0,");
                            _csvWriter.Write(eventData.Payload[1]);
                            _csvWriter.Write(",0,0,Reason:");
                            _csvWriter.WriteLine((BufferDroppedReason)eventData.Payload[4]);
                        }
                        return;
                    }

                    if (eventData.EventId == 1) // BitmapDataAllocated
                    {
                        _csvWriter.WriteLine($"Bitmap,{eventData.Payload[0]},{eventData.Payload[1]},{eventData.Payload[2]},{eventData.Payload[3]},{eventData.Payload[4]},{eventData.Payload[5]}");
                    }
                    else if (eventData.EventId == 2) // RleDataAllocated
                    {
                        _csvWriter.WriteLine($"RleData,0,0,{eventData.Payload[0]},0,0,{eventData.Payload[1]}");
                    }
                    else if (eventData.EventId == 3) // RleDataRented
                    {
                        _csvWriter.WriteLine($"RleDataRented,0,0,{eventData.Payload[0]},0,0,{eventData.Payload[1]}");
                    }
                    else if (eventData.EventId == 4) // RleDataReturned
                    {
                        _csvWriter.WriteLine($"RleDataReturned,0,0,{eventData.Payload[0]},0,0,{eventData.Payload[1]}");
                    }
                }
            }
            finally
            {
                _isProcessingEvent = false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            lock (_lock)
            {
                _isDisposed = true;
                _csvWriter?.Flush();
                _csvWriter?.Dispose();
            }
        }
    }
}
