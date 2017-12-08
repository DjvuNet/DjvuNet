// <copyright file="DjvuChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjvuChunk : DjvuFormElement
    {
        #region Fields

        private InfoChunk _Info;
        private bool _InfoQueried;

        private List<InclChunk> _Includes;

        #endregion Fields

        #region Public Properties

        public override ChunkType ChunkType
        {
            get { return ChunkType.Djvu; }
        }

        public InfoChunk Info
        {
            get
            {
                if (_InfoQueried)
                    return _Info;
                else if (Children != null)
                    _Info = (InfoChunk)Children.FirstOrDefault<IDjvuNode>(x => x.ChunkType == ChunkType.Info);

                _InfoQueried = true;
                return _Info;
            }
        }

        /// <summary>
        /// Include chunk of page or single page document.
        /// Theoretically there could be many includes but
        /// in practice it is very rare to see that kind of docs.
        /// </summary>
        public IReadOnlyList<InclChunk> Includes
        {
            get
            {
                if (_Includes != null)
                    return _Includes;
                else if (Children != null)
                {
                    _Includes = Children.Select(
                        x => x.ChunkType == ChunkType.Incl ? x as InclChunk : null)
                        .Where(x => x != null).ToList<InclChunk>();
                }
                else
                    _Includes = new List<InclChunk>();

                return _Includes;
            }
        }


        #endregion Public Properties

        #region Constructors

        public DjvuChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public DjvuChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        #endregion Protected Methods
    }
}
