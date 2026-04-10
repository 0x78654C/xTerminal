using System;
using System.Globalization;
using System.Runtime.Versioning;

namespace Core.SystemTools
{
    /*
     Calendar class.
     */

    [SupportedOSPlatform("windows")]
    public class CalendarX
    {
        private int Year { get; set; }
        private int Month { get; set; }

        private int[,] calendar = new int[6, 7];
        private DateTime date;
        bool IsCurrentDay { get; set; }
        private int DayNow = DateTime.Today.Day;
        private int MonthNow = DateTime.Today.Month;
        private int YearNow = DateTime.Today.Year;

        /// <summary>
        /// Set calendar
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        public CalendarX(int year, int month)
        {
            Year = year;
            Month = month;
        }

        /// <summary>
        /// Setr calsendar
        /// </summary>
        /// <param name="isCurrentDay"></param>
        public CalendarX(bool isCurrentDay)
        {
            IsCurrentDay = isCurrentDay;
        }

        /// <summary>
        /// Dray Year, month days.
        /// </summary>
        /// <param name="isCurrentDay"></param>
        private void DrawHeader(bool isCurrentDay)
        {
            if (isCurrentDay)
                Console.WriteLine(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(MonthNow) + " " + YearNow);
            else
                Console.WriteLine(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month) + " " + Year);
            Console.WriteLine("Mo Tu We Th Fr Sa Su");
        }


        /// <summary>
        /// Fil days in calendar
        /// </summary>
        /// <param name="isCurrentDay"></param>
        private void FillCalendar(bool isCurrentDay)
        {
            // clear previous values
            Array.Clear(calendar, 0, calendar.Length);

            int y = isCurrentDay ? YearNow : Year;
            int m = isCurrentDay ? MonthNow : Month;

            DrawHeader(isCurrentDay);

            int days = DateTime.DaysInMonth(y, m);
            date = new DateTime(y, m, 1);

            // Monday-first offset (Mon=0 ... Sun=6)
            int firstDayOffset = ((int)date.DayOfWeek + 6) % 7;

            int day = 1;
            for (int i = 0; i < calendar.GetLength(0) && day <= days; i++)
            {
                for (int j = 0; j < calendar.GetLength(1) && day <= days; j++)
                {
                    if (i == 0 && j < firstDayOffset)
                        calendar[i, j] = 0;
                    else
                        calendar[i, j] = day++;
                }
            }
        }

        /// <summary>
        /// Display calendar.
        /// </summary>
        public void ShowCalandar()
        {
            FillCalendar(IsCurrentDay);
            for (int i = 0; i < calendar.GetLength(0); i++)
            {
                for (int j = 0; j < calendar.GetLength(1); j++)
                {
                    if (calendar[i, j] > 0)
                    {
                        if (calendar[i, j] < 10)
                        {
                            var day = calendar[i, j];
                            if (day == 0) day = 1;
                            if (day == DayNow && IsCurrentDay)
                                FileSystem.ColorConsoleText(ConsoleColor.Green, " " + day + " ");
                            else
                                Console.Write(" " + day + " ");
                        }
                        else
                        {
                            var day = calendar[i, j];
                            if (day == 0) day = 1;
                            if (day == DayNow && IsCurrentDay)
                                FileSystem.ColorConsoleText(ConsoleColor.Green, day + " ");
                            else
                                Console.Write(day + " ");
                        }
                    }
                    else
                        Console.Write("   ");
                }
                Console.WriteLine("");
            }
        }
    }
}
