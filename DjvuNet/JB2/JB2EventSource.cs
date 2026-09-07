using System;
using System.Diagnostics.Tracing;

namespace DjvuNet.JB2
{
    [EventSource(Name = _Name, Guid = _Guid)]
    public sealed class JB2EventSource : EventSource
    {
        const string _Name = "DjvuNet.JB2.JB2EventSource";
        const String _Guid = "{C4B24587-99D2-5E78-B079-49FF03848F03}";
        const byte _Version = 0x01;

        public static readonly JB2EventSource Log = new JB2EventSource();

        private JB2EventSource() : base(_Name) { }

        // --- Dictionary Decoding ---

        [Event(1, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Version = _Version)]
        public void OnDictionaryDecodeStart(string callSite)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                WriteEvent(1, callSite ?? string.Empty);
        }

        [Event(2, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Version = _Version)]
        public void OnDictionaryDecodeStop(int shapeCount, string callSite)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                WriteEvent(2, shapeCount, callSite ?? string.Empty);
        }

        // --- Image Decoding ---

        [Event(3, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Version = _Version)]
        public void OnImageDecodeStart(string callSite)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                WriteEvent(3, callSite ?? string.Empty);
        }

        [Event(4, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Version = _Version)]
        public void OnImageDecodeStop(int blitCount, string callSite)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                WriteEvent(4, blitCount, callSite ?? string.Empty);
        }

        // --- Dictionary Encoding ---

        [Event(5, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Version = _Version)]
        public void OnDictionaryEncodeStart(string callSite)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                WriteEvent(5, callSite ?? string.Empty);
        }

        [Event(6, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Version = _Version)]
        public void OnDictionaryEncodeStop(int shapeCount, string callSite)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                WriteEvent(6, shapeCount, callSite ?? string.Empty);
        }

        // --- Image Encoding ---

        [Event(7, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Version = _Version)]
        public void OnImageEncodeStart(string callSite)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                WriteEvent(7, callSite ?? string.Empty);
        }

        [Event(8, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Version = _Version)]
        public void OnImageEncodeStop(int blitCount, string callSite)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                WriteEvent(8, blitCount, callSite ?? string.Empty);
        }

        // --- JB2 Image Rendering (JB2 to Bitmap) ---

        [Event(9, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Version = _Version)]
        public void OnJB2ImageRenderStart(string callSite)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                WriteEvent(9, callSite ?? string.Empty);
        }

        [Event(10, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Version = _Version)]
        public void OnJB2ImageRenderStop(int shapeCount, long sizeBytes, string callSite)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                WriteEvent(10, shapeCount, sizeBytes, callSite ?? string.Empty);
        }
    }
}
