using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DataChunks;
using DjvuNet.Errors;
using Xunit;

namespace DjvuNet.DataChunks.Tests
{
    public class AnnotationTests
    {
        [Fact()]
        public void AnnotationTest001()
        {
            string test = null;
            Assert.Throws<DjvuArgumentNullException>("text", () => new Annotation(test));
        }

        [Fact()]
        public void AnnotationTest002()
        {
            string test = String.Empty;
            Assert.Throws<DjvuArgumentException>("text", () => new Annotation(test));
        }

        [Fact()]
        public void AnnotationTest003()
        {
            string test = "             ";
            Assert.Throws<DjvuArgumentException>("text", () => new Annotation(test));
        }

        [Fact()]
        public void AnnotationTest004()
        {
            string test = "This is an annotation test";
            string[] testSplit = test.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            Annotation anno = new Annotation(test);
            Assert.Equal<string>("This", anno.Name);
            Assert.NotNull(anno.Parameters);
            Assert.Equal<int>(4, anno.Parameters.Length);
            for (int i = 0; i < anno.Parameters.Length; i++)
                Assert.Equal<string>(testSplit[i + 1], anno.Parameters[i].Name);
        }

        [Fact()]
        public void AnnotationTest005()
        {
            string test = "(This (is (an) (annotation)) (test))";
            string[] testSplit = test.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            Annotation anno = new Annotation(test);
            Assert.Equal<string>("This", anno.Name);
            Assert.NotNull(anno.Parameters);
            Assert.Equal<int>(2, anno.Parameters.Length);
            Assert.Equal<string>("is", anno.Parameters[0].Name);
            Assert.Equal<int>(2, anno.Parameters[0].Parameters.Length);
            Assert.Equal<string>("an", anno.Parameters[0].Parameters[0].Name);
            Assert.Equal<string>("annotation", anno.Parameters[0].Parameters[1].Name);
            Assert.Equal<string>("test", anno.Parameters[1].Name);
        }

        [Fact()]
        public void AnnotationTest006()
        {
            string test = "(This (is (an) (\"annotation\")) (test))";
            string[] testSplit = test.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            Annotation anno = new Annotation(test);
            Assert.Equal<string>("This", anno.Name);
            Assert.NotNull(anno.Parameters);
            Assert.Equal<int>(2, anno.Parameters.Length);
            Assert.Equal<string>("is", anno.Parameters[0].Name);
            Assert.Equal<int>(2, anno.Parameters[0].Parameters.Length);
            Assert.Equal<string>("an", anno.Parameters[0].Parameters[0].Name);
            Assert.Equal<string>("\"annotation\"", anno.Parameters[0].Parameters[1].Name);
            Assert.Equal<string>("test", anno.Parameters[1].Name);
        }

        [Fact()]
        public void AnnotationTest007()
        {
            string test = "(This (is \"(an) (\"annotation\")\") (test))";
            string[] testSplit = test.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            Annotation anno = new Annotation(test);
            Assert.Equal<string>("This", anno.Name);
            Assert.NotNull(anno.Parameters);
            Assert.Equal<int>(2, anno.Parameters.Length);
            Assert.Equal<string>("is", anno.Parameters[0].Name);
            Assert.Equal<int>(2, anno.Parameters[0].Parameters.Length);
            Assert.Equal<string>("an", anno.Parameters[0].Parameters[0].Name);
            Assert.Equal<string>("\"annotation\"", anno.Parameters[0].Parameters[1].Name);
            Assert.Equal<string>("test", anno.Parameters[1].Name);
        }


        [Fact()]
        public void BreakIntoAnnotationPiecesTest001()
        {
            string test = "This (is (an) (annotation)) (test)";
            string[] pieces = Annotation.BreakIntoAnnotationPieces(test);
            Assert.Equal<int>(2, pieces.Length);
            Assert.Equal<string>("(is (an) (annotation))", pieces[0]);
            Assert.Equal<string>("(test)", pieces[1]);
        }

        [Fact()]
        public void BreakIntoAnnotationPiecesTest002()
        {
            string test = "This (is (an) (annotation)) (\"test\")";
            string[] pieces = Annotation.BreakIntoAnnotationPieces(test);
            Assert.Equal<int>(2, pieces.Length);
            Assert.Equal<string>("(is (an) (annotation))", pieces[0]);
            Assert.Equal<string>("(\"test\")", pieces[1]);
        }

        [Fact()]
        public void ToStringTest()
        {
            string test = "(This (is (an) (annotation)) (test))";
            string[] testSplit = test.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            Annotation anno = new Annotation(test);
            string result = anno.ToString();
            Assert.Contains("(This(is(an)", result);
            Assert.Contains(" (annotation)) (test))", result);
            Assert.StartsWith("(This", result);
            Assert.EndsWith("test))", result);
        }
    }
}