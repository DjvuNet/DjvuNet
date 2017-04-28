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
                Assert.Equal<int>(31, pageCount);

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
                Assert.Equal<int>(23, fileCount);

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
                    Assert.Equal<int>(31, pageCount);

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
                    Assert.Equal<int>(101, pageCount);

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
                Assert.Equal<int>(31, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);

                    PageType pageType = page.PageType;
                    Assert.Equal<PageType>(PageType.Compound, pageType);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, document.PageCount - 1))
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
                Assert.Equal<int>(101, pageCount);

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
                Assert.Equal<int>(31, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 11))
                {
                    Assert.NotNull(page);
                    string expected = "14 ECLECTIC SERIES. LESSON VIM. \vit 1§ stand R amp 1 a mat Ann's mat the stand See the lamp! It is on a mat. The mat is on the stand. The lamp is Nat's, and the mat is Ann's. ";
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
                Assert.Equal<int>(101, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 15))
                {
                    Assert.NotNull(page);
                    string expected = "4 1. Perspectives on Manifolds of M such that Vx E L there is a chart in the atlas with x E Mo. and <po.(L n Mo.) = {O} x JRP C JRn. Remark 1.1.8. A submanifold is itself a manifold. Example 1.1.9. The equatorial circle in the 2-sphere indicated in Figure 1.1 is a submanifold of the 2-sphere. Definition 1.1.10. Let L, M be manifolds. A map f : L \u001f M is an embedding if it is a homeomorphism onto its image f (L) and f (L) is a submanifold of M. Example 1.1.11. If L is a submanifold of M, then the inclusion map i : L \u001f M of an abstract copy L of L to L c M is an embedding. We will also consider a slightly larger class of objects: Definition 1.1.12. Set H n = {(Xl,..., X n ) E JRn : Xl > O}. An n-manifold with boundary is a second countable Hausdorff space M with an atlas such that Va, <Po. is a homeomorphism from Mo. to an open subset of JRn or H n . The boundary of M is the set of all points in M that have a neighbor- hood homeomorphic to Hn but no neighborhood homeomorphic to JRn. The boundary of M is denoted by 8M. Points not on the boundary are called interior points. Two n-manifolds with boundary are considered equivalent if they are homeomorphic. Example 1.1.13. The set Jmn = {x E JRn : IIxll < I} is an n-dimensional manifold with boundary called the n-ball. For interior points, there is noth- ing to check (because the identity map on JRn provides the required home- omorphism). For boundary points, an extension of the map obtained by stereographic projection provides the required homeomorphism. See Fig- ure 1.5. Figure 1.5. The 2-ball is also called the disk. ";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 19))  
                {
                    Assert.NotNull(page);
                    string expected = "8 1. Perspectives on Manifolds h' 0 h- l is smooth. (The case h 0 (h')-l is analogous.) Here h-I(YI, . . . , Yn) = ( 2YI 2Yn -1 + Y\u001f + · · · + Y\u001f ) 1 + Y\u001f + · · · + Y\u001f ' · . · , 1 + Y\u001f + · · · + y\u001f' 1 + Y\u001f + · · · + Y\u001f ' h' 0 h -1 (Yb . . . , Yn) = 2 1 2 (Yb . · . , Yn). YI + · · · + Y n It follows that h' 0 h- l is smooth except at the origin where the composition of maps is not defined. Thus sn is a smooth manifold. Example 1.2.5. In the exercises you proved that the product of manifolds is a manifold. Since the product of smooth maps is smooth, the product of smooth manifolds is a smooth manifold. It follows that 'Jrn is a smooth manifold. In calculus we learn about differentiable maps from JRn to JRm. Some concepts extend to manifolds. Definition 1.2.6. Let M be a manifold with atlas {(Mo:, <Po:)} and let N be a manifold with atlas {(N,B, 1/J,B)}. We say that the map f : M \u001f N is C q ifVa,{3, the map 1/J,B 0 f 0 <P o: I (where it is defined) is C q . Definition 1.2.7. A Cq-map between Cq-manifolds with a Cq-inverse is called a Cq-diffeomorphism. A Coo-diffeomorphism is simply called a diffeo- morphism. Remark 1.2.8. The map f : JR \u001f JR given by f(x) = x 3 is a Coo-map but is not a diffeomorphism because its derivative is singular at o. (In fact, it is not even a Cl-diffeomorphism.) Definition 1.2.9. Two Cq-manifolds are considered equivalent if there is a Cq-diffeomorphism between them. In the exercises, you will extend the notion of submanifold, manifold with boundary, and submanifold of a manifold with boundary to the DIFF category, that is, to the setting in which manifolds are considered in this section. In the DIFF category we are interested in smooth maps between mani- folds. Example 1.2.10. Projection from 'Jr2 = Sl X Sl onto the second factor is a smooth map between manifolds. In Appendix A, we introduce the notion of transversality in the category of DIFF manifolds. Another concept that is best described in this category is that of a Morse function. We discuss the concept in more detail in Appendix B but provide the basic definition here. ";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTextTest004()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(4)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(10, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    string expected = "METHODS ARTICLE \npublished: 02 August 2013 \ndoi: 10.3389/fninf.2013.00014 \nHDDM: Hierarchical Bayesian estimation of the \nDrift-Diffusion Model in Python \nThomas V. Wiecki * † ,ImriSofer† and Michael J. Frank \nDepartment of Cognitive, Linguistic and Psychological Sciences, Brown University, Providence, RI, USA \nEdited by: \nYaroslav O. Halchenko, Dartmouth \nCollege, USA \nReviewed by: \nMichael Hanke, \nOtto-von-Guericke-University, \nGermany \nEric-Jan Wagenmakers, University \nof Amsterdam, Netherlands \nDylan D. Wagner, Dartmouth \nCollege, USA \n*Correspondence: \nThomas V. Wiecki, Department of \nCognitive, Linguistic and \nPsychological Sciences, Brown \nUniversity, 190 Thayer St., \nProvidence, RI 02912-1821, USA \ne-mail: thomas_wiecki@brown.edu \n†These authors have contributed \nequally to this work. \nThe diffusion model is a commonly used tool to infer latent psychological processes \nunderlying decision-making, and to link them to neural mechanisms based on response \ntimes. Although efﬁcient open source software has been made available to quantitatively \nﬁt the model to data, current estimation methods require an abundance of response \ntime measurements to recover meaningful parameters, and only provide point estimates \nof each parameter. In contrast, hierarchical Bayesian parameter estimation methods are \nuseful for enhancing statistical power, allowing for simultaneous estimation of individual \nsubject parameters and the group distribution that they are drawn from, while also \nproviding measures of uncertainty in these parameters in the posterior distribution. Here, \nwe present a novel Python-based toolbox called HDDM (hierarchical drift diffusion model), \nwhich allows fast and ﬂexible estimation of the the drift-diffusion model and the related \nlinear ballistic accumulator model. HDDM requires fewer data per subject/condition than \nnon-hierarchical methods, allows for full Bayesian data analysis, and can handle outliers in \nthe data. Finally, HDDM supports the estimation of how trial-by-trial measurements (e.g., \nfMRI) inﬂuence decision-making parameters. This paper will ﬁrst describe the theoretical \nbackground of the drift diffusion model and Bayesian inference. We then illustrate usage \nof the toolbox on a real-world data set from our lab. Finally, parameter recovery studies \nshow that HDDM beats alternative ﬁtting methods like the 2 \nχ -quantile method as well as \nmaximum likelihood estimation. The software and documentation can be downloaded at: \nhttp://ski.clps.brown.edu/hddm_docs/ \nKeywords: Bayesian modeling, drift diffusion model, Python, decision-making, software \nINTRODUCTION \nSequential sampling models (SSMs) (Townsend and Ashby, 1983) \nhave established themselves as the de-facto standard for model- \ning response-time data from simple two-alternative forced choice \ndecision making tasks (Smith and Ratcliff, 2004). Each decision \nis modeled as an accumulation of noisy information indicative of \none choice or the other, with sequential evaluation of the accu- \nmulated evidence at each time step. Once this evidence crosses \na threshold, the corresponding response is executed. This sim- \nple assumption about the underlying psychological process has \nthe appealing property of reproducing not only choice proba- \nbilities, but the full distribution of response times for each of \nthe two choices. Models of this class have been used success- \nfully in mathematical psychology since the 60’s and more recently \nadopted in cognitive neuroscience investigations. These studies \nare typically interested in neural mechanisms associated with \nthe accumulation process or for regulating the decision thresh- \nold (e.g., Forstmann et al., 2008; Ratcliff et al., 2009; Cavanagh \net al., 2011). One issue in such model-based cognitive neuro- \nscience approaches is that the trial numbers in each condition are \noften low, making it difﬁcult to estimate model parameters. For \nexample, studies with patient populations, especially if combined \nwith intra-operative recordings, typically have substantial con- \nstraints on the duration of the task. Similarly, model-based fMRI \nor EEG studies are often interested not in static model parameters, \nbut how these dynamically vary with trial-by-trial variations in \nrecorded brain activity. Efﬁcient and reliable estimation methods \nthat take advantage of the full statistical structure available in the \ndata across subjects and conditions are critical to the success of \nthese endeavors. \nBayesian data analytic methods are quickly gaining popu- \nlarity in the cognitive sciences because of their many desir- \nable properties (Kruschke, 2010; Lee and Wagenmakers, 2013). \nFirst, Bayesian methods allow inference of the full posterior \ndistribution of each parameter, thus quantifying uncertainty in \ntheir estimation, rather than simply provide their most likely \nvalue. Second, hierarchical modeling is naturally formulated in \na Bayesian framework. Traditionally, psychological models either \nassume subjects are completely independent of each other, ﬁt- \nting models separately to each individual, or that all subjects are \nthe same, ﬁtting models to the group as if they are all copies of \nsome “average subject.” Both approaches are sub-optimal in that \nthe former fails to capitalize on statistical strength offered by the \ndegreetowhichsubjectsaresimilarwithrespecttooneormore \nmodel parameters, whereas the latter approach fails to account for \nthe differences among subjects, and hence could lead to a situa- \ntion where the estimated model cannot ﬁt any individual subject. \nThe same limitations apply to current DDM software pack- \nages such as DMAT (Vandekerckhove and Tuerlinckx, 2008)and \nfast-dm (Voss and Voss, 2007). Hierarchical Bayesian methods \nFrontiers in Neuroinformatics \nwww.frontiersin.org \nAugust 2013 | Volume 7 | Article 14 | 1 \nNEUROINFORMATICS \n";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 1))
                {
                    Assert.NotNull(page);
                    string expected = "Wiecki et al. \nHDDM \nprovide a remedy for this problem by allowing group and subject \nparameters to be estimated simultaneously at different hierar- \nchical levels (Kruschke, 2010; Vandekerckhove et al., 2011; Lee \nand Wagenmakers, 2013). Subject parameters are assumed to be \ndrawn from a group distribution, and to the degree that subjects \nare similar to each other, the variance in the group distribution \nwill be estimated to be small, which reciprocally has a greater \ninﬂuence on constraining parameter estimates of any individual. \nEven in this scenario, the method still allows the posterior for any \ngiven individual subject to differ substantially from that of the rest \nof the group given sufﬁcient data to overwhelm the group prior. \nThus the method capitalizes on statistical strength shared across \nthe individuals, and can do so to different degrees even within the \nsame sample and model, depending on the extent to which sub- \njects are similar to each other in one parameter vs. another. In \nthe DDM for example, it may be the case that there is relatively \nlittle variability across subjects in the perceptual time for stim- \nulus encoding, quantiﬁed by the “non-decision time” but more \nvariability in their degree of response caution, quantiﬁed by the \n“decision threshold.” The estimation should be able to capital- \nize on this structure so that the non-decision time in any given \nsubject is anchored by that of the group, potentially allowing for \nmore efﬁcient estimation of that subject’s decision threshold. This \napproach may be particularly helpful when relatively few trials \nper condition are available for each subject, and when incorpo- \nrating noisy trial-by-trial neural data into the estimation of DDM \nparameters. \nHDDM is an open-source software package written in Python \nwhich allows (1) the ﬂexible construction of hierarchical Bayesian \ndrift diffusion models and (2) the estimation of its posterior \nparameter distributions via PyMC (Patil et al., 2010). User- \ndeﬁned models can be created via a simple Python script or be \nused interactively via, for example, the IPython interpreter shell \n(Pérez and Granger, 2007).Allr \nu \nn \n-tim \nec \nritic \nalfu \nn \nctio \nn \nsa \nr \ne \ncodedinCython(Behnel et al., 2011) and compiled natively for \nspeed which allows estimation of complex models in minutes. \nHDDM includes many commonly used statistics and plotting \nfunctionality generally used to assess model ﬁt. The code is \nreleased under the permissive BSD 3-clause license, test-covered \nto assure correct behavior and well documented. An active mail- \ning list exists to facilitate community interaction and help users. \nFinally, HDDM allows ﬂexible estimation of trial-by-trial regres- \nsions where an external measurement (e.g., brain activity as mea- \nsured by fMRI) is correlated with one or more decision-making \nparameters. \nThis report is intended to familiarize experimentalists with the \nusage and beneﬁts of HDDM. The purpose of this report is thus \ntwo-fold; (1) we brieﬂy introduce the toolbox and provide a tuto- \nrial on a real-world data set (a more comprehensive description of \nall the features can be found online); and (2) characterize its suc- \ncess in recovering model parameters by performing a parameter \nrecovery study using simulated data to compare the hierarchi- \ncal model used in HDDM to non-hierarchical or non-Bayesian \nmethods as a function of the number of subjects and trials. We \nshow that it outperforms these other methods and has greater \npower to detect dependencies of model parameters on other mea- \nsures such as brain activity, when such relationships are present \nin the data. These simulation results can also inform experimen- \ntal design by showing minimum number of trials and subjects to \nachieve a desired level of precision. \nMETHODS \nDRIFT DIFFUSION MODEL \nSSMs generally fall into one of two classes: (1) diffusion mod- \nels which assume that relative evidence is accumulated over time \nand (2) race models which assume independent evidence accu- \nmulation and response commitment once the ﬁrst accumulator \ncrossed a boundary (LaBerge, 1962; Vickers, 1970). Currently, \nHDDM includes two of the most commonly used SSMs: the drift \ndiffusion model (DDM) (Ratcliff and Rouder, 1998; Ratcliff and \nMcKoon, 2008) belonging to the class of diffusion models and the \nlinear ballistic accumulator (LBA) (Brown and Heathcote, 2008) \nbelonging to the class of race models. In the remainder of this \npaper we focus on the more commonly used DDM. \nAs input these methods require trial-by-trial RT and choice \ndata(HDDMcurrentlyonlysupportsbinarydecisions)asillus- \ntrated in the below example table: \nRT \nResponse \nCondition \nBrain measure \n0.8 \n1 \nhard \n0.01 \n1.2 \n0 \neasy \n0.23 \n0.25 \n1 \nhard \n0.3 \nThe DDM models decision-making in two-choice tasks. Each \nchoice is represented as an upper and lower boundary. A drift- \nprocess accumulates evidence over time until it crosses one of the \ntwo boundaries and initiates the corresponding response (Ratcliff \nand Rouder, 1998; Smith and Ratcliff, 2004)(s \ne \neFigure 1 for \nan illustration). The speed with which the accumulation pro- \ncess approaches one of the two boundaries is called drift-rate v. \nFIGURE 1 | Trajectories of multiple drift-processes (blue and red lines, \nmiddle panel). Evidence is noisily accumulated over time (x-axis) with \naverage drift-rate v until one of two boundaries (separated by threshold a)is \ncrossed and a response is initiated. Upper (blue) and lower (red) panels \ncontain density plots over boundary-crossing-times for two possible \nresponses. The ﬂat line in the beginning of the drift-processes denotes the \nnon-decision time t where no accumulation happens. The histogram \nshapes match closely to those observed in response time measurements \nof research participants. Note that HDDM uses a closed-form likelihood \nfunction and not actual simulation as depicted here. \nFrontiers in Neuroinformatics \nwww.frontiersin.org \nAugust 2013 | Volume 7 | Article 14 | 2 \n";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTextTest041()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(41)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(14, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 6))
                {
                    Assert.NotNull(page);
                    string expected = "12 \n13 \nFOKUS 2012 \nFOKUS 2012 \nden ledende polarnasjon \nEtableringen av såkalte ”arktiske brigader” \nrepresenterer en videreføring og modernisering \nav de eksisterende militære styrkene i de russiske \nnordområder og viser at Russland er seg sin rolle \nsom regional stormakt bevisst. Brigadene viser \nat Russland satser på å sikre sine interesser i \nnordområdene og tar mål av seg til å være den \nførende nasjon i regionen. \nRusslands satsing i Arktis og nordområdene vil \nfortsatt ha politisk prioritet, og kontinuitet vil \nprege landets utenrikspolitikk. \nRusslands politikk og ambisjoner i Arktis og \nnordområdene er en integrert del av landets \nstormaktsambisjoner. I kraft av territorium, \nressurser og aktivitet, er Russland allerede i dag \nden ledende polarnasjon. Russland fortsetter ut- \nviklingen av ”Den nordlige sjørute” med tilhørende \ninfrastruktur langs Nordøstpassasjen. Sammen \nmed planlagt utbygging av nye, store energifelter \ni russisk område, viser dette hvilken betydning \nArktis har som energiprovins for Russland. \nStatus quo i utenrikspolitikken \nUtenrikspolitisk vil regimet trolig anse det \nformålstjenlig å opprettholde de bedrede \nforbindelsene til Vesten fra Medvedevs president- \nperiode, ikke minst for å ivareta russiske \nkommersielle interesser. Medvedev har fremstått \nsom en mer samarbeidsvillig utenrikspolitisk aktør \nenn Putin, særlig overfor Vesten. Imidlertid har \ndet etter alt å dømme vært enighet i det russiske \nlederskapet om hovedlinjene i utenrikspolitikken. \nRussiske reaksjoner på USAs planer om missil- \nforsvar i Europa vil derimot kunne medføre en \nforsuring av forholdet. \nDet forventes ingen substansielle endringer i \nRusslands utenrikspolitiske prioriteringer som \nfølge av presidentskiftet i 2012. Prioriteringene \nligger fast og er nedfelt i det omfattende \ndoktrinegrunnlaget som ble publisert i \nperioden 2008-2010. \nDet omfattende våpenprogrammet innbærer \nen revitalisering og modernisering av Russlands \nmilitære kapasitet. Forholdet mellom Norge og \nRussland er for tiden godt, og Russland utgjør \ningen militær trussel mot Norge. Norge må \nforholde seg til at Russland er en stat med \nutpreget autoritære trekk, som er sårbar for \nfremtidig uro og som samtidig har ambisjoner om \nå være en global og regional militærmakt. \nvalutareserver og lav utenlandsgjeld. IMF/OECD \nanslår at Russland vil ha en vekst på 3,5-4 % i BNP \ni perioden 2012-2014. Dette er langt høyere enn \nprognosene for EU-landene, men lavere enn før \nden økonomiske krisen i 2008-09 – og lavere enn \nforventet vekst i de andre BRICS-landene sett \nunder ett. Den russiske statsgjelden utgjør kun ti \nprosent av BNP, mot 80-120 % i mange europeiske \nland. Russland har derimot få realøkonomiske \nkonkurransefortrinn, noe som kan føre til \nproblemer for russisk økonomi i årene som \nkommer. Investeringsklimaet i Russland anses \ndessuten som lite gunstig, men det kommende \nrussiske WTO-medlemskapet, vil kunne bedre \ndette og vil dessuten kunne føre til mer stabilitet \ni russisk økonomi fremover. \nDersom den økonomiske krisen i USA og i \neurosonen forverres ytterligere, kan dette også \nfå innvirkning på russisk økonomi. Stor usikkerhet \ni energimarkedet vil kunne føre til store sving- \nninger i olje- og gassprisene og dermed i statens \ninntekter. En kraftig nedgang i oljeprisen vil kunne \nsvekke den russiske økonomien i alvorlig grad. \nEtter vårens presidentvalg vil Russlands ledelse \ntrolig måtte gjennomføre budsjettkutt, blant \nannet gjennom reformer av pensjonssystemet. \nDet er lite trolig at dette vil føre til omfattende \nsosial uro på kort sikt, men dersom tilstanden i \nrussisk økonomi forverres drastisk, vil dette kunne \ntrue landets politiske stabilitet på noe lengre sikt. \nStyrket militærmakt \nMakttandemet Putin/Medvedev har støttet \nreform, gjenoppbygging og styrking av \nden russiske militærmakten. Dersom Våpen- \nanskaffelsesplanen fra 2010 blir fulgt opp med \ntilstrekkelige midler, vil dette på sikt føre til en \nbetydelig revitalisering av de russiske væpnede \nstyrker. \nAndelen av statsbudsjettet som settes av til \nbudsjettposten ”Nasjonalt forsvar” vil øke \nbetydelig, og etter budsjettplanen for perioden \n2012-14 skal forsvarsbudsjettet økes med nesten \n50 % i 2014 sammenlignet med 2011 (i reelle \ntall). En stor andel går til finansiering av våpen- \nanskaffelsesprogrammet. \nDette programmet (GPV 2020) ble vedtatt i \ndesember. Programmet skal finansiere våpen- \nanskaffelser for de russiske væpnede styrker i \ntiden frem til 2020. Det foreligger sterk politisk \nstøtte til GPV 2020 og programmet vil innbære \nen vesentlig, kvalitativ modernisering av den \nrussiske forsvarsmakten dersom planen får \ntilstrekkelig finansiering og prioritet de neste \nårene. Dette, sammen med en omlegging og \nmodernisering av forsvarsindustrien, tyder på \nat intensjonen om en styrket militærmakt følges \nkonkret opp. Inngåelsen av en rekke nye forsvars- \nkontrakter peker i samme retning. \n« det omfattende våpenprogrammet innebærer en revitalisering \nog modernisering av Russlands militære kapasitet.» \nBilde:Scanpix \nVladimir Putin \nBilde:Scanpix \nBilde: Gettyimages \nBilde: Gettyimages \nFolkemeningen blir en faktor å regne med: Anti-Putin demonstrasjon i Moskva 4. februar. \n";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 13))
                {
                    Assert.NotNull(page);
                    string expected = "26 \n27 \nFOKUS 2012 \nFOKUS 2012 \nDet digitale rom omfatter bredden av digitale \nløsninger som moderne samfunn baserer seg \npå innen kommunikasjon, oppbevaring av \ninformasjon og styringssystemer for infra- \nstruktur. Det kan utnyttes av aktører som ønsker \ninformasjon om sivile og militære nettverk, som vil \nforstyrre nasjonale forhold og prosesser, slik \nEstland erfarte i 2007 og Georgia i 2008. \nHensikten kan også være å ødelegge infra- \nstruktur eller å endre politiske beslutninger. Iran \nopplevde dette i 2010, gjennom et angrep på sitt \nurananrikningsanlegg. I 2011 viste opprullingen \nav den såkalte Operasjon Night-Dragon, som \nrettet seg mot et knippe internasjonale energi- \nselskaper, hvordan en statlig aktør gjennom flere \når systematisk hadde utnyttet sikkerhetshull for \ntil slutt å skaffe seg muligheten til å hente ut \nsensitiv informasjon fra selskapene. \nAlle disse formene for digital påvirkning inne- \nbærer risiko eller trusler mot et lands interesser. \nStorstilte, offensive operasjoner fra én stat \nmot en annen vil kunne bli viktige elementer \ni fremtidige konflikter. \nStedfortredere skjuler spor \nEnkelte stater har flere og kraftigere virkemidler \nenn øvrige aktører. De kan gjennomføre mål- \nrettede og virkningsfulle datanettverksoperasjoner \nsom ofte er vanskelige å avdekke. Slike aktører \nog måten de opererer på er derfor lite kjent for \noffentligheten. Den senere tid har det likevel \nkommet ut informasjon som tyder på at \nomfattende og avanserte operasjoner har pågått \nover flere år. Dette er operasjoner som har et \nbredt – ja globalt – nedslagsfelt. Flere stater \nbygger opp – eller har allerede etablert – såkalte \nmilitære cyberkommandoer, nasjonale cyber- \nsentra og dedikerte enheter. Utfordringen er at \nde ofte bruker stedfortredere (hacktivistgrupper, \nuniversitetsmiljøer, halvstatlige bedrifter o.a.) \ntil å utføre handlinger som gjør det vanskelig \nå spore operasjonen tilbake til en statlig \nmyndighetsstruktur. Det reduserer selvsagt også \nmuligheten for å stille aktørene til ansvar og gå \ntil motreaksjoner. \nI det digitale rom har angriperne alltid en fordel. \nDe trenger kun å finne ett hull for å penetrere et \nsystem, mens den som skal forsvare systemet, \nmå finne og tette alle hull. Det stiller store krav \ntil effektivt nasjonalt samarbeid å sette inn \nrelevante mottiltak: Helhetlige responsplaner, \nløpende informasjonsutveksling mellom sivile \nog militære aktører og integrert internasjonalt \nsamarbeid. \ntRUSlER I dEt dIGItalE ROm \nAvanserte statlige aktører står bak betydelig aktivitet for å innhente sensitiv informasjon om \nandre lands disposisjoner, teknologi, økonomi og forsvar. I en krisesituasjon har de evne til å \nmanipulere andre staters beslutningsmekanismer og infrastruktur. \n« I fredstid er den mest omfattende trusselen mot norske interesser \ntyveri av kommersielle data og intellektuell eiendom.» \nBakdorer som kan anvendes senere \nI fredstid er den mest omfattende trusselen mot \nnorske interesser tyveri av kommersielle data og \nintellektuell eiendom. I samme forbindelse kan \nimidlertid en aktør plante såkalte bakdører som \nkan anvendes senere i en krise eller konflikt for å \nforstyrre eller ødelegge systemer og prosesser. I \nnoen tilfeller vil spionasje i fredstid kunne danne \nutgangspunkt for etablering av en plattform for \noffensive operasjoner ved en senere anledning. \nVirksomheter i offentlig og privat sektor kan gjøre \nmye for å redusere risikoen som knytter seg til \nøkonomisk kriminalitet, hackervirksomhet og \nandre lavskala-trusler. Forbedring av datasik- \nkerheten og styrking av brukerbevisstheten er \nviktige tiltak. \nAvdekking av høyskala-truslene som knytter seg \ntil spionasje, subversjon og sabotasje krever \nimidlertid et omfattende samarbeid mellom \nsikkerhetsmyndigheter, politi og etterretnings- \ntjenesten. Bare en moderne utenlands- \netterretning har evne til å kartlegge trussel- \naktørene, deres motiver, kapasitet og metoder. \n« I det digitale rom har angriperne alltid en fordel.» \nBilde: Gettyimages \n";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTextTest076()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(76)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(18, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    string expected = "PLOS ONE | DOI:10.1371/journal.pone.0166378  November 15, 2016 1 / 18\n−1\nOPEN ACCESS\nCitation: Wu YS, Chen Y-T, Bao Y-T, Li Z-M, Zhou \nX-J, He J-N, et al. (2016) Identification and \nVerification of Potential Therapeutic Target Genes \nin Berberine-Treated Zucker Diabetic Fatty Rats \nthrough Bioinformatics Analysis. PLoS ONE 11 \n(11): e0166378. doi:10.1371/journal. \npone.0166378\nEditor: Michael Bader, Max Delbruck Centrum fur \nMolekulare Medizin Berlin Buch,GERMANY\nReceived: September 5, 2016\nAccepted: October 27, 2016\nPublished: November 15, 2016\nCopyright: © 2016 Wu et al. This is an open access \narticle distributed under the terms of the Creative\nCommons Attribution License, which permits \nunrestricted use, distribution,and reproduction in \nany medium,provided the originalauthorand \nsource are credited.\nData Availability Statement: All microarray data \nfiles are available from ArrayExpress database \n(accession number: E-MTAB-5178).\nFunding: This work was supported by the Project \nof National Great New Drug Research and \nDevelopment (No.2012ZX09503001-001), and \nTraditionalChineseMedicineOpenfundsof \nZhejiang Chinese Medical University (No.\n752223A00201/005/019). The funders had no role \nin study design, data collection and analysis,\nRESEARCH ARTICLE\nIdentification and Verification of Potential \nTherapeutic Target Genes in Berberine-\nTreated Zucker Diabetic Fatty Rats through \nBioinformatics Analysis\nYang Sheng Wu1☯, Yi-Tao Chen2☯, Yu-Ting Bao1, Zhe-Ming Li1, Xiao-Jie Zhou1, Jia-Na He1, \nShi-Jie Dai1, Chang yu Li1*\n1 College of Pharmacy, Zhejiang Chinese Medical University, Hangzhou, Zhejiang, People’s Republic of \nChina, 2 College of Life Science, Zhejiang Chinese Medical University, Hangzhou, Zhejiang, People’s \nRepublic of China\n☯ These authors contributed equally to this work.\n* lcyzcmu@sina.com\nAbstract\nBackground\nBerberine is used to treat diabetes and dyslipidemia. However, the effect of berberine on \nspecific diabetes treatment targets is unknown. In the current study, we investigated the \neffect of berberine on the random plasma glucose, glycated hemoglobin (HbA1C), AST, \nALT, BUN and CREA levels of Zucker diabetic fatty (ZDF) rats, and we identified and veri-\nfied the importance of potential therapeutic target genes to provide molecular information for \nfurther investigation of the mechanisms underlying the anti-diabetic effects of berberine.\nMethods\nZDF rats were randomly divided into control (Con), diabetic (DM) and berberine-treated \n(300 mg-kg   , BBR) groups. After the ZDF rats were treated with BBR for 12 weeks, its \neffect on the random plasma glucose and HbA1C levels was evaluated. Aspartate amino-\ntransferase (AST), alanine aminotransferase (ALT), blood urea nitrogen (BUN), CREA and \nOGTT were measured from blood, respectively. The levels of gene expression in liver sam-\nples were analyzed using an Agilent rat gene expression 4x44K microarray. The differen-\ntially expressed genes (DEGs) were screened as those with log2 (Con vs DM) 2: 1 and log2 \n(BBR vs DM) 2: 1 expression levels, which were the genes with up-regulated expression, \nand those with log2 (Con vs DM) :s -1 and log2 (BBR vs DM) :s -1 expression levels, which \nwere the genes with down-regulated expression; the changes in gene expression were con-\nsidered significant at P<0.05. The functions of the DEGs were determined using gene ontol-\nogy (GO) and pathway analysis. Furthermore, a protein-protein interaction (PPI) network \nwas constructed using STRING and Cytoscape software. The expression levels of the key \nnode genes in the livers of the ZDF rats were also analyzed using qRT-PCR.\n";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 17))
                {
                    Assert.NotNull(page);
                    string expected = "PLOS ONE | DOI:10.1371/journal.pone.0166378  November 15, 2016 18 / 18\nIdentification and Verification of Potential Therapeutic Target Genes\n34. Pan D, Mao C, Zou T, Yao AY, Cooper MP, Boyartchuk V, et al. The histone demethylase Jhdm1a regu-\nlates hepatic gluconeogenesis. PLoS Genet. 2012; 8: e1002761. doi: 10.1371/journal.pgen.1002761 \nPMID: 22719268\n35. Pongratz RL, Kibbey RG, Shulman GI, Cline GW. Cytosolic and mitochondrial malic enzyme isoforms \ndifferentially control insulin secretion. J Biol Chem. 2007; 282: 200–207. doi: 10.1074/jbc.M602954200 \nPMID: 17102138\n36. Dentin R, Girard J, Postic C. Carbohydrate responsive element binding protein (ChREBP) and sterol \nregulatory element binding protein-1c (SREBP-1c): two key regulators of glucose metabolism and lipid \nsynthesis in liver. Biochimie. 2005; 87: 81–86. doi: 10.1016/j.biochi.2004.11.008 PMID: 15733741\n37. Hasan NM, Longacre MJ, Stoker SW, Kendrick MA, MacDonald MJ. Mitochondrial Malic Enzyme 3 is \nimportant for insulin secretion in pancreatic β-cells. Mol Endocrinol. 2015; 29: 396–410. doi: 10.1210/ \nme.2014-1249 PMID: 25594249.\n38. Head RA, Brown RM, Zolkipli Z, Shahdadpuri R, King MD, Clayton PT, et al. Clinical and genetic spec-\ntrum of pyruvate dehydrogenase deficiency: dihydrolipoamide acetyltransferase (E2) deficiency. Ann \nNeurol. 2005; 58: 234–241. doi: 10.1002/ana.20550 PMID: 16049940\n39. Ciara E,Rokicki D,Halat P, Karkucińska-Więckowska A,Piekutowska-Abramczuk D, Mayr J et al. Diffi-\nculties in recognition of pyruvate dehydrogenase complex deficiency on the basis of clinical and bio-\nchemical features. The role of next-generation sequencing. Mol Genet Metab Rep. 2016; 7: 70–76. doi: \n10.1016/j.ymgmr.2016.03.004  PMID:  27144126\n40. Goguet-Rubio P, Seyran B, Gayte L, Bernex F, Sutter A, Delpech H, et al. E4F1-mediated control of \npyruvate dehydrogenase activity is essential for skin homeostasis. Proc Natl Acad Sci U S A.2016; 11: \n11004–11009.  doi: 10.1073/pnas.1602751113\n41. Lee MH, Jeon YJ, Lee SM, Park MH, Jung SC, Kim YJ. Placental gene expression is related to glucose \nmetabolism and fetal cord blood levels of insulin and insulin-like growth factors in intrauterine growth \nrestriction. Early Hum Dev. 2010; 86: 45–50. doi: 10.1016/j.earlhumdev.2010.01.001 PMID: 20106611\n42. Tasdelen I, van Beekum O, Gorbenko O, Fleskens V, van den Broek NJ, Koppen A, et al. The serine/ \nthreonine phosphatase PPM1B (PP2Cβ) selectively modulates PPARγ activity. Biochem J. 2013; 451: \n45–53. doi: 10.1042/BJ20121113 PMID: 23320500\n";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTextTest077()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(21, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    string expected = "*For correspondence: rsun@ mednet.ucla.edu These authors contributed equally to this work Present address: Department of Integrative Structural and Computational Biology, The Scripps Research Institute, La Jolla, United States Competing interests: The authors declare that no competing interests exist. Funding: See page 18 Received: 18 April 2016 Accepted: 07 July 2016 Published: 08 July 2016 Reviewing editor: Richard A Neher, Max Planck Institute for Developmental Biology, Germany Copyright Wu et al. This article is distributed under the terms of the Creative Commons Attribution License, which permits unrestricted use and redistribution provided that the original author and source are credited. Adaptation in protein fitness landscapes is facilitated by indirect paths Nicholas C Wu1,2 , Lei Dai1,3 , C Anders Olson1, James O Lloyd-Smith3, Ren Sun1,2* 1Department of Molecular and Medical Pharmacology, University of California, Los Angeles, Los Angeles, United States; 2Molecular Biology Institute, University of California, Los Angeles, Los Angeles, United States; 3Department of Ecology and Evolutionary Biology, University of California, Los Angeles, Los Angeles, United States Abstract The structure of fitness landscapes is critical for understanding adaptive protein evolution. Previous empirical studies on fitness landscapes were confined to either the neighborhood around the wild type sequence, involving mostly single and double mutants, or a combinatorially complete subgraph involving only two amino acids at each site. In reality, the dimensionality of protein sequence space is higher (20L) and there may be higher-order interactions among more than two sites. Here we experimentally characterized the fitness landscape of four sites in protein GB1, containing 204 = 160,000 variants. We found that while reciprocal sign epistasis blocked many direct paths of adaptation, such evolutionary traps could be circumvented by indirect paths through genotype space involving gain and subsequent loss of mutations. These indirect paths alleviate the constraint on adaptive protein evolution, suggesting that the heretofore neglected dimensions of sequence space may change our views on how proteins evolve. DOI: 10.7554/eLife.16965.001 Introduction The fitness landscape is a fundamental concept in evolutionary biology (Kauffman and Levin, 1987; Poelwijk et al., 2007; Romero and Arnold, 2009; Hartl, 2014; Kondrashov and Kondrashov, 2015; de Visser and Krug, 2014). Large-scale datasets combined with quantitative analysis have successfully unraveled important features of empirical fitness landscapes (Kouyos et al., 2012; Barton et al., 2015; Szendro et al., 2013). Nevertheless, there is a huge gap between the limited throughput of fitness measurements (usually on the order of 102 variants) and the vast size of sequence space. Recently, the bottleneck in experimental throughput has been improved substan- tially by coupling saturation mutagenesis with deep sequencing (Fowler et al., 2010; Hietpas et al., 2011; Jacquier et al., 2013; Wu et al., 2014; Thyagarajan and Bloom, 2014; Qi et al., 2014; Stiffler et al., 2015), which opens up unprecedented opportunities to understand the structure of high-dimensional fitness landscapes (Jimenez et al., 2013; Pitt and Ferre-D’Amare, 2010; Payne and Wagner, 2014). Previous empirical studies on combinatorially complete fitness landscapes have been limited to subgraphs of the sequence space consisting of only two amino acids at each site (2L genotypes) (Weinreich et al., 2006; Lunzer et al., 2005; O’Maille et al., 2008; Lozovsky et al., 2009; Franke et al., 2011; Tan et al., 2011). Most studies of adaptive walks in these diallelic sequence spaces focused on direct paths where each mutational step reduces the Hamming distance from the starting point to the destination. However, it has also been shown that mutational reversions can occur during adaptive walks in diallelic sequence spaces such that adaptation proceeds via indirect paths (DePristo et al., 2007; Berestycki et al., 2014; Martinsson, 2015; Li, 2015; Palmer et al., Wu et al. eLife 2016;5:e16965. DOI: 10.7554/eLife.16965 1of21 RESEARCH ARTICLE ";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 20))
                {
                    Assert.NotNull(page);
                    string expected = "Sjobring U, Bjorck L, Kastern W. 1991. Streptococcal protein G. Gene structure and protein binding properties. The Journal of Biological Chemistry 266:399 405. Smith JM. 1970. Natural selection and the concept of a protein space. Nature 225:563 564. doi: 10.1038/ 225563a0 Stadler PF. 1996. Landscapes and their correlation functions. Journal of Mathematical Chemistry 20:1 45. doi: 10.1007/BF01165154 Stiffler MA, Hekstra DR, Ranganathan R. 2015. Evolvability as a function of purifying selection in TEM-1 b- lactamase. Cell 160:882 892. doi: 10.1016/j.cell.2015.01.035 Szendro IG, Schenk MF, Franke J, Krug J, de Visser JAGM. 2013. Quantitative analyses of empirical fitness landscapes. Journal of Statistical Mechanics: Theory and Experiment 2013:P01005. doi: 10.1088/1742-5468/ 2013/01/P01005 Tan L, Serene S, Chao HX, Gore J. 2011. Hidden Randomness between Fitness Landscapes Limits Reverse Evolution. Physical Review Letters 106:198102. doi: 10.1103/PhysRevLett.106.198102 Thyagarajan B, Bloom JD. 2014. The inherent mutational tolerance and antigenic evolvability of influenza hemagglutinin. eLife 3:e03300. doi: 10.7554/eLife.03300 Tufts DM, Natarajan C, Revsbech IG, Projecto-Garcia J, Hoffmann FG, Weber RE, Fago A, Moriyama H, Storz JF. 2015. Epistasis constrains mutational pathways of hemoglobin adaptation in high-altitude pikas. Molecular Biology and Evolution 32:287 298. doi: 10.1093/molbev/msu311 Wang Y, Arenas CD, Stoebel DM, Cooper TF. 2013. Genetic background affects epistatic interactions between two beneficial mutations. Biology Letters 9:20120328. doi: 10.1098/rsbl.2012.0328 Weinberger ED. 1991. Fourier and Taylor series on fitness landscapes. Biological Cybernetics 65:321 330. doi: 10.1007/BF00216965 Weinreich DM, Delaney NF, Depristo MA, Hartl DL. 2006. Darwinian evolution can follow only very few mutational paths to fitter proteins. Science 312:111 114. doi: 10.1126/science.1123539 Weinreich DM, Lan Y, Wylie CS, Heckendorn RB. 2013. Should evolutionary geneticists worry about higher-order epistasis? Current Opinion in Genetics & Development 23:700 707. doi: 10.1016/j.gde.2013.10.007 Weinreich DM, Watson RA, Chao L. 2005. Perspective: sign epistasis and genetic costraint on evolutionary trajectories. Evolution 59:1165 1174. doi: 10.1111/j.0014-3820.2005.tb01768.x Weissman DB, Desai MM, Fisher DS, Feldman MW. 2009. The rate at which asexual populations cross fitness valleys. Theoretical Population Biology 75:286 300. doi: 10.1016/j.tpb.2009.02.006 Weissman DB, Feldman MW, Fisher DS. 2010. The rate of fitness-valley crossing in sexual populations. Genetics 186:1389 1410. doi: 10.1534/genetics.110.123240 Wu K, Peng G, Wilken M, Geraghty RJ, Li F. 2012. Mechanisms of host receptor adaptation by severe acute respiratory syndrome coronavirus. Journal of Biological Chemistry 287:8904 8911. doi: 10.1074/jbc.M111. 325803 Wu NC, Young AP, Al-Mawsawi LQ, Olson CA, Feng J, Qi H, Chen SH, Lu IH, Lin CY, Chin RG, Luan HH, Nguyen N, Nelson SF, Li X, Wu TT, Sun R. 2014. High-throughput profiling of influenza A virus hemagglutinin gene at single-nucleotide resolution. Scientific Reports 4:4942. doi: 10.1038/srep04942 Zanini F, Brodin J, Thebo L, Lanz C, Bratt G, Albert J, Neher RA. 2015. Population genomics of intrapatient HIV- 1 evolution. eLife 4:e11282. doi: 10.7554/eLife.11282 Wu et al. eLife 2016;5:e16965. DOI: 10.7554/eLife.16965 21of21 Research article Genomics and evolutionary biology ";
                    string text = page.Text;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageAnnotationTest001()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(31, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    string expected = "";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 1))
                {
                    Assert.NotNull(page);
                    string expected = "";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageAnnotationTest004()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(4)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(10, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    string expected = "http://www.frontiersin.org/Neuroinformatics/editorialboard";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 1))
                {
                    Assert.NotNull(page);
                    string expected = "#10";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageAnnotationTest008()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(8)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(10, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    string expected = "http://crossmark.crossref.org/dialog/?doi=10.1186/s12864-016-2897-6&domain=pdf";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 1))
                {
                    Assert.NotNull(page);
                    string expected = "#10";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageAnnotationTest014()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(14)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(15, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    string expected = "#14";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 1))
                {
                    Assert.NotNull(page);
                    string expected = "#14";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageAnnotationTest021()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(21)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(11, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    string expected = "mailto:Ying-Cheng.Lai@asu.edu";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 1))
                {
                    Assert.NotNull(page);
                    string expected = "#10";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageAnnotationTest076()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(76)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(18, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    string expected = "";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 17))
                {
                    Assert.NotNull(page);
                    string expected = "";
                    string text = page.Annotation;
                    Assert.NotNull(text);
                    Assert.Equal<string>(expected, text);
                }
            }
        }
    }
}