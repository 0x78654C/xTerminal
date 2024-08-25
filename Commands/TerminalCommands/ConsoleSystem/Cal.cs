using System;
using Core;
using System.Runtime.Versioning;
using Calend = Core.SystemTools.CalendarX;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("windows")]
    public class Cal: ITerminalCommand
    {
        public string Name => "cal";
        private static string s_helpMessage = @"Usage of cal command:
               cal : Display current date calendar.
    cal month-year : Display calendar of a specific year and month. 
                     Example : cal 2-2023 
            cal -h : Display this message.
";
        public void Execute(string arg)
        {
            try
            {
                if (arg.Trim() == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                if (arg.Trim() == Name)
                {
                    var calendar = new Calend(true);
                    calendar.ShowCalandar();
                    return;
                }

                var date = arg.Replace($"{Name}", "").Trim().Split('-');
                var month =int.Parse(date[0]);
                var year = int.Parse(date[1]);

                var calendarOtherYear = new Calend(year, month);
                calendarOtherYear.ShowCalandar();
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
            }
        }
    }
}
