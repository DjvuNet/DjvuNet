// -----------------------------------------------------------------------
// <copyright file="AnnotationChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class AnnotationChunk : DjvuNode, IAnnotationChunk
    {

        #region Public Properties

        private Annotation[] _annotations;

        /// <summary>
        /// Gets the list of annotations
        /// </summary>
        public Annotation[] Annotations
        {
            get
            {
                if (_annotations != null)
                    return _annotations;
                else
                {
                    _annotations = ReadAnnotationData();
                    return _annotations;
                }
            }

            internal set
            {
                if (_annotations != value)
                    _annotations = value;
            }
        }

        #endregion Properties

        #region Constructors

        public AnnotationChunk() { }

        public AnnotationChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public AnnotationChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Gets the reader for the annotation data
        /// </summary>
        /// <returns></returns>
        protected abstract IDjvuReader GetAnnotationDataReader(long position);

        /// <summary>
        /// Reads the compressed annotation data
        /// </summary>
        internal Annotation[] ReadAnnotationData()
        {
            if (Length == 0)
                return new Annotation[0];

            using (IDjvuReader reader = GetAnnotationDataReader(DataOffset))
            {
                string annotationText = new UTF8Encoding(false).GetString(reader.ReadToEnd());
                return DecodeAnnotationText(annotationText);
            }
        }

        /// <summary>
        /// Decodes the text into annotations
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal Annotation[] DecodeAnnotationText(string text)
        {
            return Annotation
                .BreakIntoAnnotationPieces(text)
                .Select(piece => new Annotation(piece))
                .ToArray();
        }

        #endregion Methods
    }
}