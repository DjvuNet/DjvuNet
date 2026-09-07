using System;
using System.Linq;
using Xunit;
using DjvuNet.JB2;
using DjvuNet.Diagnostics.Tests;

namespace DjvuNet.JB2.Tests
{
    public sealed class JB2EventFixture : IDisposable
    {
        public TestEventListener Listener { get; }

        public JB2EventFixture()
        {
            Listener = new TestEventListener("DjvuNet.JB2.JB2EventSource");
        }

        public void Dispose()
        {
            Listener.Dispose();
        }
    }

    [Collection("ETW")]
    public class JB2EventSourceTests : IClassFixture<JB2EventFixture>
    {
        private readonly TestEventListener _listener;

        public JB2EventSourceTests(JB2EventFixture fixture)
        {
            _listener = fixture.Listener;
            _listener.Events.Clear();
        }

        // --- Signature A: Start Events (callSite) ---

        public enum JB2StartEventType 
        { 
            DictDecode, 
            ImageDecode, 
            DictEncode, 
            ImageEncode, 
            Render 
        }

        public static TheoryData<JB2StartEventType, string> StartEventTestData => new TheoryData<JB2StartEventType, string>
        {
            { JB2StartEventType.DictDecode, null }, 
            { JB2StartEventType.DictDecode, "Site" },
            { JB2StartEventType.ImageDecode, null }, 
            { JB2StartEventType.ImageDecode, "Site" },
            { JB2StartEventType.DictEncode, null }, 
            { JB2StartEventType.DictEncode, "Site" },
            { JB2StartEventType.ImageEncode, null }, 
            { JB2StartEventType.ImageEncode, "Site" },
            { JB2StartEventType.Render, null }, 
            { JB2StartEventType.Render, "Site" }
        };

        [Theory]
        [MemberData(nameof(StartEventTestData))]
        public void StartEventsTest(JB2StartEventType eventType, string expectedSite)
        {
            int expectedId = 0;
            switch (eventType)
            {
                case JB2StartEventType.DictDecode: 
                    expectedId = 1; 
                    JB2EventSource.Log.OnDictionaryDecodeStart(expectedSite); 
                    break;
                case JB2StartEventType.ImageDecode: 
                    expectedId = 3; 
                    JB2EventSource.Log.OnImageDecodeStart(expectedSite); 
                    break;
                case JB2StartEventType.DictEncode: 
                    expectedId = 5; 
                    JB2EventSource.Log.OnDictionaryEncodeStart(expectedSite); 
                    break;
                case JB2StartEventType.ImageEncode: 
                    expectedId = 7; 
                    JB2EventSource.Log.OnImageEncodeStart(expectedSite); 
                    break;
                case JB2StartEventType.Render: 
                    expectedId = 9; 
                    JB2EventSource.Log.OnJB2ImageRenderStart(expectedSite); 
                    break;
            }
            
            Assert.Single(_listener.Events);
            var ev = _listener.Events.Single();
            Assert.Equal(expectedId, ev.EventId);
            Assert.Equal(expectedSite ?? string.Empty, (string)ev.Payload[0]);
        }

        // --- Signature B: Stop Events (count, callSite) ---

        public enum JB2StopEventType 
        { 
            DictDecode, 
            ImageDecode, 
            DictEncode, 
            ImageEncode 
        }

        public static TheoryData<JB2StopEventType, int, string> StopEventTestData => new TheoryData<JB2StopEventType, int, string>
        {
            { JB2StopEventType.DictDecode, 50, null }, 
            { JB2StopEventType.DictDecode, 50, "Site" },
            { JB2StopEventType.ImageDecode, 150, null }, 
            { JB2StopEventType.ImageDecode, 150, "Site" },
            { JB2StopEventType.DictEncode, 10, null }, 
            { JB2StopEventType.DictEncode, 10, "Site" },
            { JB2StopEventType.ImageEncode, 20, null }, 
            { JB2StopEventType.ImageEncode, 20, "Site" }
        };

        [Theory]
        [MemberData(nameof(StopEventTestData))]
        public void StopEventsTest(JB2StopEventType eventType, int expectedCount, string expectedSite)
        {
            int expectedId = 0;
            switch (eventType)
            {
                case JB2StopEventType.DictDecode: 
                    expectedId = 2; 
                    JB2EventSource.Log.OnDictionaryDecodeStop(expectedCount, expectedSite); 
                    break;
                case JB2StopEventType.ImageDecode: 
                    expectedId = 4; 
                    JB2EventSource.Log.OnImageDecodeStop(expectedCount, expectedSite); 
                    break;
                case JB2StopEventType.DictEncode: 
                    expectedId = 6; 
                    JB2EventSource.Log.OnDictionaryEncodeStop(expectedCount, expectedSite); 
                    break;
                case JB2StopEventType.ImageEncode: 
                    expectedId = 8; 
                    JB2EventSource.Log.OnImageEncodeStop(expectedCount, expectedSite); 
                    break;
            }
            
            Assert.Single(_listener.Events);
            var ev = _listener.Events.Single();
            Assert.Equal(expectedId, ev.EventId);
            Assert.Equal(expectedCount, (int)ev.Payload[0]);
            Assert.Equal(expectedSite ?? string.Empty, (string)ev.Payload[1]);
        }

        // --- Signature C: Render Stop (count, sizeBytes, callSite) ---

        public static TheoryData<int, long, string> RenderStopTestData => new TheoryData<int, long, string>
        {
            { 100, 4096L, null },
            { 100, 4096L, "Site" }
        };

        [Theory]
        [MemberData(nameof(RenderStopTestData))]
        public void OnJB2ImageRenderStopTest(int expectedCount, long expectedSize, string expectedSite)
        {
            JB2EventSource.Log.OnJB2ImageRenderStop(expectedCount, expectedSize, expectedSite);
            
            Assert.Single(_listener.Events);
            var ev = _listener.Events.Single();
            Assert.Equal(10, ev.EventId);
            Assert.Equal(expectedCount, (int)ev.Payload[0]);
            Assert.Equal(expectedSize, (long)ev.Payload[1]);
            Assert.Equal(expectedSite ?? string.Empty, (string)ev.Payload[2]);
        }
    }
}
