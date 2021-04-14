#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
#endregion Using Directives

namespace TraverseApi
{
    [Serializable]
    public class ApiErrorLogInfo
    {
        #region Private Methods
        private void SetDateInfo()
        {
            int year = 0;
            int week = 0;

            if (int.TryParse(file_name.Substring(9, 4), out year) && int.TryParse(file_name.Substring(14), out week))
                SetDateInfo(year, week);
        }

        private void SetDateInfo(int year, int weekNum)
        {
            DateTime firstDayOfYear = new DateTime(year, 1, 1);
            int offset = (int)firstDayOfYear.DayOfWeek;

            int weekJan = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(firstDayOfYear,
                CultureInfo.InvariantCulture.DateTimeFormat.CalendarWeekRule,
                CultureInfo.InvariantCulture.DateTimeFormat.FirstDayOfWeek);

            int weeks = weekNum;
            if (weekJan <= 1)
                weeks--;

            date_from = firstDayOfYear.AddDays(weeks * 7 - offset);
            date_thru = date_from.AddDays(6);
        }
        #endregion Private Methods

        #region Static Methods
        public async static Task<List<ApiErrorLogInfo>> GetLogErrorDirectory(string dir)
        {
            List<ApiErrorLogInfo> list = new List<ApiErrorLogInfo>();
            await Task.Run(() =>
            {
                if (Directory.Exists(dir))
                {
                    foreach (string path in Directory.EnumerateFiles(dir))
                    {
                        var info = new ApiErrorLogInfo();
                        info.file_name = Path.GetFileNameWithoutExtension(path);

                        var errorList = ApiErrorHandler.ReadErrorFile(path);
                        info.log_entries_in_file = errorList.Count;
                        info.SetDateInfo();

                        list.Add(info);
                    }
                }
            });

            return list;
        }
        #endregion Static Methods

        #region Properties
        public string file_name { get; private set; }

        public int log_entries_in_file { get; private set; }

        public DateTime date_from { get; private set; }

        public DateTime date_thru { get; private set; }
        #endregion Properties
    }
}