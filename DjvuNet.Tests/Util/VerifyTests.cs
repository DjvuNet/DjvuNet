using Xunit;
using DjvuNet.Utilities;
using DjvuNet.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Utilities.Tests
{
    public class VerifyTests
    {
        [Fact()]
        public void SubsampleRangeTest001()
        {
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => Verify.SubsampleRange(0));
        }

        [Fact()]
        public void SubsampleRangeTest002()
        {
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => Verify.SubsampleRange(13));
        }

        [Fact()]
        public void SubsampleRangeTest003()
        {
            Verify.SubsampleRange(1);
            Verify.SubsampleRange(12);
        }
    }
}