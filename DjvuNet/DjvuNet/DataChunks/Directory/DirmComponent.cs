// <copyright file="DirmComponent.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;

namespace DjvuNet.DataChunks.Directory
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DirmComponent
    {
        #region Public Properties

        #region ID

        private string _ID;

        /// <summary>
        /// Gets or sets the ID of the component
        /// </summary>
        public string ID
        {
            get
            {
                return _ID;
            }

            set
            {
                if (ID != value)
                {
                    _ID = value;
                }
            }
        }

        #endregion ID

        #region Name

        private string _name;

        /// <summary>
        /// Gets or sets the name of the component
        /// </summary>
        public string Name
        {
            get { return _name; }

            set
            {
                if (Name != value)
                {
                    _name = value;
                }
            }
        }

        #endregion Name

        #region Title

        private string _title;

        /// <summary>
        /// Gets or sets the title of the component
        /// </summary>
        public string Title
        {
            get { return _title; }

            set
            {
                if (Title != value)
                {
                    _title = value;
                }
            }
        }

        #endregion Title

        #region Offset

        private int _offset;

        /// <summary>
        /// Gets the offset of the component
        /// </summary>
        public int Offset
        {
            get
            {
                return _offset;
            }

            private set
            {
                if (Offset != value)
                {
                    _offset = value;
                }
            }
        }

        #endregion Offset

        #region Size

        private int _size;

        /// <summary>
        /// Gets or sets the size of the component
        /// </summary>
        public int Size
        {
            get { return _size; }

            set
            {
                if (Size != value)
                {
                    _size = value;
                }
            }
        }

        #endregion Size

        #region HasName

        private bool _hasName;

        /// <summary>
        /// True if the component has a different name
        /// </summary>
        public bool HasName
        {
            get { return _hasName; }

            private set
            {
                if (HasName != value)
                {
                    _hasName = value;
                }
            }
        }

        #endregion HasName

        #region HasTitle

        private bool _hasTitle;

        /// <summary>
        /// True if the component has a different title
        /// </summary>
        public bool HasTitle
        {
            get { return _hasTitle; }

            private set
            {
                if (HasTitle != value)
                {
                    _hasTitle = value;
                }
            }
        }

        #endregion HasTitle

        #region IsIncluded

        private bool _isIncluded;

        /// <summary>
        /// True if the component is included by other files
        /// </summary>
        public bool IsIncluded
        {
            get { return _isIncluded; }

            private set
            {
                if (IsIncluded != value)
                {
                    _isIncluded = value;
                }
            }
        }

        #endregion IsIncluded

        #region IsPage

        private bool _isPage;

        /// <summary>
        /// True if the component represents a page
        /// </summary>
        public bool IsPage
        {
            get { return _isPage; }

            private set
            {
                if (IsPage != value)
                {
                    _isPage = value;
                }
            }
        }

        #endregion IsPage

        #region IsThumbnail

        private bool _isThumbnail;

        /// <summary>
        /// True if the component represents a thumbnail
        /// </summary>
        public bool IsThumbnail
        {
            get { return _isThumbnail; }

            private set
            {
                if (IsThumbnail != value)
                {
                    _isThumbnail = value;
                }
            }
        }

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
        /// Decoder the flags for the sbyte
        /// </summary>
        /// <param name="flag"></param>
        public void DecodeFlags(sbyte flag)
        {
            // B[7]
            HasName = (flag & 128) == 1;
            HasTitle = (flag & 64) == 1;

            int remainder = flag & 3;

            if (remainder == 0 || remainder == 3) IsIncluded = true;
            else if (remainder == 1) IsPage = true;
            else if (remainder == 2) IsThumbnail = true;
            //else throw new Exception("Invalid Dirm flag value");
        }

        #endregion Public Methods
    }
}
