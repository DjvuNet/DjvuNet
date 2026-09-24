using System;
using System.Linq;
using LibGit2Sharp;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DjvuNet.Build.Tasks
{
    public class BuildMajorVersion : Task
    {
        [Required]
        public string MajorMinorVersion { get; set; }

        public string RepoRoot { get; set; }

        [Output]
        public string Version { get; set; }

        [Output]
        public string FullVersion { get; set; }

        public override bool Execute()
        {
            TaskLogger.Current = this.Log;
            DjvuNetBuildEventSource.Log.BuildMajorVersionStart();
            try
            {
                if (!System.Version.TryParse(MajorMinorVersion, out System.Version baseVersion))
                {
                    Log.LogError($"{nameof(MajorMinorVersion)} has invalid version format.");
                    return false;
                }

                DateTime commitDate = DateTime.UtcNow;
                int commitOrderToday = 0;
                string hashSuffix = "";

                if (!string.IsNullOrWhiteSpace(RepoRoot))
                {
                    try
                    {
                        DjvuNetBuildEventSource.Log.InstantiateRepositoryStart();
                        using (var repo = new Repository(RepoRoot))
                        {
                            DjvuNetBuildEventSource.Log.InstantiateRepositoryStop();
                            var headCommit = repo.Head.Tip;
                            if (headCommit != null)
                            {
                                commitDate = headCommit.Author.When.UtcDateTime;

                                var dateToMatch = commitDate.Date;
                                DjvuNetBuildEventSource.Log.IterateCommitsStart();
                                foreach (var commit in repo.Commits)
                                {
                                    if (commit.Author.When.UtcDateTime.Date == dateToMatch)
                                        commitOrderToday++;
                                    else
                                        break;
                                }
                                DjvuNetBuildEventSource.Log.IterateCommitsStop(commitOrderToday);

                                commitOrderToday--; // 0-based index
                                if (commitOrderToday < 0) commitOrderToday = 0;

                                string shortHash = headCommit.Sha.Substring(0, 7);
                                DjvuNetBuildEventSource.Log.RetrieveStatusStart();
                                var statusOpts = new StatusOptions { IncludeIgnored = false };
                                RepositoryStatus status = repo.RetrieveStatus(statusOpts);
                                bool isDirty = status.IsDirty;
                                DjvuNetBuildEventSource.Log.RetrieveStatusStop(isDirty);

                                hashSuffix = isDirty ? $"{shortHash}-dev" : $"{shortHash}";
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore git errors, fallback to defaults
                    }
                }

                // yy * 1000 + DayOfYear
                int majorBuildVersion = (commitDate.Year % 100) * 1000 + commitDate.DayOfYear;

                int buildRevision = commitOrderToday;

                var intVersion = new System.Version(baseVersion.Major, baseVersion.Minor, majorBuildVersion, buildRevision);
                Version = intVersion.ToString();

                FullVersion = string.IsNullOrEmpty(hashSuffix) ? Version : $"{Version}-{hashSuffix}";
            }
            catch (Exception ex)
            {
                Log.LogError(ex.ToString());
                Log.LogErrorFromException(ex, true);
            }

            DjvuNetBuildEventSource.Log.BuildMajorVersionStop();
            return !Log.HasLoggedErrors;
        }
    }
}
