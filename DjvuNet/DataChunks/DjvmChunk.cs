// <copyright file="DjvmChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Enums;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjvmChunk : FormChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Djvm; }
        }

        #endregion ChunkType

        #region DirmData

        private DirmChunk _dirmData;

        /// <summary>
        /// Gets the DIRM chunk for the form
        /// </summary>
        public DirmChunk DirmData
        {
            get
            {
                if (_dirmData == null && Children?.Length > 0)
                    _dirmData = (DirmChunk)Children.FirstOrDefault<IFFChunk>(x => x.ChunkType == ChunkType.Dirm);

                return _dirmData;
            }
        }

        #endregion DirmData

        #region NavmData

        private NavmChunk _NavmData;

        /// <summary>
        /// Gets the Navm chunk for the form
        /// </summary>
        public NavmChunk NavmData
        {
            get
            {
                if (Children != null && Children.Length > 0 && _NavmData == null)
                    _NavmData = (NavmChunk)Children.FirstOrDefault<IFFChunk>(x => x.ChunkType == ChunkType.Navm);

                return _NavmData;
            }
        }

        #endregion NavmData

        #endregion Public Properties

        #region Constructors

        public DjvmChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        #endregion Protected Methods
    }
}