using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using DjvuNet.DjvuLibre;
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
                    _JsonDocumentList = new List<DjvuJsonDocument>();
                    string artifactsPath = Path.Combine(Util.RepoRoot, "artifacts");
                    string[] jsonFiles = Directory.GetFiles(artifactsPath, "*C*.json");

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
