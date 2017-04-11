// <copyright file="DjvuDocument.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks;
using DjvuNet.DataChunks.Directory;

using DjvuNet.DataChunks.Navigation;
using DjvuNet.DataChunks.Navigation.Interfaces;

namespace DjvuNet
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjvuDocument : INotifyPropertyChanged, IDisposable
    {

        /// <summary>
        /// DjvuFile according to specification starts with AT&T followed by FORM
        /// </summary>
        public static readonly byte[] MagicBuffer = new byte[] { 0x41, 0x54, 0x26, 0x54, 0x46, 0x4f, 0x52, 0x4d };

        public const string DjvuFileHeader = "AT&TFORM";

        #region Private Members

        private DjvuReader _reader;

        #endregion Private Members

        #region Public Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Public Events

        #region Public Properties

        private int _identifier;

        /// <summary>
        /// Gets the unique identifier for the document
        /// </summary>
        public int Identifier
        {
            get { return _identifier; }

            internal set
            {
                if (_identifier != value)
                {
                    _identifier = value;
                    OnPropertyChanged(nameof(Identifier));
                }
            }
        }

        private FormChunk _rootForm;

        /// <summary>
        /// Gets the root form in the document
        /// </summary>
        public FormChunk RootForm
        {
            get { return _rootForm; }

            internal set
            {
                if (_rootForm != value)
                {
                    _rootForm = value;
                    OnPropertyChanged(nameof(RootForm));
                }
            }
        }

        private DirmChunk _directory;

        /// <summary>
        /// Gets the DIRM chunk if present
        /// </summary>
        public DirmChunk Directory
        {
            get
            {
                if (_directory == null)
                {
                    _directory = RootForm.ChunkType == ChunkType.Djvm ? ((DjvmChunk)RootForm).Dirm : null;
                    if (_directory != null)
                        OnPropertyChanged(nameof(Directory));
                }

                return _directory;
            }
        }

        private INavigation _navigation;

        /// <summary>
        /// Gets the Navm chunk if present
        /// </summary>
        public INavigation Navigation
        {
            get
            {
                if (_navigation == null)
                {
                    _navigation = RootForm.ChunkType == ChunkType.Djvm ? ((DjvmChunk)RootForm).NavmData : null;
                    if (_navigation != null)
                        OnPropertyChanged(nameof(Navigation));
                }

                return _navigation;
            }

            internal set
            {
                if (_navigation != value)
                {
                    _navigation = value;
                    OnPropertyChanged(nameof(Navigation));
                }
            }
        }

        private IReadOnlyList<DjviChunk> _sharedItems;

        public IReadOnlyList<DjviChunk> Includes
        {
            get
            {
                if (_sharedItems != null)
                    return _sharedItems;
                else
                {
                    _sharedItems = RootForm.ChunkType == ChunkType.Djvm ?
                        ((DjvmChunk)RootForm).Includes : new List<DjviChunk>();
                    OnPropertyChanged(nameof(Includes));
                    return _sharedItems;
                }
            }
        }

        private DjvuPage[] _pages;

        /// <summary>
        /// Gets the pages for the document
        /// </summary>
        public DjvuPage[] Pages
        {
            get { return _pages; }

            internal set
            {
                if (_pages != value)
                {
                    _pages = value;
                    OnPropertyChanged(nameof(Pages));
                }
            }
        }

        private DjvuPage _activePage;

        /// <summary>
        /// Gets or sets the currently active page
        /// </summary>
        public DjvuPage ActivePage
        {
            get { return _activePage; }

            set
            {
                if (_activePage != value)
                {
                    _activePage = value;
                    UpdateCurrentPages();
                    UpdateImageCache();
                    OnPropertyChanged("ActivePage");
                }
            }
        }

        private bool _isInverted;

        /// <summary>
        /// True if the image is inverted, false otherwise
        /// </summary>
        public bool IsInverted
        {
            get { return _isInverted; }

            set
            {
                if (_isInverted != value)
                {
                    _isInverted = value;
                    OnPropertyChanged("IsInverted");

                    // Propagate value to children
                    Pages.ToList().ForEach(x => x.IsInverted = value);
                }
            }
        }

        #region FirstPage

        private DjvuPage _firstPage;

        /// <summary>
        /// Gets the first page of the document
        /// </summary>
        public DjvuPage FirstPage
        {
            get { return _firstPage; }

            internal set
            {
                if (_firstPage != value)
                {
                    _firstPage = value;
                    OnPropertyChanged(nameof(FirstPage));
                }
            }
        }

        #endregion FirstPage

        #region LastPage

        private DjvuPage _lastPage;

        /// <summary>
        /// Gets the last page of the document
        /// </summary>
        public DjvuPage LastPage
        {
            get { return _lastPage; }

            internal set
            {
                if (_lastPage != value)
                {
                    _lastPage = value;
                    OnPropertyChanged(nameof(LastPage));
                }
            }
        }

        #endregion LastPage

        #region NextPage

        private DjvuPage _nextPage;

        /// <summary>
        /// Gets the next page of the document
        /// </summary>
        public DjvuPage NextPage
        {
            get { return _nextPage; }

            internal set
            {
                if (_nextPage != value)
                {
                    _nextPage = value;
                    OnPropertyChanged(nameof(NextPage));
                }
            }
        }

        #endregion NextPage

        #region PreviousPage

        private DjvuPage _previousPage;

        /// <summary>
        /// Gets the previous page of the document
        /// </summary>
        public DjvuPage PreviousPage
        {
            get { return _previousPage; }

            internal set
            {
                if (_previousPage != value)
                {
                    _previousPage = value;
                    OnPropertyChanged(nameof(PreviousPage));
                }
            }
        }

        #endregion PreviousPage

        #region Name

        private string _name;

        /// <summary>
        /// Gets the name of the djvu document
        /// </summary>
        public string Name
        {
            get { return _name; }

            internal set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        #endregion Name

        #region Location

        private string _location;

        /// <summary>
        /// Gets the location of the djvu document
        /// </summary>
        public string Location
        {
            get { return _location; }

            internal set
            {
                if (_location != value)
                {
                    _location = value;
                    OnPropertyChanged(nameof(Location));
                }
            }
        }

        #endregion Location

        #endregion Public Properties

        #region Constructors

        public DjvuDocument()
        {
        }



        /// <summary>
        /// TODO docs
        /// </summary>
        /// <param name="filePath"></param>
        public DjvuDocument(string filePath)
        {
            Load(filePath, 0);
        }

        /// <summary>
        /// TODO docs
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="identifier"></param>
        public DjvuDocument(string filePath, int identifier)
        {
            Load(filePath, identifier);
        }

        #endregion Constructors

        #region IDisposable implementation

        protected bool _Disposed;

        public bool Disposed { get { return _Disposed; } }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (_Disposed)
                return;

            if (disposing)
            {
            }

            _reader?.Close();
            _reader = null;

            for (int i = 0; i < Pages?.Length; i++)
            {
                Pages[i]?.Dispose();
                Pages[i] = null;
            }

            _Disposed = true;
        }

        ~DjvuDocument()
        {
            Dispose(false);
        }

        #endregion IDisposable implementation

        #region Public Methods

        public void Load(string filePath, int identifier = 0)
        {
            Identifier = identifier;
            _reader = new DjvuReader(filePath);
            _name = Path.GetFileNameWithoutExtension(filePath);
            _location = filePath;

            DecodeDjvuDocument(_reader);
        }

        public static bool IsDjvuDocument(String filePath)
        {
            if (!String.IsNullOrWhiteSpace(filePath))
            {
                FileStream stream = null;
                bool result = false;
                if (File.Exists(filePath))
                {
                    try
                    {
                        stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        result = IsDjvuDocument(stream);
                    }
                    catch(Exception error)
                    {
                        throw new AggregateException("Error while trying to verify DjVu file.", error);
                    }
                    finally
                    {
                        stream?.Close();
                    }

                    return result;
                }
                else
                    throw new FileNotFoundException($"File was not found: {filePath}");
            }

            throw new ArgumentException($"Invalid file path: \"{filePath}\" ");
        }

        public static bool IsDjvuDocument(Stream stream)
        {
            if (null == stream)
                throw new ArgumentNullException(nameof(stream));

            // Minimum empty Djvu file will consist of file header (8 bytes)
            // followed by length of IFF stream in the form of uint (4 bytes) and 
            // "DJVM", "DJVU" or "DJVI" ASCII string (4 bytes) giving total of 16 bytes.
            if (stream.Length < MagicBuffer.Length * 2)
                return false;

            if (stream.Position != 0)
                if (stream.CanSeek)
                    stream.Position = 0;
                else
                    throw new ArgumentException(
                        $"Stream is not set to the start of data and does not support seek. Current position: {stream.Position}", nameof(stream));

            byte[] buff = new byte[MagicBuffer.Length];
            int readBytes = stream.Read(buff, 0, buff.Length);

            string actualFileHeader = Encoding.ASCII.GetString(buff);

            // TODO Verify speed of alternative test - compare UInt64 / Int64 created from first 8 bytes
            // but use BinaryReader to read first bytes.

            //readBytes = stream.Read(buff, 0, buff.Length);
            //var docLength = BitConverter.ToUInt64(buff, 0);
            //readBytes = stream.Read(buff, 0, buff.Length);
            //var formTypeTest = BitConverter.ToUInt64(MagicBuffer, 0))

            return (actualFileHeader == DjvuFileHeader);
        }

        #endregion Public Methods

        #region Private Methods

        internal void DecodeDjvuDocument(DjvuReader reader)
        {
            //Check the first 4 bytes
            CheckDjvuHeader(reader);
            DecodeRootForm(reader);

            BuildPageList();

            if (Navigation == null)
                Navigation = new DocumentNavigator(this);

            if (Pages?.Length > 0)
            {
                ActivePage = FirstPage = Pages[0];
                LastPage = Pages[Pages.Length - 1];
            }
        }

        public List<T> GetRootFormChildren<T>() where T : IFFChunk
        {
            if (RootForm.ChunkType == ChunkType.Djvu)
                return new List<T>(new T[] { RootForm as T });

            string id = typeof(T).Name.Replace("Chunk", null);
            ChunkType chunkType = IFFChunk.GetChunkType(id);
            return RootForm.Children.Where<IFFChunk>(x => x.ChunkType == chunkType)
                .ToList<IFFChunk>().ConvertAll<T>(x => { return (T)x; });
        }

        /// <summary>
        /// Builds the list of pages
        /// </summary>
		internal void BuildPageList()
		{
            //
            // TODO Handle single page document RootForm as Page
            //

			List<DjvuPage> pages = new List<DjvuPage>();
			Queue<ThumChunk> thumbnails = new Queue<ThumChunk>(GetRootFormChildren<ThumChunk>());

			_sharedItems = GetRootFormChildren<DjviChunk>();
			
            foreach (DjvuChunk child in GetRootFormChildren<DjvuChunk>())
            {
                if (child.ChunkType == ChunkType.Djvu)
                {
                    DjvuChunk form = (DjvuChunk)child;

                    //TH44Chunk currentThumbnail = thumbnail.Count > 0 ? thumbnail.Dequeue() : null;
                    // TODO Get rid of arrays for shared items
                    DjvuPage newPage = new DjvuPage(pages.Count + 1, this, null, null, Includes.ToArray(), form);
                    pages.Add(newPage);
                }
            }

			Pages = pages.ToArray();
		}

        /// <summary>
        /// Checks for a valid DjVu AT&T file header
        /// </summary>
        /// <param name="reader"></param>
        internal void CheckDjvuHeader(DjvuReader reader)
        {
            long previousPosition = reader.Position;
            try
            {
                reader.Position = 0;
                uint expectedHeader = 0x54265441;
                uint actual = reader.ReadUInt32();

                if (expectedHeader != actual)
                    throw new DjvuFormatException("DjVu \"AT$T\" file header is invalid");
            }
            finally
            {
                // 
                reader.Position = previousPosition + 4;
            }
        }

        /// <summary>
        /// Decodes the available IFF chunks
        /// </summary>
        /// <param name="reader"></param>
        internal void DecodeRootForm(DjvuReader reader)
        {
            _rootForm = FormChunk.GetRootForm(reader, null, this);
            _rootForm.Initialize(reader);
            foreach (IFFChunk chunk in _rootForm.Children)
                chunk.Initialize(reader);
        }

        /// <summary>
        /// Updates the cached page images
        /// </summary>
        internal void UpdateImageCache()
        {
            // Clear all except active
            //Pages.Where(page => page != ActivePage).ToList().ForEach(x => x.ClearImage());

            //Task.Factory.StartNew(() =>
            //                          {
            //                              Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            //                              // Make sure the previous and next pages are preloaded
            //                              var previous = PreviousPage.ForegroundJB2Image;
            //                              var next = NextPage.ForegroundJB2Image;
            //                          });

            //int pageCount = 3;

            //int index = -1;

            //// Get the item position
            //for (int x = 0; x < Pages.Count(); x++)
            //{
            //    if (Pages[x] == ActivePage)
            //    {
            //        index = x;
            //        break;
            //    }
            //}

            //if (index == -1)
            //{
            //    return;
            //}

            //new Thread(() =>
            //    {
            //        // Cache the images
            //        CachePageImages(
            //            Pages.Where(
            //                (item, pageIndex) => pageIndex >= index - pageCount && pageIndex <= index + pageCount));
            //    }).Start();
        }

        /// <summary>
        /// Update the current pages
        /// </summary>
        internal void UpdateCurrentPages()
        {
            // Find the index of the current page
            for (int index = 0; index < Pages.Length; index++)
            {
                if (Pages[index] == ActivePage)
                {
                    int previous = Math.Max(index - 1, 0);
                    int next = Math.Min(index + 1, Pages.Length - 1);

                    PreviousPage = Pages[previous];
                    NextPage = Pages[next];

                    break;
                }
            }
        }

        /// <summary>
        /// Caches the page image referenced by the page index
        /// </summary>
        /// <param name="pages"></param>
        internal void CachePageImages(IEnumerable<DjvuPage> cachePages)
        {
            // Clear all the current pages which are not needed
            Pages.Except(cachePages).ToList().ForEach(x => x.ClearImage());

            // Build the list of current images
            cachePages.AsParallel().ForAll(x =>
                                               {
                                                   // Cache each page image
                                                   x.IsPageImageCached = true;
                                                   var result = x.Image;
                                               });
        }

        /// <summary>
        /// Sends the property changed notification
        /// </summary>
        /// <param name="property"></param>
        protected void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        #endregion Private Methods
    }
}