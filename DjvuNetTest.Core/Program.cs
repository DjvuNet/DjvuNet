using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using LibGit2Sharp;

namespace DjvuNetTest.Core
{
    class Program
    {
        public static void Main(string[] args)
        {
            Repository repo = new Repository(Util.RepoRoot);
            Commit cmt = repo.Commits.FirstOrDefault<Commit>();
            int testsToSkip = 5;
            int testsNumber = 50;

            string[] docs = new string[]
            {
                DjvuNet.Tests.Util.GetTestFilePath(1),
                DjvuNet.Tests.Util.GetTestFilePath(3),
                DjvuNet.Tests.Util.GetTestFilePath(32),
                DjvuNet.Tests.Util.GetTestFilePath(33),
                DjvuNet.Tests.Util.GetTestFilePath(35),
                DjvuNet.Tests.Util.GetTestFilePath(42),
                DjvuNet.Tests.Util.GetTestFilePath(43),
                DjvuNet.Tests.Util.GetTestFilePath(44),
                DjvuNet.Tests.Util.GetTestFilePath(45),
                DjvuNet.Tests.Util.GetTestFilePath(46),
                DjvuNet.Tests.Util.GetTestFilePath(69),
                DjvuNet.Tests.Util.GetTestFilePath(72),
            };

            long[] elapsed = new long[docs.Length * 3];

            Console.WriteLine($"\n\nPerformance tests for DjvuNet library                  ");
            Console.WriteLine($"                                                           ");
            Console.WriteLine($"Test Configuration:                                        ");
            Console.WriteLine($"Git Commit -          \t{cmt.Sha}");
            Console.WriteLine($"Test Documents -      \t{docs.Length}");
            Console.WriteLine($"                                                           ");
            for (int d = 0; d < docs.Length; d++)
                Console.WriteLine($"Document\t\t{(d + 1)}\t{Path.GetFileName(docs[d])}");
            Console.WriteLine($"                                                           ");
            Console.WriteLine($"Measured Tests -      \t{testsNumber}");
            Console.WriteLine($"Warm up Tests -       \t{testsToSkip}");
            Console.WriteLine($"                                                           ");
            Console.WriteLine($"                                                           ");

            int i = 0;

            for (int docNo = 0; docNo < docs.Length; docNo++)
            {

                Console.WriteLine($"Document {docNo + 1} tests:");
                i = 0;

                for (; i < (testsNumber + testsToSkip); i++)
                {
                    int topPos = Console.CursorTop;
                    Console.WriteLine($"Test cycle: {i + 1}");

                    TestPage(testsToSkip, docs, elapsed, i, docNo);

                    Console.SetCursorPosition(0, topPos);
                }

                i -= testsToSkip;
                Console.WriteLine("---------------------------------------------------------------------------------------------");
                Console.WriteLine("                                                                                             ");
                Console.WriteLine($"Total and average execution times in ticks after {i + testsToSkip} tests                             ");
                Console.WriteLine($"First {testsToSkip} results are discarded as a cold run (test harness warmup)                                    ");
                Console.WriteLine("                                                                                             ");
                Console.WriteLine($"Document opened in      \t{elapsed[docNo * 3 + 0]:000 000 000 000}\t\taverage {((double)elapsed[docNo * 3 + 0] / i):000 000 000 000.00} ");
                Console.WriteLine($"Image 1 - 1 generated in\t{elapsed[docNo * 3 + 1]:000 000 000 000}\t\taverage {((double)elapsed[docNo * 3 + 1] / i):000 000 000 000.00} ");
                Console.WriteLine($"Image 1 - 2 generated in\t{elapsed[docNo * 3 + 2]:000 000 000 000}\t\taverage {((double)elapsed[docNo * 3 + 2] / i):000 000 000 000.00} ");
                Console.WriteLine("                                                                                             ");
                Console.WriteLine("----------------------------------------------------------------------------------------------");
                Console.WriteLine();
            }

        }

        private static void TestPage(int testsToSkip, string[] docs, long[] elapsed, int i, int docNo)
        {
            Stopwatch watch = Stopwatch.StartNew();
            using (DjvuDocument doc = new DjvuDocument(docs[docNo]))
            {
                watch.Stop();
                Console.WriteLine($"Document {doc.Name} opened in\t{watch.ElapsedTicks:000 000 000 000}");
                if (i + 1 > testsToSkip)
                    elapsed[docNo * 3 + 0] += watch.ElapsedTicks;

                string fileName = Path.GetFileNameWithoutExtension(doc.Location);
                fileName = Path.Combine(Util.ArtifactsDataPath, "dumps", fileName);
                var page = doc.Pages[0];

                BenchmarkBuildPageImageCall(testsToSkip, elapsed, i, docNo, watch, fileName, page, 1);

                page.IsInverted = true;
                BenchmarkBuildPageImageCall(testsToSkip, elapsed, i, docNo, watch, fileName, page, 2);
            }
        }

        private static void BenchmarkBuildPageImageCall(int testsToSkip, long[] elapsed, int i, int docNo,
            Stopwatch watch, string fileName, IDjvuPage page, int imageNo = 1)
        {
            watch.Restart();
            using (System.Drawing.Bitmap bmp = ((DjvuPage)page).BuildPageImage())
            {
                watch.Stop();
                Console.WriteLine($"Image {docNo + 1} - {imageNo} generated in\t{watch.ElapsedTicks:000 000 000 000}");
                if (i + 1 > testsToSkip)
                    elapsed[docNo * 3 + imageNo] += watch.ElapsedTicks;

                //bmp.Save(fileName + $"Mn_{imageNo}.png", ImageFormat.Png);
            }
        }
    }
}

//internal class AssemblyData
//{
//    public const string Name = "DjvuNet.Tests";
//}
