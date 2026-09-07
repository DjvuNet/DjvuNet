using System;
using System.Diagnostics.Tracing;

namespace DjvuNet.Graphics
{
    [EventSource(Name = _Name, Guid = _Guid)]
    public sealed class GraphicsEventSource : EventSource
    {
        const string _Name = "DjvuNet.Graphics.GraphicsEventSource";
        const String _Guid = "{B3A13476-88C1-4D67-A968-38EE02737E92}";
        const byte _Version = 0x01;

        public static readonly GraphicsEventSource Log = new GraphicsEventSource();

        private GraphicsEventSource() : base(_Name) { }

        [Event(1, Level = EventLevel.Verbose, Opcode = EventOpcode.Info, Version = _Version)]
        public unsafe void OnBitmapDataAllocated(int width, int height, long sizeBytes, int border, bool uninitialized, string callSite)
        {
            if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
            {
                callSite = callSite ?? string.Empty;
                fixed (char* callerPtr = callSite)
                {
                    EventData* descrs = stackalloc EventData[6];
                    descrs[0].DataPointer = (IntPtr)(&width); descrs[0].Size = 4;
                    descrs[1].DataPointer = (IntPtr)(&height); descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)(&sizeBytes); descrs[2].Size = 8;
                    descrs[3].DataPointer = (IntPtr)(&border); descrs[3].Size = 4;
                    int uninit = uninitialized ? 1 : 0;
                    descrs[4].DataPointer = (IntPtr)(&uninit); descrs[4].Size = 4;
                    descrs[5].DataPointer = (IntPtr)callerPtr; descrs[5].Size = (callSite.Length + 1) * 2;
                    WriteEventCore(1, 6, descrs);
                }
            }
        }

        [Event(2, Level = EventLevel.Verbose, Opcode = EventOpcode.Info, Version = _Version)]
        public unsafe void OnRleDataAllocated(long sizeBytes, string callSite)
        {
            if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
            {
                callSite = callSite ?? string.Empty;
                fixed (char* callerPtr = callSite)
                {
                    EventData* descrs = stackalloc EventData[2];
                    descrs[0].DataPointer = (IntPtr)(&sizeBytes); descrs[0].Size = 8;
                    descrs[1].DataPointer = (IntPtr)callerPtr; descrs[1].Size = (callSite.Length + 1) * 2;
                    WriteEventCore(2, 2, descrs);
                }
            }
        }

        [Event(3, Level = EventLevel.Verbose, Opcode = EventOpcode.Info, Version = _Version)]
        public unsafe void OnRleDataRented(long sizeBytes, string callSite = "")
        {
            if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
            {
                callSite = callSite ?? string.Empty;
                fixed (char* callerPtr = callSite)
                {
                    EventData* descrs = stackalloc EventData[2];
                    descrs[0].DataPointer = (IntPtr)(&sizeBytes); descrs[0].Size = 8;
                    descrs[1].DataPointer = (IntPtr)callerPtr; descrs[1].Size = (callSite.Length + 1) * 2;
                    WriteEventCore(3, 2, descrs);
                }
            }
        }

        [Event(4, Level = EventLevel.Verbose, Opcode = EventOpcode.Info, Version = _Version)]
        public unsafe void OnRleDataReturned(long sizeBytes, string callSite = "")
        {
            if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
            {
                callSite = callSite ?? string.Empty;
                fixed (char* callerPtr = callSite)
                {
                    EventData* descrs = stackalloc EventData[2];
                    descrs[0].DataPointer = (IntPtr)(&sizeBytes); descrs[0].Size = 8;
                    descrs[1].DataPointer = (IntPtr)callerPtr; descrs[1].Size = (callSite.Length + 1) * 2;
                    WriteEventCore(4, 2, descrs);
                }
            }
        }
    }
}
