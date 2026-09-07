using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using DjvuNet.DjvuLibre;
using DjvuNet.Graphics;
using DjvuNet.JB2;
using DjvuNet.Tests;
using DjvuNet.Benchmarks;
using DjvuNet.Benchmarks.Core;

namespace DjvuNet.JB2.Benchmarks
{
    public abstract class JB2ImageBenchmarkBase : IDisposable
    {
        private static readonly Lazy<Dictionary<string, string>> _payloadMap = new Lazy<Dictionary<string, string>>(() =>
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (object[] row in Util.GetJB2ImageTestData())
            {
                string djbz = (string)row[0];
                string sjbz = (string)row[1];
                if (sjbz != null)
                {
                    string key = Path.GetFileNameWithoutExtension(sjbz);
                    if (!map.ContainsKey(key))
                        map.Add(key, djbz);
                }
            }
            return map;
        });

        protected byte[] _djbzPayload;
        protected byte[] _sjbzPayload;
        protected JB2Dictionary _sharedDict;
        protected JB2Image _decodedImage;
        protected byte[] _encodeBuffer;
        protected MemoryStream _encodeStream;
        protected DjvuWriter _writer;
        protected bool _IsDisposed;
        protected IntPtr _nativeImageHandle;
        protected IntPtr _nativeDictHandle;
        protected byte[] _nativeRenderBuffer;

        public virtual string TestFile { get; set; }

        [GlobalSetup]
        public virtual void GlobalSetup()
        {
            if (!_payloadMap.Value.TryGetValue(TestFile, out string djbzPath))
            {
                throw new FileNotFoundException($"Benchmark payload '{TestFile}' not found in jb2_chunk_map.json");
            }

            string sjbzPath = Path.Combine(Util.ArtifactsDataPath, "extracted", $"{TestFile}.sjbz");
            if (djbzPath != null)
            {
                djbzPath = Path.Combine(Util.ArtifactsDataPath, djbzPath);
            }

            _djbzPayload = djbzPath != null && File.Exists(djbzPath) ? File.ReadAllBytes(djbzPath) : null;
            _sjbzPayload = File.ReadAllBytes(sjbzPath);

            if (_djbzPayload != null)
            {
                _sharedDict = new JB2Dictionary();
                using (var ms = new MemoryStream(_djbzPayload))
                using (var reader = new DjvuReader(ms))
                    _sharedDict.Decode(reader);
            }

            _decodedImage = new JB2Image();
            using (var ms = new MemoryStream(_sjbzPayload))
            using (var reader = new DjvuReader(ms))
                _decodedImage.Decode(reader, _sharedDict);

            _encodeBuffer = GC.AllocateUninitializedArray<byte>(10 * 1024 * 1024, pinned: true);
            _encodeStream = new MemoryStream(_encodeBuffer, 0, _encodeBuffer.Length, writable: true, publiclyVisible: true);
            _writer = new DjvuWriter(_encodeStream);

            GCHandle sjbzHandle = default;
            GCHandle djbzHandle = default;
            try
            {
                sjbzHandle = GCHandle.Alloc(_sjbzPayload, GCHandleType.Pinned);
                if (_djbzPayload != null)
                {
                    djbzHandle = GCHandle.Alloc(_djbzPayload, GCHandleType.Pinned);
                    NativeMethods.CreateDjvuJb2DictFromChunk(
                        djbzHandle.AddrOfPinnedObject(), _djbzPayload.Length,
                        out int shapeCount, out _nativeDictHandle);
                }

                NativeMethods.CreateDjvuJb2ImageFromChunk(
                    sjbzHandle.AddrOfPinnedObject(), _sjbzPayload.Length,
                    djbzHandle.IsAllocated ? djbzHandle.AddrOfPinnedObject() : IntPtr.Zero,
                    _djbzPayload?.Length ?? 0,
                    out _nativeImageHandle);
            }
            finally
            {
                if (sjbzHandle.IsAllocated) sjbzHandle.Free();
                if (djbzHandle.IsAllocated) djbzHandle.Free();
            }

            _nativeRenderBuffer = new byte[10 * 1024 * 1024];
        }

        [GlobalCleanup]
        public virtual void GlobalCleanup()
        {
            Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    _writer?.Dispose();
                    _encodeStream?.Dispose();
                }

                if (_nativeImageHandle != IntPtr.Zero)
                {
                    NativeMethods.FreeDjvuJb2Image(_nativeImageHandle);
                    _nativeImageHandle = IntPtr.Zero;
                }
                if (_nativeDictHandle != IntPtr.Zero)
                {
                    NativeMethods.FreeDjvuJb2Dict(_nativeDictHandle);
                    _nativeDictHandle = IntPtr.Zero;
                }

                _IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
