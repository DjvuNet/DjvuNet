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
                DateTime now = DateTime.Now;
                DateTime reference = new DateTime(2014, 1, 23);
                TimeSpan span = now.Subtract(reference);
                int reminder;
                Version = Math.DivRem((int)span.TotalDays, 7, out reminder);
                Version *= 100;
                int reminderHour;
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
