using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DjvuNet.Artifacts.Build
{
    public class ArtifactsBuild : Task
    {
        [Required]
        public string HashFilePath { get; set; }

        [Required]
        public string ArtifactsDirectory { get; set; }

        [Required]
        public string ArtifactsSearchPattern { get; set; }

        [Required]
        public string SubdirectoryList { get; set; }

        [Required]
        public string SubdirectorySearchPatterns { get; set; }

        private List<Tuple<string, byte[]>> HashList { get; set; }

        public override bool Execute()
        {
            try
            {
                var files = GetFilesToProcess();
                CreateArtifactsArchive(files);
                CalculateHashes(files);
                WriteHashFile(HashFilePath, HashList);
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true);
            }

            return !Log.HasLoggedErrors;
        }

        private List<string> GetFilesToProcess()
        {
            List<FileInfo> fInfoToProcess = new List<FileInfo>();
            FileInfo targetnfo = new FileInfo(HashFilePath);
            
            DirectoryInfo dirInfo = new DirectoryInfo(ArtifactsDirectory);
            if (dirInfo.Exists)
            {
                string[] patterns = ArtifactsSearchPattern.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in patterns)
                {
                    FileInfo[] artFiles = dirInfo.GetFiles(s, SearchOption.TopDirectoryOnly);
                    fInfoToProcess.AddRange(artFiles);
                }
            }

            string[] subdirectories = SubdirectoryList.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string[] subPatterns = SubdirectorySearchPatterns.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string[][] subsubPatterns = new string[subPatterns.Length][];
            for (int i = 0; i < subPatterns.Length; i++)
            {
                string[] subsubP = subPatterns[i].Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                subsubPatterns[i] = subsubP;
            }

            for (int i = 0; i < subdirectories.Length; i++)
            {
                string d = subdirectories[i];

                DirectoryInfo subdirInfo = new DirectoryInfo(Path.Combine(ArtifactsDirectory, d));
                if (subdirInfo.Exists)
                {
                    string[] patterns = new string[] { "*" };
                    if (i < subsubPatterns.Length)
                        patterns = subsubPatterns[i];

                    foreach (string pattern in patterns)
                    {
                        FileInfo[] fInfo = subdirInfo.GetFiles(pattern, SearchOption.TopDirectoryOnly);
                        fInfoToProcess.AddRange(fInfo);
                    }
                }
            }

            var files = fInfoToProcess.OrderBy(x => x.FullName)
                .SelectMany<FileInfo, string>(f => new string[] { f.FullName }).ToList();
            return files;
        }

        public void CreateArtifactsArchive(List<string> filesList)
        {
 
        }

        public void CalculateHashes(List<string> filesList)
        {
            SHA512 hash = new SHA512Managed();

            foreach (var f in filesList)
            {
                using (FileStream stream = new FileStream(f, FileMode.Open, FileAccess.Read))
                    HashList.Add(new Tuple<string, byte[]>(f, hash.ComputeHash(stream)));

                if (!hash.CanReuseTransform)
                {
                    hash.Dispose();
                    hash = new SHA512Managed();
                }
            }
        }

        public void WriteHashFile(string hashFilePath, List<Tuple<string, byte[]>> fileHashList)
        {
            using (FileStream stream = new FileStream(hashFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                foreach(var t in fileHashList)
                {
                    writer.Write(t.Item2.ToHexString());
                    writer.Write("    ");
                    writer.WriteLine($"[{t.Item1}]");
                }
            }
        }
    }
}
