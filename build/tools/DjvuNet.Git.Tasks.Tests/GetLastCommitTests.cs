using DjvuNet.Tests;
using Xunit;

namespace DjvuNet.Git.Tasks.Tests
{
    public class GetLastCommitTests
    {
        [Fact, Trait("Category", "Skip")]
        public void LastCommit()
        {
            var task = new GetLastCommit();
            task.BuildEngine = new Moqs.BuildEngineMoq();
            task.RepoRoot = Util.RepoRoot;

            var result = task.Execute();

            Assert.True(result);
            Assert.NotNull(task.CommitHash);
        }
    }
}
