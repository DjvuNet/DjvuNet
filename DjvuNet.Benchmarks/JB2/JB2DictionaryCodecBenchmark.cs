using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DjvuNet.DjvuLibre;
using DjvuNet.JB2;
using DjvuNet.Benchmarks;
using DjvuNet.Tests;

using DjvuNet.Benchmarks.Core;

namespace DjvuNet.JB2.Benchmarks
{
    [Config(typeof(ShortSimdMatrixConfig))]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class JB2DictionaryCodecBenchmark : IDisposable
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

        private byte[] _djbzPayload;
        private JB2Dictionary _sharedDict;
        
        private byte[] _encodeBuffer;
        private MemoryStream _encodeStream;
        private DjvuWriter _writer;
        private bool _IsDisposed;

        private IntPtr _nativeDictHandle;

        [Params("test032C_P01", "test049C_P01")] 
        public string TestFile { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            if (!_payloadMap.Value.TryGetValue(TestFile, out string djbzPath) || djbzPath == null)
            {
                throw new FileNotFoundException($"Benchmark dictionary payload '{TestFile}' not found or is null in jb2_chunk_map.json");
            }

            djbzPath = Path.Combine(Util.ArtifactsDataPath, djbzPath);
            _djbzPayload = File.Exists(djbzPath) ? File.ReadAllBytes(djbzPath) : null;

            if (_djbzPayload != null)
            {
                _sharedDict = new JB2Dictionary();
                using (var ms = new MemoryStream(_djbzPayload))
                using (var reader = new DjvuReader(ms))
                    _sharedDict.Decode(reader);
            }

            _encodeBuffer = GC.AllocateUninitializedArray<byte>(10 * 1024 * 1024, pinned: true);
            _encodeStream = new MemoryStream(_encodeBuffer, 0, _encodeBuffer.Length, writable: true, publiclyVisible: true);
            _writer = new DjvuWriter(_encodeStream);

            if (_djbzPayload != null)
            {
                GCHandle djbzHandle = default;
                try
                {
                    djbzHandle = GCHandle.Alloc(_djbzPayload, GCHandleType.Pinned);
                    NativeMethods.CreateDjvuJb2DictFromChunk(
                        djbzHandle.AddrOfPinnedObject(), _djbzPayload.Length, 
                        out int shapeCount, out _nativeDictHandle);
                }
                finally
                {
                    if (djbzHandle.IsAllocated) djbzHandle.Free();
                }
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
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

        [BenchmarkCategory("Decode"), Benchmark]
        public JB2Dictionary DecodeJB2Dictionary()
        {
            if (_djbzPayload == null) return null;
            var dict = new JB2Dictionary();
            using (var ms = new MemoryStream(_djbzPayload))
            using (var reader = new DjvuReader(ms))
            {
                dict.Decode(reader);
            }
            return dict;
        }

        [BenchmarkCategory("Encode"), Benchmark]
        public void EncodeJB2Dictionary()
        {
            if (_sharedDict == null) return;
            _encodeStream.Position = 0;
            _sharedDict.Encode(_writer);
        }

        [BenchmarkCategory("Decode"), Benchmark(Baseline = true)]
        public void NativeDecodeJB2Dictionary()
        {
            if (_djbzPayload == null) return;

            GCHandle djbzHandle = default;
            try
            {
                djbzHandle = GCHandle.Alloc(_djbzPayload, GCHandleType.Pinned);
                
                NativeMethods.CreateDjvuJb2DictFromChunk(
                    djbzHandle.AddrOfPinnedObject(), _djbzPayload.Length, 
                    out int shapeCount, out IntPtr dictHandle);
                    
                if (dictHandle != IntPtr.Zero)
                    NativeMethods.FreeDjvuJb2Dict(dictHandle);
            }
            finally
            {
                if (djbzHandle.IsAllocated) djbzHandle.Free();
            }
        }

        [BenchmarkCategory("Encode"), Benchmark(Baseline = true)]
        public void NativeEncodeJB2Dictionary()
        {
            if (_nativeDictHandle == IntPtr.Zero) return;

            NativeMethods.EncodeDjvuJb2DictToChunk(
                _nativeDictHandle, out IntPtr outData, out int outSize);
                
            if (outData != IntPtr.Zero)
                DjvuMarshal.FreeHGlobal(outData);
        }
    }
}
