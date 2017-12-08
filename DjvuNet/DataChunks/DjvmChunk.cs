// <copyright file="DjvmChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

namespace DjvuNet.DataChunks
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjvmChunk : DjvuFormElement
    {
        private List<IDjviChunk> _Includes;
        private List<IThumChunk> _Thumbnails;
        private List<DjvuChunk> _Pages;

        private List<IDjvuNode> _Files;

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Djvm; }
        }

        #endregion ChunkType

        private DirmChunk _dirmData;

        /// <summary>
        /// Gets the DIRM chunk for the form
        /// </summary>
        public DirmChunk Dirm
        {
            get
            {
                if (_dirmData != null)
                    return _dirmData;
                else if (Children != null)
                {
                    _dirmData = (DirmChunk)Children.FirstOrDefault<IDjvuNode>(x => x.ChunkType == ChunkType.Dirm);
                    return _dirmData;
                }
                else
                    return null;
            }
        }

        public IReadOnlyList<IDjviChunk> Includes
        {
            get
            {
                if (_Includes != null)
                    return _Includes;
                else if (Children != null)
                {
                    _Includes = Children.Select(
                        (x) => x.ChunkType == ChunkType.Djvi ? x as IDjviChunk : null)
                        .Where(x => x != null).ToList<IDjviChunk>();
                    return _Includes;
                }
                else
                    return null;
            }
        }


        public IReadOnlyList<IThumChunk> Thumbnails
        {
            get
            {
                if (_Thumbnails != null)
                    return _Thumbnails;
                else if (Children != null)
                {
                    _Thumbnails = Children.Select(
                        (x) => x.ChunkType == ChunkType.Thum ? x as IThumChunk : null)
                        .Where(x => x != null).ToList<IThumChunk>();
                    return _Thumbnails;
                }
                else
                    return null;
            }
        }

        public IReadOnlyList<DjvuChunk> Pages
        {
            get
            {
                if (_Pages != null)
                    return _Pages;
                else if (Children != null)
                {
                    _Pages = Children.Select(
                        (x) => x.ChunkType == ChunkType.Djvu ? x as DjvuChunk : null)
                        .Where(x => x != null).ToList<DjvuChunk>();
                    return _Pages;
                }
                else
                    return null;
            }
        }

        public IReadOnlyList<IDjvuNode> Files
        {
            get
            {
                if (_Files != null)
                    return _Files;
                else if (Children != null)
                {
                    _Files = Children.Where( x =>
                        x.ChunkType == ChunkType.Djvu || x.ChunkType == ChunkType.Djvi ||
                        x.ChunkType == ChunkType.Thum ).ToList<IDjvuNode>();
                }
                else
                    _Files = new List<IDjvuNode>();

                return _Files;
            }
        }

        private NavmChunk _NavmData;

        /// <summary>
        /// Gets the Navm chunk for the form
        /// </summary>
        public NavmChunk NavmData
        {
            get
            {
                if (_NavmData != null)
                    return _NavmData;
                else
                {
                    if (Children != null && Children.Count > 0)
                        _NavmData = (NavmChunk)Children.FirstOrDefault<IDjvuNode>(x => x.ChunkType == ChunkType.Navm);
                    return _NavmData;
                }
            }
        }

        #endregion Public Properties

        #region Constructors

        public DjvmChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public DjvmChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        #endregion Protected Methods
    }
}
