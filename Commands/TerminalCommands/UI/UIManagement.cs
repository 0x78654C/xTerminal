using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace Commands.TerminalCommands.UI
{
    public class UIManagement : ITerminalCommand
    {

        private string _regUI;
        List<string> _colors = new List<string>() { "darkred", "darkgreen", "darkyellow", "darkmagenta", "darkcyan", "darkgray", "darkblue", "red", "green", "yellow", "white", "magenta", "cyan", "black", "gray", "blue" };
        List<string> _indicators = new List<string>() { ":", ">", "->", "=>", "$", ">>" };
        public string Name => "ui";
        public void Execute(string arg)
        {
            _regUI = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regUI);
            var args = arg.Split(' ');

            // Sore userinfo settings.
            if (arg.ContainsText("-u"))
            {
                if (arg.ContainsText(":e"))
                {
                    SetUserColor(args.ParameterAfter("-c"), _regUI, "", true, Core.SystemTools.UI.Setting.UserInfo);
                    return;
                }
                SetUserColor(args.ParameterAfter("-c"), _regUI, "", false, Core.SystemTools.UI.Setting.UserInfo);
                return;
            }

            // Store indicator setting.
            if (arg.ContainsText("-i"))
            {
                if (arg.ContainsText("-s"))
                {
                    SetUserColor(args.ParameterAfter("-c"), _regUI, args.ParameterAfter("-s"), true, Core.SystemTools.UI.Setting.Indicator);
                    return;
                }
                SetUserColor(args.ParameterAfter("-c"), _regUI, "$", true, Core.SystemTools.UI.Setting.Indicator);
                return;
            }

            //Store current directory setting.
            if (arg.ContainsText("-p"))
            {
                SetUserColor(args.ParameterAfter("-p"), _regUI, "$", true, Core.SystemTools.UI.Setting.CurrentDirectoy);
                return;
            }
        }

        private void SetUserColor(string color, string settings, string indicator, bool enable, Core.SystemTools.UI.Setting setting)
        {
            try
            {
                int indexColor = _colors.FindIndex(c => c == color.ToLower());
                string outColor = _colors[indexColor];
                string colorSetting;
                if (setting.ToString() == "Indicator")
                {
                    int indexIndicator = _indicators.FindIndex(i => i == indicator);
                    string indiOut = _indicators[indexIndicator];
                    colorSetting = $"{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.UserInfo)}|{outColor};{indiOut}|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.CurrentDirectoy)}";
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUI, colorSetting);
                }
                else if(setting.ToString() == "UserInfo")
                {
                    if (enable)
                    {
                        colorSetting = $"{outColor};1|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.Indicator)}|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.CurrentDirectoy)}";
                        RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUI, colorSetting);
                        return;
                    }
                    colorSetting = $"{outColor};0|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.Indicator)}|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.CurrentDirectoy)}";
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUI, colorSetting);
                }else if(setting.ToString() == "CurrentDirectoy")
                {
                    colorSetting = $"{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.UserInfo)}|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.Indicator)}|{outColor}";
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUI, colorSetting);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                FileSystem.ErrorWriteLine($"Color or indicator is not supported. Check command please!");
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString());
            }
        }
    }
}
