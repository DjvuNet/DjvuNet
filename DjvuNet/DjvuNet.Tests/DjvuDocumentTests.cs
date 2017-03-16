using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DjvuNet.Tests
{
    [TestClass]
    public class DjvuDocumentTests
    {
        [TestMethod]
        public void DjvuDocument_ctor001()
        {
            using(DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(62, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor002()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test002.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(107, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor003()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test003.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(300, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor004()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test004.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(494, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor005()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test005.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(286, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor006()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test006.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(348, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor007()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test007.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(186, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor008()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test008.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(427, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor009()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test009.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(274, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor010()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }


        [TestMethod]
        public void DjvuDocument_ctor011()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test011.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(154, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor012()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test012.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(239, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor013()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test013.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(9, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor014()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test014.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(20, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor015()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test015.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(40, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor016()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test016.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(30, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor017()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test017.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(12, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor018()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test018.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(7, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor019()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test019.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(28, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor020()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test020.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(5, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }


        [TestMethod]
        public void DjvuDocument_ctor021()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test021.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(12, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor022()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test022.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(10, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor023()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test023.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(3, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor024()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test024.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(3, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor025()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test025.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(9, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor026()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test026.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(146, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor027()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test027.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(173, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor028()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test028.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(267, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }

        [TestMethod]
        public void DjvuDocument_ctor029()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test029.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(323, document.Pages.Length);
                Assert.IsFalse(document.Disposed);
                Assert.IsNotNull(document.Directory);
                Assert.IsNotNull(document.ActivePage);
                Assert.IsNotNull(document.FormChunk);
                Assert.IsNotNull(document.Navigation);
            }
        }
    }
}
