using Core;
using Core.SystemTools;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Uninstall : ITerminalCommand
    {
        public string Name => "uninstall";

        private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        private const int MaxTableWidth = 118;
        private const int TableColumnCount = 5;

        private static readonly string s_helpMessage = @"Usage of uninstall command:
    uninstall -list                  : List installed applications.
    uninstall -list <filter>         : List installed applications matching filter text.
    uninstall <application_name>     : Start the uninstaller for an installed application.
    uninstall -h                     : Display this help message.

Examples:
    uninstall -list
    uninstall -list chrome
    uninstall ""Google Chrome""

Note: Some uninstallers require administrator privileges and may show their own confirmation UI.
";

        public void Execute(string args)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;

                var parameters = ParseArguments(args);
                if (parameters.Length == 0 || string.Equals(args.Trim(), Name, StringComparison.OrdinalIgnoreCase))
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }

                if (HasParameter(parameters, "-h"))
                {
                    WriteOutput(s_helpMessage);
                    return;
                }

                if (HasParameter(parameters, "-list"))
                {
                    var filter = GetListFilter(parameters);
                    ListApplications(filter);
                    return;
                }

                var appName = GetApplicationName(parameters);
                if (string.IsNullOrWhiteSpace(appName))
                {
                    FileSystem.ErrorWriteLine("Application name is required. Use -h for more information!");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                UninstallApplication(appName);
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
                GlobalVariables.isErrorCommand = true;
            }
        }

        private static void ListApplications(string filter)
        {
            var apps = GetInstalledApplications();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                apps = apps.Where(app => app.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                           .ToList();
            }

            if (apps.Count == 0)
            {
                WriteOutput("No installed applications found.\n");
                return;
            }

            if (!ShouldPipe())
            {
                WriteApplicationsTable(apps, filter);
                return;
            }

            WriteOutput(BuildApplicationsPlainList(apps));
        }

        private static string BuildApplicationsPlainList(List<InstalledApplication> apps)
        {
            var output = new StringBuilder();
            output.AppendLine("Installed applications:");

            foreach (var app in apps)
            {
                output.Append("  ");
                output.Append(app.DisplayName);

                if (!string.IsNullOrWhiteSpace(app.DisplayVersion))
                    output.Append($" | Version: {app.DisplayVersion}");

                if (!string.IsNullOrWhiteSpace(app.Publisher))
                    output.Append($" | Publisher: {app.Publisher}");

                output.AppendLine($" | Scope: {app.Scope}");
            }

            return output.ToString();
        }

        private static void WriteApplicationsTable(List<InstalledApplication> apps, string filter)
        {
            var columns = GetTableColumns(apps);
            string border = BuildBorder(columns);
            int tableWidth = border.Length;

            Console.WriteLine();
            WriteTitleRule(tableWidth, " XTERMINAL UNINSTALL TABLE ", ConsoleColor.Cyan);
            WriteMetaLine(tableWidth, apps.Count, filter);

            FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, border);
            WriteHeaderRow(columns);
            FileSystem.ColorConsoleTextLine(ConsoleColor.DarkGray, border);

            for (int i = 0; i < apps.Count; i++)
            {
                WriteApplicationRow(i + 1, apps[i], columns);
            }

            FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, border);
            Console.WriteLine();
        }

        private static TableColumns GetTableColumns(List<InstalledApplication> apps)
        {
            int tableWidth = GetTableWidth();
            int contentWidth = Math.Max(36, tableWidth - ((TableColumnCount * 3) + 1));

            const int idWidth = 4;
            const int scopeWidth = 7;
            int versionWidth = Clamp(MaxCellWidth(apps.Select(app => app.DisplayVersion), "Version"), 9, 18);
            int publisherWidth = Clamp(MaxCellWidth(apps.Select(app => app.Publisher), "Publisher"), 12, 28);
            int nameWidth = contentWidth - idWidth - versionWidth - publisherWidth - scopeWidth;

            if (nameWidth < 22)
            {
                int deficit = 22 - nameWidth;
                int publisherReduction = Math.Min(deficit, publisherWidth - 12);
                publisherWidth -= publisherReduction;
                deficit -= publisherReduction;

                int versionReduction = Math.Min(deficit, versionWidth - 9);
                versionWidth -= versionReduction;
                nameWidth = contentWidth - idWidth - versionWidth - publisherWidth - scopeWidth;
            }

            if (nameWidth < 10)
            {
                nameWidth = 10;
            }

            return new TableColumns(idWidth, nameWidth, versionWidth, publisherWidth, scopeWidth);
        }

        private static int GetTableWidth()
        {
            try
            {
                int availableWidth = Console.WindowWidth - 1;
                if (availableWidth > 0)
                {
                    return Math.Min(availableWidth, MaxTableWidth);
                }
            }
            catch
            {
            }

            return 100;
        }

        private static int MaxCellWidth(IEnumerable<string> values, string header)
        {
            int max = header.Length;
            foreach (var value in values)
            {
                string normalized = NormalizeValue(value);
                if (normalized.Length > max)
                {
                    max = normalized.Length;
                }
            }

            return max;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static string BuildBorder(TableColumns columns)
        {
            return "+"
                + new string('-', columns.Id + 2) + "+"
                + new string('-', columns.Name + 2) + "+"
                + new string('-', columns.Version + 2) + "+"
                + new string('-', columns.Publisher + 2) + "+"
                + new string('-', columns.Scope + 2) + "+";
        }

        private static void WriteTitleRule(int width, string title, ConsoleColor titleColor)
        {
            title = Clip(title, width);
            int padding = Math.Max(0, width - title.Length);
            int left = padding / 2;
            int right = padding - left;

            FileSystem.ColorConsoleText(ConsoleColor.DarkCyan, new string('=', left));
            FileSystem.ColorConsoleText(titleColor, title);
            FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, new string('=', right));
        }

        private static void WriteMetaLine(int width, int count, string filter)
        {
            string filterText = string.IsNullOrWhiteSpace(filter) ? "none" : filter;
            string text = $"targets:{count}  filter:{filterText}  view:table  source:registry uninstall keys";
            text = "  " + Clip(text, Math.Max(0, width - 2));
            FileSystem.ColorConsoleTextLine(ConsoleColor.DarkGray, text.PadRight(width));
        }

        private static void WriteHeaderRow(TableColumns columns)
        {
            Console.Write("| ");
            WriteCell("#", columns.Id, ConsoleColor.Cyan, rightAlign: true);
            Console.Write(" | ");
            WriteCell("Application", columns.Name, ConsoleColor.Cyan);
            Console.Write(" | ");
            WriteCell("Version", columns.Version, ConsoleColor.Cyan);
            Console.Write(" | ");
            WriteCell("Publisher", columns.Publisher, ConsoleColor.Cyan);
            Console.Write(" | ");
            WriteCell("Scope", columns.Scope, ConsoleColor.Cyan);
            Console.WriteLine(" |");
        }

        private static void WriteApplicationRow(int index, InstalledApplication app, TableColumns columns)
        {
            Console.Write("| ");
            WriteCell(index.ToString(), columns.Id, ConsoleColor.DarkGray, rightAlign: true);
            Console.Write(" | ");
            WriteCell(app.DisplayName, columns.Name, ConsoleColor.White);
            Console.Write(" | ");
            WriteCell(NormalizeValue(app.DisplayVersion), columns.Version, ConsoleColor.Green);
            Console.Write(" | ");
            WriteCell(NormalizeValue(app.Publisher), columns.Publisher, ConsoleColor.Gray);
            Console.Write(" | ");
            WriteCell(app.Scope, columns.Scope, GetScopeColor(app.Scope));
            Console.WriteLine(" |");
        }

        private static void WriteCell(string value, int width, ConsoleColor color, bool rightAlign = false)
        {
            string cell = FormatCell(value, width, rightAlign);
            FileSystem.ColorConsoleText(color, cell);
        }

        private static string FormatCell(string value, int width, bool rightAlign)
        {
            string text = Clip(NormalizeValue(value), width);
            return rightAlign ? text.PadLeft(width) : text.PadRight(width);
        }

        private static string NormalizeValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "N/A" : value.Trim();
        }

        private static string Clip(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || maxLength <= 0)
            {
                return string.Empty;
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            if (maxLength <= 3)
            {
                return value.Substring(0, maxLength);
            }

            return value.Substring(0, maxLength - 3) + "...";
        }

        private static ConsoleColor GetScopeColor(string scope)
        {
            return string.Equals(scope, "Machine", StringComparison.OrdinalIgnoreCase)
                ? ConsoleColor.Magenta
                : ConsoleColor.Yellow;
        }

        private static bool ShouldPipe()
        {
            return GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0;
        }

        private static void UninstallApplication(string appName)
        {
            var apps = GetInstalledApplications();
            var exactMatches = apps.Where(app => string.Equals(app.DisplayName, appName, StringComparison.OrdinalIgnoreCase))
                                   .ToList();

            var matches = exactMatches.Count > 0
                ? exactMatches
                : apps.Where(app => app.DisplayName.IndexOf(appName, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (matches.Count == 0)
            {
                FileSystem.ErrorWriteLine($"Application '{appName}' was not found. Use 'uninstall -list' to see installed applications.");
                GlobalVariables.isErrorCommand = true;
                return;
            }

            if (matches.Count > 1)
            {
                FileSystem.ErrorWriteLine($"More than one application matched '{appName}'. Use the exact application name:");
                foreach (var app in matches.Take(20))
                    Console.WriteLine($"  {app.DisplayName}");

                if (matches.Count > 20)
                    Console.WriteLine($"  ... and {matches.Count - 20} more");

                GlobalVariables.isErrorCommand = true;
                return;
            }

            var selectedApp = matches[0];
            var uninstallString = !string.IsNullOrWhiteSpace(selectedApp.UninstallString)
                ? selectedApp.UninstallString
                : selectedApp.QuietUninstallString;

            if (string.IsNullOrWhiteSpace(uninstallString))
            {
                FileSystem.ErrorWriteLine($"Application '{selectedApp.DisplayName}' does not provide an uninstall command.");
                GlobalVariables.isErrorCommand = true;
                return;
            }

            StartUninstaller(selectedApp.DisplayName, uninstallString);
        }

        private static void StartUninstaller(string displayName, string uninstallString)
        {
            uninstallString = Environment.ExpandEnvironmentVariables(uninstallString.Trim());

            if (!TrySplitCommandLine(uninstallString, out var fileName, out var arguments))
            {
                FileSystem.ErrorWriteLine($"Could not parse uninstall command for '{displayName}'.");
                GlobalVariables.isErrorCommand = true;
                return;
            }

            if (IsMsiExec(fileName))
                arguments = ConvertMsiRepairToUninstall(arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                WorkingDirectory = GetWorkingDirectory(fileName)
            };

            Process.Start(startInfo);
            FileSystem.SuccessWriteLine($"Started uninstaller for: {displayName}");
        }

        private static List<InstalledApplication> GetInstalledApplications()
        {
            var apps = new List<InstalledApplication>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var source in GetRegistrySources())
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(source.Hive, source.View);
                    using var uninstallKey = baseKey.OpenSubKey(UninstallRegistryPath);

                    if (uninstallKey == null)
                        continue;

                    foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                    {
                        using var appKey = uninstallKey.OpenSubKey(subKeyName);
                        if (appKey == null)
                            continue;

                        var displayName = ReadRegistryString(appKey, "DisplayName");
                        if (string.IsNullOrWhiteSpace(displayName))
                            continue;

                        if (ReadRegistryInt(appKey, "SystemComponent") == 1)
                            continue;

                        var releaseType = ReadRegistryString(appKey, "ReleaseType");
                        if (IsWindowsUpdateEntry(releaseType))
                            continue;

                        var uninstallString = ReadRegistryString(appKey, "UninstallString");
                        var quietUninstallString = ReadRegistryString(appKey, "QuietUninstallString");
                        if (string.IsNullOrWhiteSpace(uninstallString) && string.IsNullOrWhiteSpace(quietUninstallString))
                            continue;

                        var app = new InstalledApplication
                        {
                            DisplayName = displayName,
                            DisplayVersion = ReadRegistryString(appKey, "DisplayVersion"),
                            Publisher = ReadRegistryString(appKey, "Publisher"),
                            UninstallString = uninstallString,
                            QuietUninstallString = quietUninstallString,
                            Scope = source.Hive == RegistryHive.LocalMachine ? "Machine" : "User"
                        };

                        var key = $"{app.DisplayName}|{app.UninstallString}|{app.QuietUninstallString}|{app.Scope}";
                        if (seen.Add(key))
                            apps.Add(app);
                    }
                }
                catch
                {
                    // Registry views can be unavailable depending on OS architecture.
                }
            }

            return apps.OrderBy(app => app.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static IEnumerable<RegistrySource> GetRegistrySources()
        {
            yield return new RegistrySource(RegistryHive.CurrentUser, RegistryView.Registry64);
            yield return new RegistrySource(RegistryHive.CurrentUser, RegistryView.Registry32);
            yield return new RegistrySource(RegistryHive.LocalMachine, RegistryView.Registry64);
            yield return new RegistrySource(RegistryHive.LocalMachine, RegistryView.Registry32);
        }

        private static string[] ParseArguments(string args)
        {
            try
            {
                return new SplitArguments(args).CommandLineToArgs();
            }
            catch
            {
                return args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private static bool HasParameter(IEnumerable<string> parameters, string parameter)
        {
            return parameters.Any(p => string.Equals(p, parameter, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetListFilter(string[] parameters)
        {
            var filterParts = parameters.Skip(1)
                                        .Where(p => !string.Equals(p, "-list", StringComparison.OrdinalIgnoreCase));
            return string.Join(" ", filterParts).Trim();
        }

        private static string GetApplicationName(string[] parameters)
        {
            return string.Join(" ", parameters.Skip(1)).Trim();
        }

        private static string ReadRegistryString(RegistryKey key, string valueName)
        {
            var value = key.GetValue(valueName);

            if (value is string[] values)
                return string.Join(";", values).Trim();

            return value?.ToString()?.Trim() ?? string.Empty;
        }

        private static int ReadRegistryInt(RegistryKey key, string valueName)
        {
            var value = key.GetValue(valueName);
            if (value == null)
                return 0;

            if (value is int intValue)
                return intValue;

            return int.TryParse(value.ToString(), out var result) ? result : 0;
        }

        private static bool IsWindowsUpdateEntry(string releaseType)
        {
            return string.Equals(releaseType, "Update", StringComparison.OrdinalIgnoreCase)
                || string.Equals(releaseType, "Hotfix", StringComparison.OrdinalIgnoreCase)
                || string.Equals(releaseType, "Security Update", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TrySplitCommandLine(string commandLine, out string fileName, out string arguments)
        {
            fileName = string.Empty;
            arguments = string.Empty;

            if (string.IsNullOrWhiteSpace(commandLine))
                return false;

            commandLine = commandLine.Trim();

            if (commandLine.StartsWith("\"", StringComparison.Ordinal))
            {
                var closingQuote = commandLine.IndexOf('"', 1);
                if (closingQuote > 1)
                {
                    fileName = commandLine.Substring(1, closingQuote - 1).Trim();
                    arguments = commandLine.Substring(closingQuote + 1).Trim();
                    return !string.IsNullOrWhiteSpace(fileName);
                }
            }

            foreach (var extension in new[] { ".exe", ".msi", ".bat", ".cmd", ".com" })
            {
                var extensionIndex = commandLine.IndexOf(extension, StringComparison.OrdinalIgnoreCase);
                if (extensionIndex < 0)
                    continue;

                var endIndex = extensionIndex + extension.Length;
                fileName = commandLine.Substring(0, endIndex).Trim().Trim('"');
                arguments = commandLine.Substring(endIndex).Trim();
                return !string.IsNullOrWhiteSpace(fileName);
            }

            var parts = new SplitArguments(commandLine).CommandLineToArgs();
            if (parts.Length == 0)
                return false;

            fileName = parts[0].Trim().Trim('"');
            arguments = string.Join(" ", parts.Skip(1)).Trim();
            return !string.IsNullOrWhiteSpace(fileName);
        }

        private static bool IsMsiExec(string fileName)
        {
            var executable = Path.GetFileName(fileName);
            return string.Equals(executable, "msiexec.exe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(executable, "msiexec", StringComparison.OrdinalIgnoreCase);
        }

        private static string ConvertMsiRepairToUninstall(string arguments)
        {
            return Regex.Replace(arguments, @"(^|\s)/I(?=\s|\{)", "$1/X", RegexOptions.IgnoreCase);
        }

        private static string GetWorkingDirectory(string fileName)
        {
            try
            {
                if (!Path.IsPathRooted(fileName))
                    return string.Empty;

                var directory = Path.GetDirectoryName(fileName);
                return Directory.Exists(directory) ? directory : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void WriteOutput(string output)
        {
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = output;
            else
                Console.Write(output);
        }

        private sealed class InstalledApplication
        {
            public string DisplayName { get; set; }
            public string DisplayVersion { get; set; }
            public string Publisher { get; set; }
            public string UninstallString { get; set; }
            public string QuietUninstallString { get; set; }
            public string Scope { get; set; }
        }

        private readonly struct TableColumns
        {
            public TableColumns(int id, int name, int version, int publisher, int scope)
            {
                Id = id;
                Name = name;
                Version = version;
                Publisher = publisher;
                Scope = scope;
            }

            public int Id { get; }
            public int Name { get; }
            public int Version { get; }
            public int Publisher { get; }
            public int Scope { get; }
        }

        private readonly struct RegistrySource
        {
            public RegistrySource(RegistryHive hive, RegistryView view)
            {
                Hive = hive;
                View = view;
            }

            public RegistryHive Hive { get; }
            public RegistryView View { get; }
        }
    }
}
