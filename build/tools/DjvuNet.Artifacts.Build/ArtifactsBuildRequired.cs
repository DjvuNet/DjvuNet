using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DjvuNet.Artifacts.Build
{
    public class ArtifactsBuildRequired : Task
    {
        #region Properties

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

        [Output]
        public bool CalculateHashes { get; set; }

        #endregion Properties

        public override bool Execute()
        {

            try
            {
                FileInfo targetnfo = new FileInfo(HashFilePath);
                if (!targetnfo.Exists)
                    CalculateHashes = true;
                else
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(ArtifactsDirectory);
                    FileSystemInfo youngestOne = null;
                    if (dirInfo.Exists)
                    {
                        string[] patterns = ArtifactsSearchPattern.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string s in patterns)
                        {
                            FileInfo artFile = dirInfo.GetFiles(s, SearchOption.TopDirectoryOnly)
                                .OrderByDescending(file => file.LastWriteTimeUtc).FirstOrDefault();
                            if (youngestOne == null || youngestOne.LastWriteTimeUtc < artFile.LastWriteTimeUtc)
                                youngestOne = artFile;
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
                                FileSystemInfo fInfo = subdirInfo.GetFiles(pattern, SearchOption.TopDirectoryOnly)
                                    .OrderByDescending(file => file.LastWriteTimeUtc).FirstOrDefault();
                                if (youngestOne.LastWriteTimeUtc < fInfo.LastWriteTimeUtc)
                                    youngestOne = fInfo;
                            }
                        }
                    }
                    CalculateHashes = targetnfo.LastWriteTimeUtc < youngestOne.LastWriteTimeUtc;
                }
            }
            catch(Exception ex)
            {
                Log.LogErrorFromException(ex, true);
                throw new AggregateException(
                    "ArtifactsBuildRequired task failed. Check logged exception details for " +
                    "more information.", ex);
            }

            return !Log.HasLoggedErrors;
        }
    }
}
