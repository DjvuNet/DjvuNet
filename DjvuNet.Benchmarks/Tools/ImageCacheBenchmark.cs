using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DjvuNet;
using DjvuNet.Tests;
using DjvuNet.Graphics;
using DjvuNet.Benchmarks.Core;

namespace DjvuNet.Tools.Benchmarks
{
    [Config(typeof(LongRunningConfig))]
    public class ImageCacheBenchmark : IDisposable
    {
        private List<int> _documentIndices;
        private DjvuDocFixture _managedFixture;

        [GlobalSetup]
        public void Setup()
        {
            _documentIndices = [23]; // single document index for benchmarking to avoid very long run
            // Use the full suite of documents (approx 132 documents)
            // foreach (object[] data in Util.AllTestDocumentIndices)
            // {
            //     _documentIndices.Add((int)data[0]);
            // }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Recreate the fixture for each iteration to get a fresh cache
            _managedFixture = new DjvuDocFixture();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            // Dispose the fixture, which clears the ConcurrentDictionary cache
            // and releases the GDI+ memory, mirroring xUnit's Collection Fixture teardown
            _managedFixture?.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _managedFixture?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [Benchmark]
        public int ForceDecodeAllLayers()
        {
            int totalWidth = 0;
            int totalHeight = 0;
            int totalPixelsRead = 0;

            foreach (int index in _documentIndices)
            {
                // This calls the internal constructor with isTesting = true
                // and caches the document in the fixture's ConcurrentDictionary
                var doc = _managedFixture.GetDocument(index);

                // Access a few representative pages to test decoding paths
                var pageIndex = new int[] { 1, 3, 25 };

                foreach (int pageNum in pageIndex)
                {
                    var page = doc.Pages[pageNum];

                    // Accessing the Image property forces BuildImage() to execute.
                    if (page.Image is DjvuImage djvuImage)
                    {
                        var bitmap = djvuImage.Image;

                        if (bitmap != null)
                        {
                            // Image property is lazily loaded and cached, so this entire
                            // code ensures the decoding process is executed and image is available.
                            totalWidth += bitmap.Width;
                            totalHeight += bitmap.Height;

                            // Read the center pixel to guarantee the pixel buffer was actually mapped
                            if (bitmap.Width > 0 && bitmap.Height > 0)
                            {
                                var pixel = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
                                totalPixelsRead += pixel.R;
                            }
                        }
                    }
                }
            }

            // Return the accumulated values so the benchmark runner consumes them,
            // proving to the JIT that the entire calculation branch was necessary.
            return totalPixelsRead + totalWidth + totalHeight;
        }
    }
}
