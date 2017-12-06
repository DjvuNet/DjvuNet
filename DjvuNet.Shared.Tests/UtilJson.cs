using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace DjvuNet.Tests
{
    public static partial class UtilJson
    {
        private static List<DjvuJsonDocument> _JsonDocumentList;

        public static IReadOnlyList<DjvuJsonDocument> JsonDocuments
        {
            get
            {
                if (_JsonDocumentList != null)
                    return _JsonDocumentList;
                else
                {
                    string artifactsPath = Path.Combine(Util.RepoRoot, "artifacts");
                    _JsonDocumentList = new List<DjvuJsonDocument>();
                    string[] jsonFiles = Directory.GetFiles(Util.ArtifactsPath, "*C*.json");
                    Array.Sort(jsonFiles);

                    foreach (string f in jsonFiles)
                    {
                        using (StreamReader reader = new StreamReader(f))
                        {
                            string json = reader.ReadToEnd();
                            DjvuJsonDocument doc = JsonConvert.DeserializeObject<DjvuJsonDocument>(json);
                            doc.DocumentFile = f;
                            _JsonDocumentList.Add(doc);
                        }
                    }
                    return _JsonDocumentList;
                }
            }
        }

        public static DjvuJsonDocument GetJsonDocument(int index)
        {
            return JsonDocuments[index];
        }

        public static IEnumerable<object[]> DjvuJsonData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                var jsonDocs = UtilJson.JsonDocuments;

                for (int i = 0; i < jsonDocs.Count; i++)
                    retVal.Add(new object[] { jsonDocs[i], i + 1 });


                return retVal;
            }
        }

        /// <summary>
        /// Build function used to determine if any build operations on test artifacts are required.
        /// Used in ArtifactsBuildRequired Task in DjvuNet.Tests.targets build file.
        /// </summary>
        /// <param name="ArtifactsDirectory">
        /// Root directory for test artifacts.
        /// </param>
        /// <param name="ArtifactsSearchPattern">
        /// File search patterns for artifacts root directory. Individual patterns can be separated with semicolon.
        /// </param>
        /// <param name="SubdirectoryList">
        /// List of subdirectories expressed as relative paths to artifacts directory separated by semicolon.
        /// </param>
        /// <param name="SubdirectorySearchPatterns">
        /// List of subdirectory lists of search patterns.
        /// Each subdirectory list is separated with semicolon and individual directory lists are separated by |</param>
        /// <param name="HashFilePath">
        /// Path to file containing hashes and names with relative paths of all files tracked files in artifacts directory.
        /// </param>
        /// <param name="CalculateHashes">
        /// If true hash file should be updated with new hashes at least for one file.
        /// </param>
        public static void ArtifactsBuildRequired(string ArtifactsDirectory, string ArtifactsSearchPattern,
            string SubdirectoryList, string SubdirectorySearchPatterns, string HashFilePath, out bool CalculateHashes)
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
                for(int i = 0; i < subPatterns.Length; i++)
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
    }

    public class DjvuJsonDataSource : IEnumerable<object[]>
    {

        public IEnumerator<object[]> GetEnumerator()
        {
            return UtilJson.DjvuJsonData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return UtilJson.DjvuJsonData.GetEnumerator();
        }
    }
}
