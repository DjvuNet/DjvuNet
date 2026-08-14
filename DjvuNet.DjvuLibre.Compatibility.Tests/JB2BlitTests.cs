using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DjvuNet;
using DjvuNet.JB2;
using DjvuNet.DjvuLibre;
using Xunit;
using DjvuNet.Tests;

namespace DjvuNet.DjvuLibre.Compatibility.Tests
{
#if DJVUNET_ALL_TESTS
    [Collection("NativeAndManagedDocCollection")]
#endif
    public class JB2BlitTests
    {
#if DJVUNET_ALL_TESTS
        private readonly DjvuDocFixture _managedFixture;
        private readonly NativeDjvuDocFixture _nativeFixture;

        public JB2CompatibilityTests(DjvuDocFixture managedFixture, NativeDjvuDocFixture nativeFixture)
        {
            _managedFixture = managedFixture;
            _nativeFixture = nativeFixture;
        }
#endif

        [Theory]
        [MemberData(nameof(Util.ForegroundImageSourceDocs), MemberType = typeof(Util))]
        public void JB2BlitTheory(int index)
        {
            string filePath = Util.GetTestFilePath(index);
            Assert.True(File.Exists(filePath), $"Test file not found: {filePath}");

#if DJVUNET_ALL_TESTS
            DjvuDocument doc = _managedFixture.GetDocument(index);
            DjvuDocumentInfo nativeDocInfo = _nativeFixture.GetDocument(index);
            int pageCount = doc.Pages.Count;
#else
            // 1. Extract Managed Blits (C#)
            using (var doc = Util.GetTestDocument(index, out int pageCount))
            // 2. Safely initialize the DjVuLibre native document via the wrapper
            using (var nativeDocInfo = DjvuDocumentInfo.CreateDjvuDocumentInfo(filePath))
#endif
            {
                for (int p = 0; p < Math.Min(pageCount, 2); p++)
                {
                    var managedPage = doc.Pages[p];
                    var managedJb2 = managedPage.ForegroundJB2Image;
                    if (managedJb2 == null)
                    {
                        continue; // Skip pages without JB2 foregrounds
                    }

                    var managedBlits = managedJb2.Blits;

                    // 3. Safely decode the native page via the wrapper
                    using (var nativePageInfo = new DjvuPageInfo(nativeDocInfo, p))
                    {
                        IntPtr nativePage = nativePageInfo.Page;

                        bool success = NativeMethods.GetDjvuPageJb2BlitCount(nativePage, out int nativeBlitCount);
                        Assert.True(success, $"Failed to retrieve native JB2 blit count for page {p} in doc {index}");
                        Assert.Equal(nativeBlitCount, managedBlits.Length);

                        for (int b = 0; b < nativeBlitCount; b++)
                        {
                            success = NativeMethods.GetDjvuPageJb2Blit(nativePage, b, out uint shapeNo, out int left, out int bottom);
                            Assert.True(success, $"Failed to retrieve native blit at index {b} on page {p}");

                            Assert.True(
                                shapeNo == (uint)managedBlits[b].ShapeNumber &&
                                left == managedBlits[b].Left &&
                                bottom == managedBlits[b].Bottom,
                                $"Blit mismatch Doc:{index} Page:{p} Index:{b}. Native(Shape:{shapeNo}, Left:{left}, Bottom:{bottom}) vs Managed(Shape:{managedBlits[b].ShapeNumber}, Left:{managedBlits[b].Left}, Bottom:{managedBlits[b].Bottom})"
                            );
                        }
                    }
                }
            }
        }

        [Fact]
        public void JB2BlitTestIndex23()
        {
            JB2BlitTheory(23);
        }

    }
}

