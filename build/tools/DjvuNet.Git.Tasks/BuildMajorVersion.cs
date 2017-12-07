using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DjvuNet.Git.Tasks
{
    public class BuildMajorVersion : Task
    {
        [Output]
        public int Version { get; set; }

        public override bool Execute()
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                DateTime reference = new DateTime(2017, 1, 1);
                TimeSpan span = now.Subtract(reference);
                int reminder;
                Version = Math.DivRem((int)span.TotalDays, 28, out reminder);
                Version *= 1000;
                int reminderHour;
                int value = (int) Math.Round((double) reminder * 24.0 * 1.488095238d, 0);
                int hoursMod = Math.DivRem(now.Hour , 8, out reminderHour);
                reminder = (reminder * 3 + hoursMod) * 4 + reminderHour;
                Version += reminder;
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
