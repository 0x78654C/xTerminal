using Core;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.UI
{
    [SupportedOSPlatform("Windows")]
    public class UIManagement : ITerminalCommand
    {

        private string _regUI;
        List<string> _colors = new List<string>() { "darkred", "darkgreen", "darkyellow", "darkmagenta", "darkcyan", "darkgray", "darkblue", "red", "green", "yellow", "white", "magenta", "cyan", "gray", "blue" };
        List<string> _indicators = new List<string>() { ">", "->", "=>", "$", ">>" };
        private string _helpMessage = @"Usage of UI PS1 (Prompt string 1) command:
 ::Predefined Colors: darkred, darkgreen, darkyellow, darkmagenta, darkcyan, darkgray, darkblue,
                      red, green, yellow, white, magenta, cyan, gray, blue
 ::Predefined Indicators: >, ->, =>, $, >>

 -h : Displays this help message.
 -u : Enables or disables current user@machine information with a predefined color from list:
       Example1: ui -u -c <color> :e  -- enables information with a predefined color from list.
       Example2: ui -u -c <color> :d  -- disables information (need to specify color anyway).
 -i : Changes command indicator and sets a predefined color from list:
       Example1: ui -i -c <color> -s <indicator>  -- sets a custom indicator from predefined list with a predefined color from list. 
       Example2: ui -i -c <color> -s  -- sets default indicator($) with a predefined color from list. 
 -cd : Changes current directory with a predefined color from list:
       Example1: ui -cd <color> -- sets a predefined color from list to current directory path.
";
        public string Name => "ui";
        public void Execute(string arg)
        {
            if (arg.Length == 2)
            {
                FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                return;
            }
            
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
                else if (arg.ContainsText(":d"))
                {
                    SetUserColor(args.ParameterAfter("-c"), _regUI, "", false, Core.SystemTools.UI.Setting.UserInfo);
                    return;
                }
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

            // Store current directory setting.
            if (arg.ContainsText("-cd"))
            {
                SetUserColor(args.ParameterAfter("-cd"), _regUI, "$", true, Core.SystemTools.UI.Setting.CurrentDirectoy);
                return;
            }

            // Display help message.
            if (arg == $"{Name} -h")
            {
                Console.WriteLine(_helpMessage);
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
                else if (setting.ToString() == "UserInfo")
                {
                    if (enable)
                    {
                        colorSetting = $"{outColor};1|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.Indicator)}|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.CurrentDirectoy)}";
                        RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUI, colorSetting);
                        return;
                    }
                    colorSetting = $"{outColor};0|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.Indicator)}|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.CurrentDirectoy)}";
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUI, colorSetting);
                }
                else if (setting.ToString() == "CurrentDirectoy")
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
