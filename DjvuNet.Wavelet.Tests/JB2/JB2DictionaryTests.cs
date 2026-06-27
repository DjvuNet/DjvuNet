using Xunit;
using DjvuNet.JB2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests;
using DjvuNet.Errors;

namespace DjvuNet.JB2.Tests
{
    public class JB2DictionaryTests
    {
        public static IEnumerable<object[]> JB2DictionaryTestData => Util.GetJB2DictionaryTestData(
            skipDocs: new int[] { },
            skipChunks: new string[] { }
        );

        [Theory]
        [MemberData(nameof(JB2DictionaryTestData))]
        [InlineData("testE002_001.djbz")]
        [InlineData("testE003_001.djbz")]
        [InlineData("testE004_001.djbz")]
        [InlineData("testE005_001.djbz")]
        public void DecodeDjbzTest(string djbzFileName)
        {
            byte[] djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName));

            JB2Dictionary managedDict = new JB2Dictionary();

            using (var ms = new MemoryStream(djbzPayload))
            using (var reader = new DjvuReader(ms))
            {
                managedDict.Decode(reader);
            }

            // Console.WriteLine($"Decoded {djbzFileName} with djbz chunk: dictionary containing {managedDict.ShapeCount} shapes.");

            Assert.True(managedDict.ShapeCount > 0, "Managed dictionary decoded 0 shapes.");
        }

        [Fact]
        public void AddShape_Success()
        {
            var dict = new JB2Dictionary();
            var shape = new JB2Shape();
            shape.Init(-1); // parent = -1

            int result = dict.AddShape(shape);

            Assert.Equal(0, result);
            Assert.Equal(1, dict.ShapeCount);
        }

        [Fact]
        public void AddShape_InvalidParent_ThrowsDjvuArgumentException()
        {
            var dict = new JB2Dictionary();
            var invalidShape = new JB2Shape();
            invalidShape.Init(1); // ShapeCount is 0, so 1 >= 0
            
            var ex = Assert.Throws<DjvuArgumentException>(() => dict.AddShape(invalidShape));
            Assert.Contains("JB2 decoding failed: Illegal parent shape number in JB2Shape.", ex.Message);
            Assert.Equal("jb2Shape", ex.ParamName);
        }

        [Fact]
        public void ImageData_ReturnsFalse()
        {
            var dict = new JB2Dictionary();
            Assert.False(dict.ImageData);
        }

        [Fact]
        public void GetShape_Local_Success()
        {
            var dict = new JB2Dictionary();
            var shape = new JB2Shape();
            shape.Init(-1);
            dict.AddShape(shape);

            var retrievedShape = dict.GetShape(0);
            Assert.Same(shape, retrievedShape);
        }

        [Fact]
        public void GetShape_Inherited_Success()
        {
            var inheritedDict = new JB2Dictionary();
            var inheritedShape = new JB2Shape();
            inheritedShape.Init(-1);
            inheritedDict.AddShape(inheritedShape);

            var parentDict = new JB2Dictionary();
            parentDict.SetInheritedDict(inheritedDict, true);

            var retrievedInherited = parentDict.GetShape(0);
            Assert.Same(inheritedShape, retrievedInherited);
        }

        [Fact]
        public void GetShape_InvalidIndex_ThrowsDjvuArgumentOutOfRangeException()
        {
            var dict = new JB2Dictionary();
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => dict.GetShape(1));
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => dict.GetShape(-1));
        }

        [Fact]
        public void Init_Success()
        {
            var dict = new JB2Dictionary();
            var shape = new JB2Shape();
            shape.Init(-1);
            dict.AddShape(shape);
            dict.SetInheritedDict(new JB2Dictionary(), true);

            Assert.Equal(1, dict.ShapeCount);
            Assert.NotNull(dict.InheritedDictionary);

            dict.Init();

            Assert.Equal(0, dict.ShapeCount);
            Assert.Null(dict.InheritedDictionary);
        }

        [Fact]
        public void SetInheritedDict_Null_ClearsDictionary()
        {
            var dict = new JB2Dictionary();
            dict.SetInheritedDict(new JB2Dictionary(), true);
            dict.SetInheritedDict(null, false);
            
            Assert.Null(dict.InheritedDictionary);
            Assert.Equal(0, dict.InheritedShapes);
        }

        [Fact]
        public void SetInheritedDict_DirtyDictionary_ThrowsDjvuInvalidOperationException()
        {
            var dict = new JB2Dictionary();
            var shape = new JB2Shape();
            shape.Init(-1);
            dict.AddShape(shape);
            
            var ex = Assert.Throws<DjvuInvalidOperationException>(() => dict.SetInheritedDict(new JB2Dictionary(), false));
            Assert.Contains("JB2 decoding failed: Cannot set dictionary after adding shapes.", ex.Message);
        }

        [Fact]
        public void SetInheritedDict_AlreadySet_ThrowsDjvuInvalidOperationException()
        {
            var dict = new JB2Dictionary();
            dict.SetInheritedDict(new JB2Dictionary(), true);
            
            var ex = Assert.Throws<DjvuInvalidOperationException>(() => dict.SetInheritedDict(new JB2Dictionary(), false));
            Assert.Contains("JB2 decoding failed: Cannot change dictionary once set.", ex.Message);
        }

        [Fact]
        public void SetInheritedDict_Force_OverridesConstraints()
        {
            var dict = new JB2Dictionary();
            dict.SetInheritedDict(new JB2Dictionary(), true);
            
            var inherited = new JB2Dictionary();
            var shape = new JB2Shape();
            shape.Init(-1);
            inherited.AddShape(shape);
            
            dict.SetInheritedDict(inherited, true); // force=true
            
            Assert.Same(inherited, dict.InheritedDictionary);
            Assert.Equal(1, dict.InheritedShapes);
        }

        [Fact]
        public void InheritedDictionary_PropertySetter_Success()
        {
            var dict = new JB2Dictionary();
            var inherited = new JB2Dictionary();
            
            dict.InheritedDictionary = inherited;
            Assert.Same(inherited, dict.InheritedDictionary);
        }
    }
}
