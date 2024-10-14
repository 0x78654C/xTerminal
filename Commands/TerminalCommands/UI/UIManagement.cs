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

 -h  : Displays this help message.
 -u  : Enables or disables current user@machine information with a predefined color from list:
        Example1: ui -u -c <color> :e  -- enables information with a predefined color from list.
        Example2: ui -u -c <color> :d  -- disables information (need to specify color anyway).
 -i  : Changes command indicator and sets a predefined color from list:
        Example1: ui -i -c <color> -s <indicator>  -- sets a custom indicator from predefined list with a predefined color from list. 
        Example2: ui -i -c <color> -s  -- sets default indicator($) with a predefined color from list. 
 -cd : Changes current directory with a predefined color from list:
        Example1: ui -cd <color> -- sets a predefined color from list to current directory path.
        Example2: ui -cd :e -- enable display current working directory in console.
        Example3: ui -cd :d -- disable display current working directory in console.
 -r  : Reset console foreground and background color to default.
 -p  : Change color of success output data. Default is gray.
        Exanmple: ui -p red
";
        public string Name => "ui";
        public void Execute(string arg)
        {
            if (arg == Name)
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
                var argTrimed = arg.Trim();
                if (argTrimed.EndsWith(" :e")) // Enable current directory display.
                {
                    SetUserColor(args.ParameterAfter("-cd"), _regUI, "$", true, Core.SystemTools.UI.Setting.CurrentDirectoy, true);
                    return;
                }
                if (argTrimed.EndsWith(" :d")) // Disable current directory display.
                {
                    SetUserColor(args.ParameterAfter("-cd"), _regUI, "$", true, Core.SystemTools.UI.Setting.CurrentDirectoy, false);
                    return;
                }

                SetUserColor(args.ParameterAfter("-cd"), _regUI, "$", true, Core.SystemTools.UI.Setting.CurrentDirectoy);
                return;
            }

            // Reset console foreground and background color to default
            if (arg.ContainsText("-r"))
            {
                Console.ResetColor();
                FileSystem.SuccessWriteLine("xTerminal default colors are restored!");
                return;
            }

            // Set success output message color.
            if (arg.ContainsText("-p"))
            {
                var color = arg.SplitByText("-p", 1).Trim();
                if(string.IsNullOrEmpty(color))
                    throw new Exception("You need to type a color name. Use -h for more information!");

                var isColorPresent = Enum.TryParse(typeof(ConsoleColor), color, true, out _);
                if (!isColorPresent)
                    throw new Exception("This color does not exist. Check -h param for color list.");
                
                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUIsc, color);
                GlobalVariables.successColorOutput = color;
                FileSystem.SuccessWriteLine($"Output success console color set to: {color}");
                return;
            }

            // Display help message.
            if (arg == $"{Name} -h")
            {
                Console.WriteLine(_helpMessage);
            }
        }

        private void SetUserColor(string color, string settings, string indicator, bool enable, Core.SystemTools.UI.Setting setting, bool isCDvisable = true)
        {
            try
            {
                if (setting.ToString() == "Indicator")
                {
                    int indexColor = _colors.FindIndex(c => c == color.ToLower());
                    string outColor = _colors[indexColor];
                    string colorSetting;
                    int indexIndicator = _indicators.FindIndex(i => i == indicator);
                    string indiOut = _indicators[indexIndicator];
                    colorSetting = $"{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.UserInfo)}|{outColor};{indiOut}|{Core.SystemTools.UI.SanitizeSettings(settings, Core.SystemTools.UI.Setting.CurrentDirectoy)}";
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUI, colorSetting);
                }
                else if (setting.ToString() == "UserInfo")
                {
                    int indexColor = _colors.FindIndex(c => c == color.ToLower());
                    string outColor = _colors[indexColor];
                    string colorSetting;
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
                    string colorSetting;
                    // Disable display current directory from registry.
                    if (!isCDvisable)
                    {
                        RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUIcd, isCDvisable.ToString());
                        return;
                    }

                    // Enable display current directory from registry.
                    if (isCDvisable)
                    {
                        RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUIcd, isCDvisable.ToString());
                        return;
                    }

                    int indexColor = _colors.FindIndex(c => c == color.ToLower());
                    string outColor = _colors[indexColor];
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
