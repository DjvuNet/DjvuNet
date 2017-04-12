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
    public class DjvuDocument : INotifyPropertyChanged, IDisposable, IDjvuDocument
    {

        /// <summary>
        /// DjvuFile according to specification starts with AT&T followed by FORM
        /// </summary>
        public static readonly byte[] MagicBuffer = new byte[] { 0x41, 0x54, 0x26, 0x54, 0x46, 0x4f, 0x52, 0x4d };

        public const ulong MagicUlong = 0x4d524f4654265441;

        public const string DjvuFileHeader = "AT&TFORM";

        #region Private Members

        private DjvuReader _Reader;

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

        private DjvuFormElement _rootForm;

        /// <summary>
        /// Gets the root form in the document
        /// </summary>
        public DjvuFormElement RootForm
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

        private List<DjviChunk> _Includes;

        public IReadOnlyList<DjviChunk> Includes
        {
            get
            {
                if (_Includes != null)
                    return _Includes;
                else
                {
                    _Includes = RootForm.ChunkType == ChunkType.Djvm ?
                        (List<DjviChunk>)((DjvmChunk)RootForm).Includes : new List<DjviChunk>();
                    OnPropertyChanged(nameof(Includes));
                    return _Includes;
                }
            }
        }

        private List<IDjvuPage> _Pages;

        /// <summary>
        /// Gets the pages for the document
        /// </summary>
        public IReadOnlyList<IDjvuPage> Pages
        {
            get { return _Pages; }

            internal set
            {
                if (_Pages != value)
                {
                    _Pages = (List<IDjvuPage>) value;
                    OnPropertyChanged(nameof(Pages));
                }
            }
        }

        private IDjvuPage _activePage;

        /// <summary>
        /// Gets or sets the currently active page
        /// </summary>
        public IDjvuPage ActivePage
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

        private IDjvuPage _firstPage;

        /// <summary>
        /// Gets the first page of the document
        /// </summary>
        public IDjvuPage FirstPage
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

        private IDjvuPage _lastPage;

        /// <summary>
        /// Gets the last page of the document
        /// </summary>
        public IDjvuPage LastPage
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

        private IDjvuPage _nextPage;

        /// <summary>
        /// Gets the next page of the document
        /// </summary>
        public IDjvuPage NextPage
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

        private IDjvuPage _previousPage;

        /// <summary>
        /// Gets the previous page of the document
        /// </summary>
        public IDjvuPage PreviousPage
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

        public bool IsDisposed { get { return _Disposed; } }

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

            _Reader?.Close();
            _Reader = null;

            for (int i = 0; i < _Pages?.Count; i++)
            {
                _Pages[i]?.Dispose();
                _Pages[i] = null;
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
            _Reader = new DjvuReader(filePath);
            _name = Path.GetFileNameWithoutExtension(filePath);
            _location = filePath;

            DecodeDjvuDocument(_Reader);
        }

        /// <summary>
        /// Function verifies DjVu file header and expects minimum length of 16 bytes.
        /// In sixteen bytes first eight form AT&TFORM ASCII text, 4 following ones
        /// contain length of DjVu file data (counted from first byte after file length field
        /// at position 12) and first chunk name being second part of form name (DJVM, DJVU, DJVI). 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
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

            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            else
                throw new ArgumentException($"Invalid file path: \"{filePath}\"", nameof(filePath));

        }

        /// <summary>
        /// Function verifies DjVu file header and expects minimum length of 16 bytes.
        /// In sixteen bytes first eight form AT&TFORM ASCII text, 4 following ones
        /// contain length of DjVu file data (counted from first byte after file length field
        /// at position 12) and first chunk name being second part of form name (DJVM, DJVU, DJVI).
        /// </summary>
        /// <param name="stream">
        /// Stream with data. If stream position is not at 0 stream must support seek (CanSeek cal must return true).
        /// </param>
        /// <returns></returns>
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
                        $"Stream is not set to the start of data and does not support seek. " + 
                        $"Current position: {stream.Position}", nameof(stream));

            byte[] buff = new byte[MagicBuffer.Length];
            int readBytes = stream.Read(buff, 0, buff.Length);
            return DjvuDocument.MagicUlong == BitConverter.ToUInt64(buff, 0);
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

            if (Pages?.Count > 0)
            {
                ActivePage = FirstPage = Pages[0];
                LastPage = Pages[Pages.Count - 1];
            }
        }

        public List<T> GetRootFormChildren<T>() where T : DjvuNode
        {
            if (RootForm.ChunkType == ChunkType.Djvu)
                return new List<T>(new T[] { RootForm as T });

            string id = typeof(T).Name.Replace("Chunk", null);
            ChunkType chunkType = DjvuNode.GetChunkType(id);
            return RootForm.Children.Where<IDjvuNode>(x => x.ChunkType == chunkType)
                .ToList<IDjvuNode>().ConvertAll<T>(x => { return (T)x; });
        }

        /// <summary>
        /// Builds the list of pages
        /// </summary>
		internal void BuildPageList()
		{
            if (Pages == null)
                _Pages = new List<IDjvuPage>();

            if (RootForm.ChunkType == ChunkType.Djvm)
            {
                DjvmChunk root = (DjvmChunk) RootForm;
                Queue<ThumChunk> thumbnails = new Queue<ThumChunk>(root.Thumbnails);

                _Includes = (List<DjviChunk>)root.Includes;

                foreach (DjvuChunk page in root.Pages)
                    AddPage(thumbnails, page);
            }
            else if (RootForm.ChunkType == ChunkType.Djvu)
                AddPage(null, (DjvuChunk)RootForm);

            OnPropertyChanged(nameof(Pages));
		}

        internal void AddPage(Queue<ThumChunk> thumbnails, DjvuChunk page)
        {
            ThumChunk thumbnail = thumbnails != null && thumbnails.Count > 0 ? thumbnails.Dequeue() : null;
            DjvuPage newPage = new DjvuPage(_Pages.Count + 1, this, null, thumbnail, _Includes, page);
            _Pages.Add(newPage);
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
                reader.Position = previousPosition + 4;
            }
        }

        /// <summary>
        /// Decodes the available IFF chunks
        /// </summary>
        /// <param name="reader"></param>
        internal void DecodeRootForm(DjvuReader reader)
        {
            _rootForm = DjvuFormElement.GetRootForm(reader, null, this);
            _rootForm.Initialize(reader);
            foreach (IDjvuNode chunk in _rootForm.Children)
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

        }

        /// <summary>
        /// Update the current pages
        /// </summary>
        internal void UpdateCurrentPages()
        {
            // Find the index of the current page
            for (int index = 0; index < Pages.Count; index++)
            {
                if (Pages[index] == ActivePage)
                {
                    int previous = Math.Max(index - 1, 0);
                    int next = Math.Min(index + 1, Pages.Count - 1);

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
        internal void CachePageImages(IEnumerable<IDjvuPage> cachePages)
        {
            // Clear all the current pages which are not needed
            //Pages.Except(cachePages).ToList().ForEach(x => x.ClearImage());

            // Build the list of current images
            //cachePages.AsParallel().ForAll(x =>
            //                                   {
            //                                       // Cache each page image
            //                                       x.IsPageImageCached = true;
            //                                       var result = x.Image;
            //                                   });
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