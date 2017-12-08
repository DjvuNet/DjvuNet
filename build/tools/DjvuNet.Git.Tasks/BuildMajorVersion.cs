using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DjvuNet.Git.Tasks
{
    public class BuildMajorVersion : Task
    {

        [Required]
        public string MajorMinorVersion { get; set; }

        [Output]
        public string Version { get; set; }

        public override bool Execute()
        {
            try
            {
                if (!System.Version.TryParse(MajorMinorVersion, out Version baseVersion))
                    Log.LogError($"{nameof(MajorMinorVersion)} has invalid version formt.");

                DateTime now = DateTime.UtcNow;
                DateTime reference = new DateTime(2017, 1, 1);
                TimeSpan spanFromEpoch = now.Subtract(reference);

                int majorBuildVersion = Math.DivRem((int)spanFromEpoch.TotalDays, 28, out int reminder);
                majorBuildVersion *= 1000;
                majorBuildVersion += (int) Math.Round((double) ((reminder * 24 + spanFromEpoch.Hours) * 1.488095238d), 0);

                // Calculate Build Revision based on part of hour expressed in
                // seconds x 0.277777778 -coefficient normalizes it to 1 000
                TimeSpan lastHour = new TimeSpan((int)spanFromEpoch.TotalHours, 0, 0);
                lastHour = spanFromEpoch - lastHour;
                int buildRevision = (int) Math.Round((double)(lastHour.TotalSeconds * 0.277777778d), 0);

                var intVersion = new Version(baseVersion.Major, baseVersion.Minor, majorBuildVersion, buildRevision);
                Version = intVersion.ToString();
            }
            catch (Exception ex)
            {
                Log.LogError(ex.ToString());
                Log.LogErrorFromException(ex, true);
            }

            return !Log.HasLoggedErrors;
        }
    }
}
