using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DjvuNet.Git.Tasks
{
    public class GetDateTime : Task
    {
        public string Format { get; set; }

        [Output]
        public string DateTimeStr { get; set; }

        [Output]
        public string Year { get; set; }

        [Output]
        public string Month { get; set; }

        [Output]
        public string Day { get; set; }

        [Output]
        public string DayOfWeek { get; set; }

        [Output]
        public string Hour { get; set; }

        [Output]
        public string Minute { get; set; }

        [Output]
        public string Second { get; set; }

        public override bool Execute()
        {
            if (String.IsNullOrWhiteSpace(Format))
            {
                Format = "yyyy-MM-dd HH:mm:ss";
            }

            try
            {
                System.DateTime now = System.DateTime.Now;
                DateTimeStr = now.ToString(Format);
                Year = now.Year.ToString();
                Month = now.Month.ToString();
                Day = now.Day.ToString();
                DayOfWeek = now.DayOfWeek.ToString();
                Hour = now.Hour.ToString();
                Minute = now.Minute.ToString();
                Second = now.Second.ToString();
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
