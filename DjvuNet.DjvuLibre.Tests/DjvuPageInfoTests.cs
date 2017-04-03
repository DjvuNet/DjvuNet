using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DjvuLibre;
using DjvuNet.Tests;
using Xunit;

namespace DjvuNet.DjvuLibre.Tests
{
    public class DjvuPageInfoTests : SynchronizedBase
    {
        [Fact(), Trait("Category", "DjvuLibre")]
        public void DjvuPageInfoTest001()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(62, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);
                    Assert.NotNull(page.PageInfo);
                    Assert.IsType<PageInfo>(page.PageInfo);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DjvuPageInfoTest002()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(2)))
            {
                Assert.NotNull(document);

                int fileCount = document.FileCount;
                Assert.Equal<int>(14, fileCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);
                    Assert.NotNull(page.PageInfo);
                    Assert.IsType<PageInfo>(page.PageInfo);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DjvuPageInfoTest077()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);

                int fileCount = document.FileCount;
                Assert.Equal<int>(22, fileCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);
                    Assert.NotNull(page.PageInfo);
                    Assert.IsType<PageInfo>(page.PageInfo);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DisposeTest001()
        {
            DjvuPageInfo page = null;
            try
            {
                using (DjvuDocumentInfo document =
                    DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
                {
                    Assert.NotNull(document);

                    int pageCount = document.PageCount;
                    Assert.Equal<int>(62, pageCount);

                    DocumentType type = document.DocumentType;
                    Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                    page = new DjvuPageInfo(document, 0);
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);
                }
            }
            finally
            {
                if (page != null)
                {
                    page.Dispose();
                    Assert.True(page.Disposed);
                    Assert.Equal<IntPtr>(IntPtr.Zero, page.Page);
                    page = null;
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DisposeTest003()
        {
            DjvuPageInfo page = null;
            try
            {
                using (DjvuDocumentInfo document =
                    DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
                {
                    Assert.NotNull(document);

                    int pageCount = document.PageCount;
                    Assert.Equal<int>(300, pageCount);

                    DocumentType type = document.DocumentType;
                    Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                    page = new DjvuPageInfo(document, 0);
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);
                }
            }
            finally
            {
                if (page != null)
                {
                    page.Dispose();
                    Assert.True(page.Disposed);
                    Assert.Equal<IntPtr>(IntPtr.Zero, page.Page);
                    page = null;
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DisposeTest070()
        {
            DjvuPageInfo page = null;
            try
            {
                using (DjvuDocumentInfo document =
                    DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(70)))
                {
                    Assert.NotNull(document);

                    int pageCount = document.PageCount;
                    Assert.Equal<int>(28, pageCount);

                    DocumentType type = document.DocumentType;
                    Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                    page = new DjvuPageInfo(document, 0);
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);
                }
            }
            finally
            {
                if (page != null)
                {
                    page.Dispose();
                    Assert.True(page.Disposed);
                    Assert.Equal<IntPtr>(IntPtr.Zero, page.Page);
                    page = null;
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTypeTest001()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(62, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);

                    PageType pageType = page.PageType;
                    Assert.Equal<PageType>(PageType.Compound, pageType);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 31))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);

                    PageType pageType = page.PageType;
                    Assert.Equal<PageType>(PageType.Compound, pageType);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTypeTest003()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);

                    PageType pageType = page.PageType;
                    Assert.Equal<PageType>(PageType.Compound, pageType);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 1))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);

                    PageType pageType = page.PageType;
                    Assert.Equal<PageType>(PageType.Unknown, pageType);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTypeTest030()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(30)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(1, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.SinglePage, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);

                    PageType pageType = page.PageType;
                    Assert.Equal<PageType>(PageType.Compound, pageType);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTextTest001()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(62, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 17))
                {
                    Assert.NotNull(page);
                    string expected = "20 \n\u001f\u001d\vECLECTIC SERIES. \n\u001f\u001d\vLESSON XIV. \n\u001f\u001d\v■ \n\u001f\u001d\u001f\u001d\u001f\u001dUr^& \n\u001f\u001dJ/: \n\u001f\u001dr;^9 \n\u001f\u001dnf'Jflj \n\u001f\u001dpa i« \n\u001f\u001d^tPC \n\u001f\u001dr ~\" * ii L \n\u001f\u001d\u001f\u001d> \n\u001f\u001d\u001f\u001dohuMfek > j- J \n\u001f\u001d; m*\"^^ \n\u001f\u001d\u001f\u001d\u001f\u001d- rr^r ~ \n\u001f\u001dI m \n\u001f\u001d\u001f\u001d- |PBf \n\u001f\u001dmm \n\u001f\u001d^Ww<\" 1 \n\u001f\u001drm4 \n\u001f\u001d\u001f\u001d\u001f\u001d\u001f\u001d\u001f\u001dL< ' .^J «-. 4 \n\u001f\u001dTflJIE' \"** \n\u001f\u001d\u001f\u001d\u001f\u001d\u001f\u001d\u001f\u001dV9BI — -■«. \n\u001f\u001d\u001f\u001d\u001f\u001dJ« \n\u001f\u001dv\"J0 \n\u001f\u001d^^HL'ti \n\u001f\u001d\u001f\u001d'W \n\u001f\u001d\u001f\u001d\u001f\u001d\u001f\u001d\u001f\u001d\u001f\u001d\vholdg to \n\u001f\u001d\vblind Ma'ry \n\u001f\u001d\vhand kind \n\u001f\u001d\va \n\u001f\u001d\vq k y \n\u001f\u001d\vThis old man can not see. \nHe is blind. \n\u001fMary holds him by the hand. \nShe is kind to the old blind \n\u001fman. \n\u001fLESSON XV.-REVIEW. \n\u001f\u001d\vI see ducks on the pond; Tom \nwill feed them. \n\u001f\u001d\v";
                    string text = page.Text;
                    Assert.False(String.IsNullOrWhiteSpace(text));
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTextTest002()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(2)))
            {
                Assert.NotNull(document);

                int fileCount = document.FileCount;
                Assert.Equal<int>(14, fileCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 7))
                {
                    Assert.NotNull(page);
                    string expected = "analyses are described in Supplementary Note 1, Supplementary \nFigs 37–40 and Supplementary Tables 14 and 15. Controls for \ngenome reference builds and effect of mapper choice are \npresented in Supplementary Note 1, Supplementary Figs 41–47 \nand Supplementary Table 16. All pipeline details (as given by \neach submitter) are presented in Supplementary Methods, \nSupplementary Fig. 48 and Supplementary Tables 17 and 18. \nDiscussion \nThis benchmarking exercise has highlighted the importance of \ncarefully considering all stages of the laboratory and analysis \npipelines required to generate consistent and high-quality whole- \ngenome data for cancer analysis. In this study we have isolated \nand tested individual library construction/sequencing methods \nand complete analysis pipelines. Analysis pipelines themselves are \n12 \n3 \n4 \n5 \n6 \n7 \n8 \n9 \n1 \n0 \n1 \n1 \n1 \n2 \n1 \n3 \n1 \n4 \n1 \n5 \n1 \n6 \n1 \n7 \n1 \n8 \n1 \n9 \n2 \n0 \n2 \n1 \n2 \n2 \nX \nY \n1e+01 \n1e+03 \n1e+05 \n1e+07 \nGenomic position \nFN \nFP \nTP \nMB.Q \na \n12 \n3 \n4 \n5 \n6 \n7 \n8 \n9 \n1 \n0 \n1 \n1 \n1 \n2 \n1 \n3 \n1 \n4 \n1 \n5 \n1 \n6 \n1 \n7 \n1 \n8 \n1 \n9 \n2 \n0 \n2 \n1 \n2 \n2 \nX \nY \n1e+01 \n1e+03 \n1e+05 \n1e+07 \nGenomic position \nFN \nFP \nTP \nMB.L1 \n12 \n3 \n4 \n5 \n6 \n7 \n8 \n9 \n1 \n0 \n1 \n1 \n1 \n2 \n1 \n3 \n1 \n4 \n1 \n5 \n1 \n6 \n1 \n7 \n1 \n8 \n1 \n9 \n2 \n0 \n2 \n1 \n2 \n2 \nX \nY \n1e+01 \n1e+03 \n1e+05 \n1e+07 \nGenomic position \nFN \nFP \nTP \nMB.C \n12 \n3 \n4 \n5 \n6 \n7 \n8 \n9 \n1 \n0 \n1 \n1 \n1 \n2 \n1 \n3 \n1 \n4 \n1 \n5 \n1 \n6 \n1 \n7 \n1 \n8 \n1 \n9 \n2 \n0 \n2 \n1 \n2 \n2 \nX \nY \n1e+01 \n1e+03 \n1e+05 \n1e+07 \nGenomic position \nFN \nFP \nTP \nMB.K \nb \nc \nd \nDistance between successive\nSNVs\nDistance between successive\nSNVs\nDistance between successive\nSNVs\nDistance between successive\nSNVs\nFigure 5 | Rainfall plot showing distribution of called mutations on the genome. The distance between mutations is plotted in the log scale (y axis) \nversus the genomic position on the x axis. TPs (blue), FPs (green) and FNs (red). Four MB submissions representative of distinct patterns are shown. \n(a) MB.Q is one of best balanced between FPs and FNs, with low positional bias. (b) MB.L1 has many FNs. (c) MB.C has clusters of FPs near centromeres \nand FNs on the X chromosome. (d)MB.K has a high FP rate with short distance clustering of mutations. \nARTICLE \nNATURE COMMUNICATIONS | DOI: 10.1038/ncomms10001 \n8 \nNATURE COMMUNICATIONS | 6:10001 | DOI: 10.1038/ncomms10001 | www.nature.com/naturecommunications \n";
                    string text = page.Text;
                    Assert.False(String.IsNullOrWhiteSpace(text));
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTextTest003()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 15))
                {
                    Assert.NotNull(page);
                    string expected = "4 \n\u001f\u001d1. Perspectives on Manifolds \n\u001f\u001dof M such that Vx E L there is a chart in the atlas with x E Mo. and \n<po.(L n Mo.) = {O} x JRP C JRn. \nRemark 1.1.8. A submanifold is itself a manifold. \n\u001f\u001dExample 1.1.9. The equatorial circle in the 2-sphere indicated in Figure \n1.1 is a submanifold of the 2-sphere. \nDefinition 1.1.10. Let L, M be manifolds. A map f : L \u001f M is an \nembedding if it is a homeomorphism onto its image f (L) and f (L) is a \nsubmanifold of M. \nExample 1.1.11. If L is a submanifold of M, then the inclusion map i : \nL \u001f M of an abstract copy L of L to L c M is an embedding. \n\u001f\u001dWe will also consider a slightly larger class of objects: \nDefinition 1.1.12. Set H n = {(Xl,..., X n ) E JRn : Xl > O}. An n-manifold \nwith boundary is a second countable Hausdorff space M with an atlas such \nthat Va, <Po. is a homeomorphism from Mo. to an open subset of JRn or H n . \nThe boundary of M is the set of all points in M that have a neighbor- \nhood homeomorphic to Hn but no neighborhood homeomorphic to JRn. The \nboundary of M is denoted by 8M. Points not on the boundary are called \ninterior points. Two n-manifolds with boundary are considered equivalent \nif they are homeomorphic. \nExample 1.1.13. The set Jmn = {x E JRn : IIxll < I} is an n-dimensional \nmanifold with boundary called the n-ball. For interior points, there is noth- \ning to check (because the identity map on JRn provides the required home- \nomorphism). For boundary points, an extension of the map obtained by \nstereographic projection provides the required homeomorphism. See Fig- \nure 1.5. \n\u001f\u001dFigure 1.5. The 2-ball is also called the disk. \n\u001f\u001d\v";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 19))  
                {
                    Assert.NotNull(page);
                    string expected = "8 \n\u001f\u001d1. Perspectives on Manifolds \n\u001f\u001dh' 0 h- l is smooth. (The case h 0 (h')-l is analogous.) Here \nh-I(YI, . . . , Yn) = \n( 2YI 2Yn -1 + Y\u001f + · · · + Y\u001f ) \n1 + Y\u001f + · · · + Y\u001f ' · . · , 1 + Y\u001f + · · · + y\u001f' 1 + Y\u001f + · · · + Y\u001f ' \nh' 0 h -1 (Yb . . . , Yn) = 2 1 2 (Yb . · . , Yn). \nYI + · · · + Y n \nIt follows that h' 0 h- l is smooth except at the origin where the composition \nof maps is not defined. Thus sn is a smooth manifold. \nExample 1.2.5. In the exercises you proved that the product of manifolds \nis a manifold. Since the product of smooth maps is smooth, the product \nof smooth manifolds is a smooth manifold. It follows that 'Jrn is a smooth \nmanifold. \n\u001f\u001dIn calculus we learn about differentiable maps from JRn to JRm. Some \nconcepts extend to manifolds. \nDefinition 1.2.6. Let M be a manifold with atlas {(Mo:, <Po:)} and let N \nbe a manifold with atlas {(N,B, 1/J,B)}. We say that the map f : M \u001f N is \nC q ifVa,{3, the map 1/J,B 0 f 0 <P o: I (where it is defined) is C q . \nDefinition 1.2.7. A Cq-map between Cq-manifolds with a Cq-inverse is \ncalled a Cq-diffeomorphism. A Coo-diffeomorphism is simply called a diffeo- \nmorphism. \nRemark 1.2.8. The map f : JR \u001f JR given by f(x) = x 3 is a Coo-map but \nis not a diffeomorphism because its derivative is singular at o. (In fact, it is \nnot even a Cl-diffeomorphism.) \nDefinition 1.2.9. Two Cq-manifolds are considered equivalent if there is a \nCq-diffeomorphism between them. \nIn the exercises, you will extend the notion of submanifold, manifold \nwith boundary, and submanifold of a manifold with boundary to the DIFF \ncategory, that is, to the setting in which manifolds are considered in this \nsection. \nIn the DIFF category we are interested in smooth maps between mani- \nfolds. \nExample 1.2.10. Projection from 'Jr2 = Sl X Sl onto the second factor is a \nsmooth map between manifolds. \nIn Appendix A, we introduce the notion of transversality in the category \nof DIFF manifolds. Another concept that is best described in this category is \nthat of a Morse function. We discuss the concept in more detail in Appendix \nB but provide the basic definition here. \n\u001f\u001d\v";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }
    }
}