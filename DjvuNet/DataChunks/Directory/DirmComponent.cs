// <copyright file="DirmComponent.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;

namespace DjvuNet.DataChunks
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DirmComponent
    {
        #region Public Properties

        #region ID

        /// <summary>
        /// Gets or sets the ID of the component
        /// </summary>
        public string ID { get; set; }

        #endregion ID

        #region Name

        /// <summary>
        /// Gets or sets the name of the component
        /// </summary>
        public string Name { get; set; }

        #endregion Name

        #region Title

        /// <summary>
        /// Gets or sets the title of the component
        /// </summary>
        public string Title { get; set; }

        #endregion Title

        #region Offset

        /// <summary>
        /// Gets the offset of the component
        /// </summary>
        public int Offset { get; set; }

        #endregion Offset

        #region Size

        /// <summary>
        /// Gets or sets the size of the component
        /// </summary>
        public int Size { get; set; }

        #endregion Size

        #region HasName

        /// <summary>
        /// True if the component has a different name
        /// </summary>
        public bool HasName { get; internal set; }

        #endregion HasName

        #region HasTitle

        /// <summary>
        /// True if the component has a different title
        /// </summary>
        public bool HasTitle { get; internal set; }

        #endregion HasTitle

        #region IsIncluded

        /// <summary>
        /// True if the component is included by other files
        /// </summary>
        public bool IsIncluded { get; set; }

        #endregion IsIncluded

        #region IsPage

        /// <summary>
        /// True if the component represents a page
        /// </summary>
        public bool IsPage { get; internal set; }

        #endregion IsPage

        #region IsThumbnail

        /// <summary>
        /// True if the component represents a thumbnail
        /// </summary>
        public bool IsThumbnail { get; internal set; }

        #endregion IsThumbnail

        #endregion Public Properties

        #region Constructors

        public DirmComponent(int offset)
        {
            Offset = offset;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Decode component flags.
        /// </summary>
        /// <param name="flag"></param>
        public void DecodeFlags(byte flag)
        {
            HasName = (flag >> 7) == 1;
            HasTitle = (flag & 0x80) == 1;

            int test = flag & 0x03;

            switch(test)
            {
                case 0:
                    IsIncluded = true;
                    break;
                case 1:
                    IsPage = true;
                    break;
                case 2:
                case 3:
                    IsThumbnail = true;
                    break;
            }
        }

        public override string ToString()
        {
            return  $"{ID}";
        }

        #endregion Public Methods
    }
}
