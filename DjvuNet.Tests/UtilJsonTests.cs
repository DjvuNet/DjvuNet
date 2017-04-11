using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace DjvuNet.Tests
{
    public class UtilJsonTests : IDisposable
    {
        string ArtifactsDirectory = "artifacts"; 
        string ArtifactsSearchPattern = "*.djvu;*.json";
        string ArtifactsDjvuFile = "test001C.djvu";
        string ArtifactsJsonFile = "test001C.json";
        string SubdirectoryList = "data";
        string SubdirectorySearchPatterns = "*";
        string HashFilePath = "artifacts.SHA256";
        string DataDirectory = "data";
        string DataDirectoryFile = "textest.bz";
        bool CalculateHashes = false;

        string TemporaryFolder;

        public UtilJsonTests()
        {
            TemporaryFolder = Path.Combine(Path.GetTempPath(), "ArtifactsTmpTest");
            if (Directory.Exists(TemporaryFolder))
                Directory.Delete(TemporaryFolder);
            Directory.CreateDirectory(TemporaryFolder);
            ArtifactsDirectory = Path.Combine(TemporaryFolder, ArtifactsDirectory);
            Directory.CreateDirectory(ArtifactsDirectory);
            using (var file = File.Create(Path.Combine(ArtifactsDirectory, ArtifactsDjvuFile)))
                file.WriteByte(0x42);
            using (var file = File.Create(Path.Combine(ArtifactsDirectory, ArtifactsJsonFile)))
                file.WriteByte(0x41);
            DataDirectory = Path.Combine(ArtifactsDirectory, DataDirectory);
            Directory.CreateDirectory(DataDirectory);
            using (var file = File.Create(Path.Combine(DataDirectory, DataDirectoryFile)))
                file.WriteByte(0x42);

            using (FileStream file = File.Create(Path.Combine(TemporaryFolder, HashFilePath)))
                file.WriteByte(0x42);
        }

        public void Dispose()
        {
            Directory.Delete(TemporaryFolder, true);
        }

        [Fact]
        public void BuildRequiredTest001() 
        {
            FileInfo hashFileInfo = new FileInfo(HashFilePath);
            if (hashFileInfo.Exists)
            {
                string backupHashFile = HashFilePath + ".bak";
                if (File.Exists(backupHashFile))
                    File.Delete(backupHashFile);
                try
                {
                    hashFileInfo.MoveTo(backupHashFile);
                    CalculateHashes = false;
                    UtilJson.ArtifactsBuildRequired(ArtifactsDirectory, ArtifactsSearchPattern, SubdirectoryList,
                        SubdirectorySearchPatterns, HashFilePath, out CalculateHashes);
                    Assert.True(CalculateHashes);
                }
                finally
                {
                    if (File.Exists(backupHashFile))
                    {
                        if (!File.Exists(HashFilePath))
                            File.Move(backupHashFile, HashFilePath);
                        else
                            File.Delete(backupHashFile);
                    }
                }
            }
        }

        [Fact]
        public void BuildRequiredTest002()
        {
            FileInfo hashFileInfo = new FileInfo(HashFilePath);
            if (hashFileInfo.Exists)
            {
                DateTime prevLastWriteUtc = hashFileInfo.LastWriteTimeUtc;
                FileInfo test001CInfo = new FileInfo(Path.Combine(ArtifactsDirectory, ArtifactsDjvuFile));
                DateTime prevTestLastWriteUtc = test001CInfo.LastWriteTimeUtc;
                try
                {
                    test001CInfo.LastWriteTimeUtc = DateTime.UtcNow.AddDays(10);
                    CalculateHashes = false;
                    UtilJson.ArtifactsBuildRequired(ArtifactsDirectory, ArtifactsSearchPattern, SubdirectoryList,
                        SubdirectorySearchPatterns, HashFilePath, out CalculateHashes);

                    Assert.True(CalculateHashes);

                    CalculateHashes = false;
                    hashFileInfo.LastWriteTimeUtc = test001CInfo.LastWriteTimeUtc.AddDays(10);
                    UtilJson.ArtifactsBuildRequired(ArtifactsDirectory, ArtifactsSearchPattern, SubdirectoryList,
                        SubdirectorySearchPatterns, HashFilePath, out CalculateHashes);

                    Assert.False(CalculateHashes);
                }
                finally
                {
                    hashFileInfo.LastWriteTimeUtc = prevLastWriteUtc;
                    test001CInfo.LastWriteTimeUtc = prevTestLastWriteUtc;
                }
            }
        }

        [Fact]
        public void BuildRequiredTest003()
        {
            FileInfo hashFileInfo = new FileInfo(HashFilePath);
            if (hashFileInfo.Exists)
            {
                DateTime prevLastWriteUtc = hashFileInfo.LastWriteTimeUtc;
                FileInfo test001CInfo = new FileInfo(Path.Combine(DataDirectory, "test001C.djvu"));
                DateTime prevTestLastWriteUtc = test001CInfo.LastWriteTimeUtc;
                try
                {
                    test001CInfo.LastWriteTimeUtc = DateTime.UtcNow.AddDays(10);
                    CalculateHashes = false;
                    UtilJson.ArtifactsBuildRequired(ArtifactsDirectory, ArtifactsSearchPattern, SubdirectoryList,
                        SubdirectorySearchPatterns, HashFilePath, out CalculateHashes);

                    Assert.True(CalculateHashes);

                    CalculateHashes = false;
                    hashFileInfo.LastWriteTimeUtc = test001CInfo.LastWriteTimeUtc.AddDays(10);
                    UtilJson.ArtifactsBuildRequired(ArtifactsDirectory, ArtifactsSearchPattern, SubdirectoryList,
                        SubdirectorySearchPatterns, HashFilePath, out CalculateHashes);

                    Assert.False(CalculateHashes);
                }
                finally
                {
                    hashFileInfo.LastWriteTimeUtc = prevLastWriteUtc;
                    test001CInfo.LastWriteTimeUtc = prevTestLastWriteUtc;
                }
            }
        }
    }
}
