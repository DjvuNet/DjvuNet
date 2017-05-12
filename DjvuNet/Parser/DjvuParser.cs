using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet;
using DjvuNet.DataChunks;
using DjvuNet.Errors;

namespace DjvuNet.Parser
{
    public class DjvuParser : IDjvuParser
    {
        public event ParsedDjvuElement ParsedDjvuElementEvent;
        public event ParsedDjvuNode ParsedDjvuNodeEvent;

        public IAbstractDocumentTree ParseDocument(IDjvuReader reader)
        {
            throw new NotImplementedException();
        }

        protected void OnParsedDjvuNode(IDjvuNode node)
        {
            if (ParsedDjvuNodeEvent != null)
                ParsedDjvuNodeEvent(this, new ParsedDjvuNodeEventArgs(node));
        }

        protected void OnParsedDjvuElement(IDjvuElement element)
        {
            if (ParsedDjvuElementEvent != null)
                ParsedDjvuElementEvent(this, new ParsedDjvuElementEventArgs(element));
        }

        public static DjvuFormElement GetRootForm(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document)
        {
            string formStr = reader.ReadUTF8String(4);
            // use of uint in Djvu Format limits file read size to 4 GB - should be long (int64)
            int length = (int)reader.ReadUInt32BigEndian();
            string formType = reader.ReadUTF8String(4);
            reader.Position -= 4;

            ChunkType type = DjvuParser.GetFormType(formType);

            DjvuFormElement formObj = (DjvuFormElement)DjvuParser.CreateDecodedDjvuNode(reader, document, parent, type, formType, length);

            return formObj;
        }

        public static IDjvuNode CreateEncodedDjvuNode(IDjvuWriter writer, IDjvuElement parent, ChunkType chunkType, long length)
        {
            IDjvuNode result = null;

            switch (chunkType)
            {
                case ChunkType.Djvm:
                    result = new DjvmChunk(writer, parent, length);
                    break;
                case ChunkType.Djvu:
                    result = new DjvuChunk(writer, parent, length);
                    break;
                case ChunkType.Djvi:
                    result = new DjviChunk(writer, parent, length);
                    break;
                case ChunkType.Thum:
                    result = new ThumChunk(writer, parent, length);
                    break;
                case ChunkType.Dirm:
                    result = new DirmChunk(writer, parent, length);
                    break;
                case ChunkType.Navm:
                    result = new NavmChunk(writer, parent, length);
                    break;
                case ChunkType.Anta:
                    result = new AntaChunk(writer, parent, length);
                    break;
                case ChunkType.Antz:
                    result = new AntzChunk(writer, parent, length);
                    break;
                case ChunkType.Txta:
                    result = new TxtaChunk(writer, parent, length);
                    break;
                case ChunkType.Txtz:
                    result = new TxtzChunk(writer, parent, length);
                    break;
                case ChunkType.Djbz:
                    result = new DjbzChunk(writer, parent, length);
                    break;
                case ChunkType.Sjbz:
                    result = new SjbzChunk(writer, parent, length);
                    break;
                case ChunkType.FG44:
                    result = new FG44Chunk(writer, parent, length);
                    break;
                case ChunkType.BG44:
                    result = new BG44Chunk(writer, parent, length);
                    break;
                case ChunkType.TH44:
                    result = new TH44Chunk(writer, parent, length);
                    break;
                case ChunkType.BM44:
                    result = new BM44Chunk(writer, parent, length);
                    break;
                case ChunkType.BM44Form:
                    result = new BM44Form(writer, parent, length);
                    break;
                case ChunkType.PM44:
                    result = new PM44Chunk(writer, parent, length);
                    break;
                case ChunkType.PM44Form:
                    result = new PM44Form(writer, parent, length);
                    break;
                case ChunkType.Wmrm:
                    result = new WmrmChunk(writer, parent, length);
                    break;
                case ChunkType.FGbz:
                    result = new FGbzChunk(writer, parent, length);
                    break;
                case ChunkType.Info:
                    result = new InfoChunk(writer, parent, length);
                    break;
                case ChunkType.Incl:
                    result = new InclChunk(writer, parent, length);
                    break;
                case ChunkType.BGjp:
                    result = new BGjpChunk(writer, parent, length);
                    break;
                case ChunkType.FGjp:
                    result = new FGjpChunk(writer, parent, length);
                    break;
                case ChunkType.Smmr:
                    result = new SmmrChunk(writer, parent, length);
                    break;
                default:
                    throw new DjvuInvalidOperationException($"Chunk type is not supported in encoding. ChunkType: {chunkType}");
            }

            return result;
        }

        /// <summary>
        /// Builds the appropriate chunk for the ID
        /// </summary>
        /// <returns></returns>
        public static IDjvuNode CreateDecodedDjvuNode(IDjvuReader reader, IDjvuDocument rootDocument,
            IDjvuElement parent, ChunkType chunkType,
            string chunkID = "", long length = 0)
        {
            IDjvuNode result = null;

            switch (chunkType)
            {
                case ChunkType.Djvm:
                    result = new DjvmChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Djvu:
                    result = new DjvuChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Djvi:
                    result = new DjviChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Thum:
                    result = new ThumChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Dirm:
                    result = new DirmChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Navm:
                    result = new NavmChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Anta:
                    result = new AntaChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Antz:
                    result = new AntzChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Txta:
                    result = new TxtaChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Txtz:
                    result = new TxtzChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Djbz:
                    result = new DjbzChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Sjbz:
                    result = new SjbzChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.FG44:
                    result = new FG44Chunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.BG44:
                    result = new BG44Chunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.TH44:
                    result = new TH44Chunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.BM44:
                    result = new BM44Chunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.BM44Form:
                    result = new BM44Form(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.PM44:
                    result = new PM44Chunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.PM44Form:
                    result = new PM44Form(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Wmrm:
                    result = new WmrmChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.FGbz:
                    result = new FGbzChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Info:
                    result = new InfoChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Incl:
                    result = new InclChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.BGjp:
                    result = new BGjpChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.FGjp:
                    result = new FGjpChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Smmr:
                    result = new SmmrChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Cida:
                    result = new CidaChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                default:
                    result = new UnknownChunk(reader, parent, rootDocument, chunkID, length);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Gets the chunk type based on the ID
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static ChunkType GetChunkType(string ID)
        {
            try { return (ChunkType)Enum.Parse(typeof(ChunkType), ID, true); }
            catch (ArgumentException) { } // Catch unsupported chunks i.e. "LTAnno"

            return ChunkType.Unknown;
        }

        public static ChunkType GetFormType(string ID)
        {
            switch (ID.ToUpper())
            {
                case "DJVM":
                    return ChunkType.Djvm;
                case "DJVU":
                    return ChunkType.Djvu;
                case "DJVI":
                    return ChunkType.Djvi;
                case "PM44":
                    return ChunkType.PM44Form;
                case "BM44":
                    return ChunkType.BM44Form;
                case "THUM":
                    return ChunkType.Thum;
                default:
                    throw new ArgumentException(nameof(ID));
            }
        }

        /// <summary>
        /// True if the chunk is a sub form chunk, false otherwise
        /// </summary>
        public static bool IsFormChunk(ChunkType type)
        {
            switch (type)
            {
                case ChunkType.Djvu:
                case ChunkType.Djvi:
                case ChunkType.Thum:
                case ChunkType.Djvm:
                case ChunkType.BM44Form:
                case ChunkType.PM44Form:
                case ChunkType.Form:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Three types of FORM chunks could form a root of DjVu document / file:
        /// DJVU for single page documents, DJVM for multi page documents 
        /// and DJVI for include files associated with separate multi page 
        /// document - include files can not function independently (?).
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsRootFormChild(ChunkType type, ChunkType rootType = ChunkType.Djvm)
        {
            if (rootType == ChunkType.Djvm)
            {
                switch (type)
                {
                    case ChunkType.Djvu:
                    case ChunkType.Djvi:
                    case ChunkType.Thum:
                    case ChunkType.Form:
                    case ChunkType.Dirm:
                    case ChunkType.Navm:
                        return true;
                    default:
                        return false;
                }
            }
            else if (rootType == ChunkType.Djvu)
            {
                switch (type)
                {
                    case ChunkType.Djvu:
                    case ChunkType.Djvi:
                    case ChunkType.Djvm:
                    case ChunkType.Thum:
                    case ChunkType.Form:
                    case ChunkType.Dirm:
                    case ChunkType.Navm:
                        return false;
                }
            }
            else if (rootType == ChunkType.BM44Form)
            {
                switch (type)
                {
                    case ChunkType.BM44:
                        return true;
                    default:
                        return false;
                }
            }
            else if (rootType == ChunkType.PM44Form)
            {
                switch (type)
                {
                    case ChunkType.PM44:
                        return true;
                    default:
                        return false;
                }
            }

            // TODO Implement other single file docs??? 

            // DJVM form is the only chunk type which is always in document root
            if (type == ChunkType.Djvm) return false;

            return IsFormChunk(type) || type == ChunkType.Dirm ||
                type == ChunkType.Navm;
        }
    }
}
