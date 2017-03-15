// <copyright file="DjvuDocument.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DjvuNet.DataChunks;
using DjvuNet.DataChunks.Directory;
using DjvuNet.DataChunks.Enums;
using DjvuNet.DataChunks.Navigation;
using DjvuNet.DataChunks.Navigation.Interfaces;

namespace DjvuNet
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjvuDocument : INotifyPropertyChanged, IDisposable
    {
        #region Private Variables

        // This should always be 4
        private long _startOffset = 4;

        private DjvuReader _reader;

        #endregion Private Variables

        #region Public Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Public Events

        #region Public Properties

        #region Identifier

        private int _identifier;

        /// <summary>
        /// Gets the unique identifier for the document
        /// </summary>
        public int Identifier
        {
            get
            {
                return _identifier;
            }

            private set
            {
                if (Identifier != value)
                {
                    _identifier = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Identifier"));

                    //OnPropertyChanged("Identifier");
                }
            }
        }

        #endregion Identifier

        #region FormChunk

        private FormChunk _formChunk;

        /// <summary>
        /// Gets the form chunk in the document
        /// </summary>
        public FormChunk FormChunk
        {
            get
            {
                return _formChunk;
            }
        }

        #endregion FormChunk

        #region Directory

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
                    _directory = (DirmChunk)FormChunk.Children.FirstOrDefault(x => x.ChunkType == ChunkTypes.Dirm);
                }

                return _directory;
            }
        }

        #endregion Directory

        #region Navigation

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
                    _navigation = (NavmChunk)FormChunk.Children.FirstOrDefault(x => x.ChunkType == ChunkTypes.Navm);
                }

                return _navigation;
            }

            private set
            {
                if (Navigation != value)
                {
                    _navigation = value;
                }
            }
        }

        #endregion Navigation

        #region Pages

        private DjvuPage[] _pages;

        /// <summary>
        /// Gets the pages for the document
        /// </summary>
        public DjvuPage[] Pages
        {
            get
            {
                return _pages;
            }

            private set
            {
                if (Pages != value)
                {
                    _pages = value;
                    OnPropertyChanged("Pages");
                }
            }
        }

        #endregion Pages

        #region ActivePage

        private DjvuPage _activePage;

        /// <summary>
        /// Gets or sets the currently active page
        /// </summary>
        public DjvuPage ActivePage
        {
            get
            {
                return _activePage;
            }

            set
            {
                if (ActivePage != value)
                {
                    _activePage = value;
                    UpdateCurrentPages();
                    UpdateImageCache();
                    OnPropertyChanged("ActivePage");
                }
            }
        }

        #endregion ActivePage

        #region IsInverted

        private bool _isInverted;

        /// <summary>
        /// True if the image is inverted, false otherwise
        /// </summary>
        public bool IsInverted
        {
            get
            {
                return _isInverted;
            }

            set
            {
                if (IsInverted != value)
                {
                    _isInverted = value;

                    OnPropertyChanged("IsInverted");

                    // Propagate value to children
                    Pages.ToList().ForEach(x => x.IsInverted = value);
                }
            }
        }

        #endregion IsInverted

        #region FirstPage

        private DjvuPage _firstPage;

        /// <summary>
        /// Gets the first page of the document
        /// </summary>
        public DjvuPage FirstPage
        {
            get
            {
                return _firstPage;
            }

            private set
            {
                if (FirstPage != value)
                {
                    _firstPage = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("FirstPage"));

                    //OnPropertyChanged("FirstPage");
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
            get
            {
                return _lastPage;
            }

            private set
            {
                if (LastPage != value)
                {
                    _lastPage = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("LastPage"));

                    //OnPropertyChanged("LastPage");
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
            get
            {
                return _nextPage;
            }

            private set
            {
                if (NextPage != value)
                {
                    _nextPage = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("NextPage"));

                    //OnPropertyChanged("NextPage");
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
            get
            {
                return _previousPage;
            }

            private set
            {
                if (PreviousPage != value)
                {
                    _previousPage = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("PreviousPage"));

                    //OnPropertyChanged("PreviousPage");
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
            get
            {
                return _name;
            }

            private set
            {
                if (Name != value)
                {
                    _name = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Name"));

                    //OnPropertyChanged("Name");
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

            private set
            {
                if (Location != value)
                {
                    _location = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Location"));
                    //OnPropertyChanged("Location");
                }
            }
        }

        #endregion Location

        #endregion Public Properties

        #region Constructors

        public DjvuDocument(string location)
        {
            Identifier = 0;            
            _reader = new DjvuReader(location);
            _name = Path.GetFileNameWithoutExtension(location);
            _location = location;

            DecodeDjvuDocument(_reader);
        }

        public DjvuDocument(string location, int identifier)
        {
            Identifier = identifier;
            _reader = new DjvuReader(location);
            _name = Path.GetFileNameWithoutExtension(location);
            _location = location;

            DecodeDjvuDocument(_reader);
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
            if (disposing)
            {
            }

            _reader?.Close();
            _reader = null;

            _Disposed = true;
        }

        ~DjvuDocument()
        {
            Dispose(false);
        }

        #endregion IDisposable implementation

        #region Public Methods

        /// <summary>
        /// Gets a chunk type by the given ID
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="ID"></param>
        /// <returns></returns>
        public TItem GetChunkByID<TItem>(string ID) where TItem : IFFChunk
        {
            if (Directory == null)
            {
                return null;
            }

            DirmComponent component = Directory.Components.FirstOrDefault(x => x.ID == ID);

            if (component == null)
            {
                return null;
            }

            TItem[] children = FormChunk.GetChildrenItems<TItem>().ToArray();
            TItem child = children.Where(x => x.Offset - 8 == component.Offset).FirstOrDefault();

            return child;
        }

        #endregion Public Methods

        #region Private Methods

        private void DecodeDjvuDocument(DjvuReader reader)
        {
            //Check the first 4 bytes
            CheckDjvuHeader(reader);
            DecodeFormChunk(reader);

            BuildPageList();

            if (Navigation == null)
            {
                Navigation = new DocumentNavigator(this);
            }

            ActivePage = Pages.FirstOrDefault();
            LastPage = Pages.Last();
            FirstPage = Pages.First();

            // Run the preload in the background with low priority
            //var thread = new Thread(() =>
            //                            {
            //                                // Start after a bit
            //                                Thread.Sleep(5000);
            //                                Pages.ToList().ForEach(x => x.Preload());
            //                            });
            //thread.Priority = ThreadPriority.Lowest;
            //thread.IsBackground = true;
            //thread.Start();
            //Task.Factory.StartNew(() => Parallel.ForEach(Pages, page => page.Preload()), TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Builds the list of pages
        /// </summary>
		private void BuildPageList()
		{
			List<DjvuPage> pages = new List<DjvuPage>();
			Queue<DirmComponent> pageHeaders = null;
			Queue<TH44Chunk> thumbnail = new Queue<TH44Chunk>(FormChunk.GetChildrenItems<TH44Chunk>());
			int pageCount = 1;
			DjviChunk[] sharedItems = FormChunk.GetChildrenItems<DjviChunk>();
			
			if (this.FormChunk.Children.Any() && this.FormChunk.Children[0].ChunkID=="DJVU")
			{
				foreach (IFFChunk child in FormChunk.Children)
					if (child is FormChunk)
				{
					FormChunk form = (FormChunk)child;
					
					TH44Chunk currentThumbnail = thumbnail.Any() ? thumbnail.Dequeue() : null;
					DjvuPage newPage = new DjvuPage(pageCount++, this, null, currentThumbnail, sharedItems, form);
					pages.Add(newPage);
				}
			}
			else
			{
				pageHeaders = new Queue<DirmComponent>(Directory.Components.Where(x => x.IsPage));
			}


			foreach (IFFChunk child in FormChunk.Children)
			{
				if (child is FormChunk)
				{
					FormChunk form = (FormChunk)child;

					if (form.Children.Any(x => x.ChunkType == ChunkTypes.Form_Djvu))
					{
						DirmComponent currentHeader = pageHeaders.Count() > 0 ? pageHeaders.Dequeue() : null;
						TH44Chunk currentThumbnail = thumbnail.Count() > 0 ? thumbnail.Dequeue() : null;
						DjvuPage newPage = new DjvuPage(pageCount++, this, currentHeader, currentThumbnail, sharedItems, form);

						pages.Add(newPage);
					}
				}
			}

			Pages = pages.ToArray();
		}

        /// <summary>
        /// Checks for a valid file header
        /// </summary>
        /// <param name="reader"></param>
        private void CheckDjvuHeader(DjvuReader reader)
        {
            byte[] header = new byte[4];
            reader.Read(header, 0, 4);

            if (header[0] != 0x41 || header[1] != 0x54 || header[2] != 0x26 || header[3] != 0x54)
            {
                throw new Exception("File header is invalid");
            }
        }

        /// <summary>
        /// Decodes the available IFF chunks
        /// </summary>
        /// <param name="reader"></param>
        private void DecodeFormChunk(DjvuReader reader)
        {
            _formChunk = new FormChunk(reader, null, this);
        }

        /// <summary>
        /// Updates the cached page images
        /// </summary>
        private void UpdateImageCache()
        {
            // Clear all except active
            Pages.Where(page => page != ActivePage).ToList().ForEach(x => x.ClearImage());

            Task.Factory.StartNew(() =>
                                      {
                                          Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                                          // Make sure the previous and next pages are preloaded
                                          var previous = PreviousPage.ForegroundJB2Image;
                                          var next = NextPage.ForegroundJB2Image;
                                      });

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
        private void UpdateCurrentPages()
        {
            // Find the index of the current page
            for (int index = 0; index < Pages.Count(); index++)
            {
                if (Pages[index] == ActivePage)
                {
                    int previous = Math.Max(index - 1, 0);
                    int next = Math.Min(index + 1, Pages.Count() - 1);

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
        private void CachePageImages(IEnumerable<DjvuPage> cachePages)
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
        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        #endregion Private Methods
    }
}