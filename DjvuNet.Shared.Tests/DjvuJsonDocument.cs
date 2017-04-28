using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DjvuNet.Tests
{

    public partial class DjvuJsonDocument
    {
        public class Chunk
        {

            [JsonProperty("ID")]
            public string ID { get; set; }

            [JsonProperty("Size")]
            public int Size { get; set; }

            [JsonProperty("Description")]
            public string Description { get; set; }

            [JsonProperty("Width")]
            public int? Width { get; set; }

            [JsonProperty("Height")]
            public int? Height { get; set; }

            [JsonProperty("Version")]
            public double? Version { get; set; }

            [JsonProperty("Dpi")]
            public int? Dpi { get; set; }

            [JsonProperty("Gamma")]
            public double? Gamma { get; set; }

            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("Colors")]
            public int? Colors { get; set; }

            [JsonProperty("Slices")]
            public int? Slices { get; set; }

            [JsonProperty("Color")]
            public string Color { get; set; }
        }
    }

    public partial class DjvuJsonDocument
    {
        public class RootChild
        {

            [JsonProperty("ID")]
            public string ID { get; set; }

            [JsonProperty("Size")]
            public int Size { get; set; }

            [JsonProperty("Description")]
            public string Description { get; set; }

            [JsonProperty("DocumentType")]
            public string DocumentType { get; set; }

            [JsonProperty("FileCount")]
            public int FileCount { get; set; }

            [JsonProperty("PageCount")]
            public int PageCount { get; set; }

            [JsonProperty("Children")]
            public Chunk[] Children { get; set; }
        }
    }

    public partial class DjvuJsonDocument
    {
        public class Document
        {
            private RootChild[] _Pages;
            private RootChild[] _Files;
            private RootChild[] _Includes;
            private RootChild[] _Thumbnails;
            private RootChild _Dirm;

            [JsonProperty("ID")]
            public string ID { get; set; }

            [JsonProperty("Size")]
            public int Size { get; set; }

            [JsonProperty("Children")]
            public RootChild[] Children { get; set; }

            [JsonIgnore]
            public RootChild Dirm
            {
                get
                {
                    if (_Dirm != null)
                        return _Dirm;
                    else
                    {
                        _Dirm = Children.Where((x) => x.ID == "DIRM").FirstOrDefault();
                        return _Dirm;
                    }
                }
            }

            [JsonIgnore]
            public RootChild[] Pages
            {
                get
                {
                    if (_Pages != null)
                        return _Pages;
                    else
                    {
                        _Pages = Children.Where((x) => x.ID == "FORM:DJVU").ToArray();
                        return _Pages;
                    }
                }
            }

            [JsonIgnore]
            public RootChild[] Files
            {
                get
                {
                    if (_Files != null)
                        return _Files;
                    else
                    {
                        _Files = Children.Where((x) => x.ID != "DIRM").ToArray();
                        return _Files;
                    }
                }
            }

            public RootChild[] Includes
            {
                get
                {
                    if (_Includes != null)
                        return _Includes;
                    else
                    {
                        _Includes = Children.Where((x) => x.ID == "FORM:DJVI").ToArray();
                        return _Includes;
                    }
                }
            }

            public RootChild[] Thumbnails
            {
                get
                {
                    if (_Thumbnails != null)
                        return _Thumbnails;
                    else
                    {
                        _Thumbnails = Children.Where((x) => x.ID == "FORM:THUM").ToArray();
                        return _Thumbnails;
                    }
                }
            }
        }
    }

    public partial class DjvuJsonDocument
    {

        [JsonProperty("DjvuData")]
        public Document Data { get; set; }

        [JsonIgnore]
        public string DocumentFile { get; set; }

        public override string ToString()
        {
            return String.IsNullOrWhiteSpace(DocumentFile) ? base.ToString() : DocumentFile ;
        }

        public override bool Equals(object obj)
        {
            DjvuJsonDocument doc = obj as DjvuJsonDocument;
            if (doc == null)
                return false;

            if (!String.IsNullOrWhiteSpace(DocumentFile))
                return DocumentFile == doc.DocumentFile;
            else
                return base.Equals(doc); 
        }

        public override int GetHashCode()
        {
            if (!String.IsNullOrWhiteSpace(DocumentFile))
                return DocumentFile.GetHashCode();

            return base.GetHashCode();
        }
    }

}