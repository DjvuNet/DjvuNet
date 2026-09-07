using System;
using System.Linq;
using Xunit;
using DjvuNet.Graphics;
using DjvuNet.Diagnostics.Tests;

namespace DjvuNet.Graphics.Tests
{
    public sealed class GraphicsEventFixture : IDisposable
    {
        public TestEventListener Listener { get; }

        public GraphicsEventFixture()
        {
            Listener = new TestEventListener("DjvuNet.Graphics.GraphicsEventSource");
        }

        public void Dispose() => Listener.Dispose();
    }

    [Collection("ETW")]
    public class GraphicsEventSourceTests : IClassFixture<GraphicsEventFixture>
    {
        private readonly TestEventListener _listener;

        public GraphicsEventSourceTests(GraphicsEventFixture fixture)
        {
            _listener = fixture.Listener;
            _listener.Events.Clear();
        }

        public static TheoryData<string> CallSiteTestData => new TheoryData<string>
        {
            { null },
            { string.Empty },
            { "ValidCallSite" }
        };

        [Theory]
        [MemberData(nameof(CallSiteTestData))]
        public void OnBitmapDataAllocatedTest(string callSite)
        {
            GraphicsEventSource.Log.OnBitmapDataAllocated(10, 20, 300L, 2, false, callSite);
            
            Assert.Single(_listener.Events);
            var ev = _listener.Events.Single();
            
            Assert.Equal(1, ev.EventId);
            Assert.Equal(10, (int)ev.Payload[0]);
            Assert.Equal(20, (int)ev.Payload[1]);
            Assert.Equal(300L, (long)ev.Payload[2]);
            Assert.Equal(2, (int)ev.Payload[3]);
            Assert.Equal(false, (bool)ev.Payload[4]);
            Assert.Equal(callSite ?? string.Empty, (string)ev.Payload[5]);
        }

        public enum GraphicsEventType 
        { 
            RleDataAllocated, 
            RleDataRented, 
            RleDataReturned 
        }

        public static TheoryData<GraphicsEventType, long, string> RleTestData => new TheoryData<GraphicsEventType, long, string>
        {
            { GraphicsEventType.RleDataAllocated, 100L, null },
            { GraphicsEventType.RleDataAllocated, 200L, string.Empty },
            { GraphicsEventType.RleDataAllocated, 300L, "SiteAlloc" },
            { GraphicsEventType.RleDataRented, 100L, null },
            { GraphicsEventType.RleDataRented, 200L, string.Empty },
            { GraphicsEventType.RleDataRented, 300L, "SiteRent" },
            { GraphicsEventType.RleDataReturned, 100L, null },
            { GraphicsEventType.RleDataReturned, 200L, string.Empty },
            { GraphicsEventType.RleDataReturned, 300L, "SiteRet" }
        };

        [Theory]
        [MemberData(nameof(RleTestData))]
        public void RleDataEventsTest(GraphicsEventType eventType, long expectedSize, string expectedSite)
        {
            int expectedId = 0;
            switch (eventType)
            {
                case GraphicsEventType.RleDataAllocated:
                    expectedId = 2;
                    GraphicsEventSource.Log.OnRleDataAllocated(expectedSize, expectedSite);
                    break;
                case GraphicsEventType.RleDataRented:
                    expectedId = 3;
                    GraphicsEventSource.Log.OnRleDataRented(expectedSize, expectedSite);
                    break;
                case GraphicsEventType.RleDataReturned:
                    expectedId = 4;
                    GraphicsEventSource.Log.OnRleDataReturned(expectedSize, expectedSite);
                    break;
            }
            
            Assert.Single(_listener.Events);
            var ev = _listener.Events.Single();
            
            Assert.Equal(expectedId, ev.EventId);
            Assert.Equal(expectedSize, (long)ev.Payload[0]);
            Assert.Equal(expectedSite ?? string.Empty, (string)ev.Payload[1]);
        }
    }
}
