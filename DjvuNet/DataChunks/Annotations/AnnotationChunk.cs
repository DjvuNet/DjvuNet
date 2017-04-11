// -----------------------------------------------------------------------
// <copyright file="AnnotationChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DjvuNet.DataChunks.Annotations
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class AnnotationChunk : IFFChunk
    {
        #region Private Members

        private long _dataLocation = 0;

        #endregion Private Members

        #region Public Properties

        #region Annotations

        private Annotation[] _annotations;

        /// <summary>
        /// Gets the list of annotations
        /// </summary>
        public Annotation[] Annotations
        {
            get
            {
                if (_annotations == null)
                    _annotations = ReadAnnotationData();

                return _annotations;
            }

            internal set
            {
                if (_annotations != value)
                    _annotations = value;
            }
        }

        #endregion Annotations

        #endregion Public Properties

        #region Constructors

        public AnnotationChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        /// <summary>
        /// Gets the reader for the annotation data
        /// </summary>
        /// <returns></returns>
        protected abstract DjvuReader GetAnnotationDataReader(long position);

        /// <summary>
        /// Reads in the chunk data
        /// </summary>
        /// <param name="reader"></param>
        protected override void ReadChunkData(DjvuReader reader)
        {
            // Save the current position for delayed decoding
            _dataLocation = reader.Position;

            // Advance the reader
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Reads the compressed annotation data
        /// </summary>
        private Annotation[] ReadAnnotationData()
        {
            if (Length == 0) return new Annotation[0];

            using (DjvuReader reader = GetAnnotationDataReader(_dataLocation))
            {
                // Decode the annotation text
                string annotationText = reader.ReadUnknownLengthString();
                return DecodeAnnotationText(annotationText);
            }
        }

        /// <summary>
        /// Decodes the text into annotations
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private Annotation[] DecodeAnnotationText(string text)
        {
            return Annotation
                .BreakIntoAnnotationPieces(text)
                .Select(piece => new Annotation(piece))
                .ToArray();
        }

        #endregion Private Methods
    }
}