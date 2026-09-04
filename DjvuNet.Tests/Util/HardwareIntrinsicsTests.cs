using System.Text.RegularExpressions;
using Xunit;
using DjvuNet.Shared.Tests;

namespace DjvuNet.Tests
{
    public class HardwareIntrinsicsTests
    {
        [Fact]
        public void GetHardwareVectorSize_FormatIsCorrect()
        {
            string size = HardwareIntrinsics.GetHardwareVectorSize();
            Assert.NotNull(size);
            
            if (size != string.Empty)
            {
                Assert.Matches(@"^MaxVectorSize=(64|128|256|512)$", size);
            }
        }

        [Fact]
        public void GetShortInfo_FormatIsCorrect()
        {
            string info = HardwareIntrinsics.GetShortInfo();
            Assert.NotNull(info);
            
            Assert.True(
                Regex.IsMatch(info, @"^x86-64-v[1-4]$") || 
                info == "armv8.0-a" || 
                Regex.IsMatch(info, @"^MaxVectorSize=(64|128|256|512)$") ||
                info == string.Empty, 
                $"GetShortInfo returned unexpected format: '{info}'");
        }

        [Theory]
        [InlineData(Platform.X64)]
        [InlineData(Platform.X86)]
        [InlineData(Platform.Arm64)]
        public void GetFullInfo_ReturnsCommaSeparatedList(Platform platform)
        {
            string info = HardwareIntrinsics.GetFullInfo(platform);
            Assert.NotNull(info);
            
            if (!string.IsNullOrEmpty(info))
            {
                Assert.DoesNotContain(";", info);
                Assert.DoesNotContain("\n", info);
                
                string[] parts = info.Split(',');
                Assert.All(parts, part => Assert.False(string.IsNullOrWhiteSpace(part)));
            }
        }
        
        [Fact]
        public void GetFullInfo_InvalidPlatform_ReturnsEmpty()
        {
            string info = HardwareIntrinsics.GetFullInfo(Platform.AnyCpu);
            Assert.Equal(string.Empty, info);
        }
    }
}
