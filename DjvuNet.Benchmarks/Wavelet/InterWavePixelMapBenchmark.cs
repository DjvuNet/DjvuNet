using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using DjvuNet.Wavelet;
using DjvuNet.Graphics;
using DjvuNet.Benchmarks.Core;
using DjvuNet.Tests;
using DjvuNet.DataChunks;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Runtime.Intrinsics.X86;

using System.Collections.Generic;

namespace DjvuNet.Benchmarks.Wavelet
{
    public class InterWaveTestParam
    {
        public string ImagePrefix { get; }
        public string ChunkID { get; }

        public InterWaveTestParam(string imagePrefix, string chunkID)
        {
            ImagePrefix = imagePrefix;
            ChunkID = chunkID;
        }

        public override string ToString() => $"{ImagePrefix}_{ChunkID}";
    }

    [Config(typeof(SimdMatrixConfig))]
    public class InterWavePixelMapBenchmark : IThroughputBenchmark, ICompressionRatioBenchmark
    {
        public IEnumerable<InterWaveTestParam> BenchmarkParams()
        {
            yield return new InterWaveTestParam("test075C", "BG44");
            yield return new InterWaveTestParam("test023C", "BG44");
            yield return new InterWaveTestParam("test074C", "BG44");
            yield return new InterWaveTestParam("test036C", "FG44");
        }

        [ParamsSource(nameof(BenchmarkParams))]
        public InterWaveTestParam TestParam { get; set; }

        private InterWavePixelMapDecoder _decoder;
        private int _width;
        private int _height;
        private int _rowSize;
        private int _blockWidth;
        private long _totalBg44FileSize;

        // Cached pre-decoded spatial channels
        private short[] _dataY;
        private short[] _dataCb;
        private short[] _dataCr;

        // Dedicated output buffers for execution
        private sbyte[] _data;
        private sbyte[] _fusedData;

        // Thread-safe cache to prevent redundant JSON parsing during report generation
        private static readonly ConcurrentDictionary<string, (long uncompressed, long compressed)> _sizeCache 
            = new ConcurrentDictionary<string, (long, long)>();

        private (long uncompressed, long compressed) GetSizes(string prefix, string targetId)
        {
            return _sizeCache.GetOrAdd($"{prefix}_{targetId}", p => 
            {
                string jsonPath = Path.Combine(Util.RepoRoot, "artifacts", "json", $"{prefix}.json");
                if (!File.Exists(jsonPath)) return (0, 0);

                string jsonStr = File.ReadAllText(jsonPath);
                using (JsonDocument doc = JsonDocument.Parse(jsonStr))
                {
                    return GetChunkSizes(doc.RootElement, targetId);
                }
            });
        }

        private (long uncompressed, long compressed) GetChunkSizes(JsonElement element, string targetId)
        {
            JsonElement formDjvu = FindFirstFormDjvu(element);
            if (formDjvu.ValueKind != JsonValueKind.Object || !formDjvu.TryGetProperty("Children", out JsonElement children))
                return (0, 0);

            long width = 0;
            long height = 0;
            long compressed = 0;

            foreach (JsonElement child in children.EnumerateArray())
            {
                if (child.TryGetProperty("ID", out JsonElement id) && id.GetString() == targetId)
                {
                    if (width == 0 && child.TryGetProperty("Width", out JsonElement w)) width = w.GetInt64();
                    if (height == 0 && child.TryGetProperty("Height", out JsonElement h)) height = h.GetInt64();
                    if (child.TryGetProperty("Size", out JsonElement s)) compressed += s.GetInt64();
                }
            }

            return (width * height * 3, compressed);
        }

        private JsonElement FindFirstFormDjvu(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("ID", out JsonElement id) && id.GetString() == "FORM:DJVU")
                    return element;

                foreach (JsonProperty prop in element.EnumerateObject())
                {
                    JsonElement result = FindFirstFormDjvu(prop.Value);
                    if (result.ValueKind == JsonValueKind.Object) return result;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in element.EnumerateArray())
                {
                    JsonElement result = FindFirstFormDjvu(item);
                    if (result.ValueKind == JsonValueKind.Object) return result;
                }
            }
            return default;
        }

        public long GetBytesPerOperation(BenchmarkCase benchmarkCase)
        {
            InterWaveTestParam param = benchmarkCase.Parameters["TestParam"] as InterWaveTestParam;
            if (param == null) return 0;
            return GetSizes(param.ImagePrefix, param.ChunkID).uncompressed;
        }

        public double GetCompressionRatio(BenchmarkCase benchmarkCase)
        {
            InterWaveTestParam param = benchmarkCase.Parameters["TestParam"] as InterWaveTestParam;
            if (param == null) return 0;
            (long uncompressed, long compressed) sizes = GetSizes(param.ImagePrefix, param.ChunkID);
            return sizes.compressed > 0 ? (double)sizes.uncompressed / sizes.compressed : 0;
        }

        [GlobalSetup]
        public void Setup()
        {
            _decoder = new InterWavePixelMapDecoder();
            string dataDir = Path.Combine(Util.ArtifactsDataPath);
            
            string[] chunkFiles = Directory.GetFiles(dataDir, $"{TestParam.ImagePrefix}_*.{TestParam.ChunkID.ToLower()}");
            Array.Sort(chunkFiles);

            long totalSize = 0;
            for (int i = 0; i < chunkFiles.Length; i++)
            {
                totalSize += new FileInfo(chunkFiles[i]).Length;
            }
            _totalBg44FileSize = totalSize;

            for (int i = 0; i < chunkFiles.Length; i++)
            {
                using (FileStream fs = File.Open(chunkFiles[i], FileMode.Open, FileAccess.Read, FileShare.Read))
                using (IDjvuReader reader = new DjvuReader(fs))
                {
                    bool isFg44 = TestParam.ChunkID == "FG44";
                    DjvuNode chunk = isFg44 ? (DjvuNode)new FG44Chunk(reader, null, null, TestParam.ChunkID, fs.Length) : new BG44Chunk(reader, null, null, TestParam.ChunkID, fs.Length);
                    
                    chunk.Initialize();
                    
                    using (IDjvuReader memReader = chunk.Reader.CloneReaderToMemory(chunk.DataOffset, chunk.Length))
                    {
                        _decoder.Decode(memReader);
                    }
                }
            }

            _width = _decoder.Width;
            _height = _decoder.Height;
            _rowSize = _width * 3;
            _blockWidth = _decoder._YMap.BlockWidth;
            
            _data = new sbyte[_height * _rowSize];
            _fusedData = new sbyte[_height * _rowSize];

            // Drown out the Wavelet Decoding Noise by caching the spatial representations in setup
            _dataY = _decoder._YMap.DecodeToSpatial(false);
            _dataCb = _decoder._CbMap?.DecodeToSpatial(true);
            _dataCr = _decoder._CrMap?.DecodeToSpatial(true);
        }

        [Benchmark(Baseline = true)]
        public unsafe void GetPixelMapStaged()
        {
            fixed (short* pY = _dataY)
            fixed (short* pCb = _dataCb)
            fixed (short* pCr = _dataCr)
            fixed (sbyte* pImg8 = _data)
            {
                if (pCb != null && pCr != null)
                {
                    InterWaveMap.UnpackPixels(pY, pImg8, _height, _width, 0, _rowSize, 3, _blockWidth);
                    InterWaveMap.UnpackPixels(pCb, pImg8, _height, _width, 1, _rowSize, 3, _blockWidth);
                    InterWaveMap.UnpackPixels(pCr, pImg8, _height, _width, 2, _rowSize, 3, _blockWidth);

                    InterWaveTransform.YCbCr2Rgb((Pixel*)pImg8, _width, _height, _rowSize);
                }
                else
                {
                    InterWaveMap.UnpackPixels(pY, pImg8, _height, _width, 0, _rowSize, 3, _blockWidth);

                    for (int y = 0, rowIndex = 0; y < _height; y++, rowIndex += _rowSize)
                    {
                        for (int x = 0, pixelIndex = rowIndex; x < _width; x++, pixelIndex += 3)
                        {
                            sbyte gray = (sbyte)(127 - pImg8[pixelIndex]);
                            pImg8[pixelIndex] = gray;
                            pImg8[pixelIndex + 1] = gray;
                            pImg8[pixelIndex + 2] = gray;
                        }
                    }
                }
            }
        }

        [Benchmark]
        public unsafe void GetPixelMap()
        {
            fixed (short* pY = _dataY, pCb = _dataCb, pCr = _dataCr)
            fixed (sbyte* pImg8 = _fusedData)
            {
                if (pCb != null && pCr != null)
                {
                    InterWaveTransform.YCbCr2Rgb(pY, pCb, pCr, pImg8, _width, _height, _rowSize, _blockWidth);
                }
                else
                {
                    InterWaveTransform.YGray2Rgb(pY, pImg8, _width, _height, _rowSize, _blockWidth);
                }
            }
        }
    }
}
