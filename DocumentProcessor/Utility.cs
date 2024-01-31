using System;
using System.IO;

namespace documentprocessor
{
    public class Utility
    {
        readonly Factory factory;
        public Utility(Factory factory)
        {
            this.factory = factory;
        }

        public DateTime ToTimeOnly(DateTime fullDateTime)
        {
            DateTime dateTime = fullDateTime;
            if (dateTime != DateTime.MinValue)
            {
                dateTime = dateTime.AddDays(-1 * dateTime.DayOfYear + 1);
                dateTime = dateTime.AddYears(-1 * dateTime.Year + 1);
            }
            return dateTime;
        }

        public bool TimeBetween(DateTime start, DateTime end)
        {
            bool result = false;
            DateTime timeNow = ToTimeOnly(DateTime.Now);
            if (start > end)
            {
                end = end.AddDays(1);
                if (timeNow < start)
                    timeNow = timeNow.AddDays(1);
            }
            result = timeNow >= start && timeNow <= end;
            return result;
        }

        public string TempFileName(string nameStart)
        {
            return TempFileName(nameStart, 0);
        }

        public string TempFileName(string nameStart, int page)
        {
            string name = Path.Combine(factory.Config.TempDirectory, string.Concat(nameStart, "-", Guid.NewGuid().ToString()));
            if (page > 0)
            {
                 name += factory.Config.PageSuffix + page.ToString();
            }
            return name;
        }

        public string TempFileName(string nameStart, Guid guid, int page)
        {
            string name = Path.Combine(factory.Config.TempDirectory, string.Concat(nameStart, "-", guid.ToString()));
            if (page > 0)
            {
                name += factory.Config.PageSuffix + page.ToString();
            }
            return name;
        }

        public string TempFileNameChange(string inputFile, string name)
        {
            int dashPos = inputFile.IndexOf("-");
            string fileName = Path.GetFileNameWithoutExtension(string.Concat(name, inputFile.Remove(0, dashPos)));
            return Path.Combine(factory.Config.TempDirectory, fileName); 
        }
    }
}
