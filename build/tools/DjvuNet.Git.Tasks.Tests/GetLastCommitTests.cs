using System;
using System.Linq;
using System.Reflection;
using Xunit;
using DjvuNet.Git.Tasks;
using Microsoft.Build;
using System.IO;

namespace DjvuNet.Git.Tasks.Tests
{
    public class GetLastCommitTests
    {
        [Fact]
        public void LastCommit()
        {
            var task = new GetLastCommit();
            task.BuildEngine = new Moqs.BuildEngineMoq();
            var codeBasePath = Assembly.GetExecutingAssembly().CodeBase;
            codeBasePath = codeBasePath.Substring(0, codeBasePath.IndexOf("DjvuNet" + Path.DirectorySeparatorChar + "build") + 6);
            task.RepoRoot = codeBasePath;

            var result = task.Execute();

            Assert.True(result);
            Assert.NotNull(task.CommitHash);
        }
    }
}
