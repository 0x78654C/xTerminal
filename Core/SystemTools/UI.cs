using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.SystemTools
{
    public static class UI
    {
        public static string SanitizeSettings(string settings, Setting setting)
        {
            var settingsList = settings.Split('|');
            string settingParsed = "";
            switch (setting)
            {
                case Setting.UserInfo:
                    settingParsed = settingsList[0];
                    break;
                case Setting.Indicator:
                    settingParsed = settingsList[1];
                    break;
                case Setting.CurrentDirectoy:
                    settingParsed = settingsList[2];
                    break;
            }
            return settingParsed;
        }

        public enum Setting
        {
            UserInfo,
            Indicator,
            CurrentDirectoy
        }

        public static ConsoleColor SetConsoleColor(string color)
        {
            return Enum.TryParse<ConsoleColor>(color, true, out var c) ? c : ConsoleColor.Green;
        }
    }
}
