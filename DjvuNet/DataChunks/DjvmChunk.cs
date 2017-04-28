// <copyright file="DjvmChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjvmChunk : DjvuFormElement
    {
        private List<DjviChunk> _Includes;
        private List<ThumChunk> _Thumbnails;
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

        public IReadOnlyList<DjviChunk> Includes
        {
            get
            {
                if (_Includes != null)
                    return _Includes;
                else if (Children != null)
                {
                    _Includes = Children.Select(
                        (x) => x.ChunkType == ChunkType.Djvi ? x as DjviChunk : null)
                        .Where(x => x != null).ToList<DjviChunk>();
                    return _Includes;
                }
                else
                    return null;
            }
        }


        public IReadOnlyList<ThumChunk> Thumbnails
        {
            get
            {
                if (_Thumbnails != null)
                    return _Thumbnails;
                else if (Children != null)
                {
                    _Thumbnails = Children.Select(
                        (x) => x.ChunkType == ChunkType.Thum ? x as ThumChunk : null)
                        .Where(x => x != null).ToList<ThumChunk>();
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
                        x.ChunkType == ChunkType.Thum || x.ChunkType == ChunkType.Navm).ToList<IDjvuNode>();
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

        #endregion Constructors

        #region Protected Methods

        #endregion Protected Methods
    }
}