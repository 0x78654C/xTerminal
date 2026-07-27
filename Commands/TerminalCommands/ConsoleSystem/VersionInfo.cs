/*
    Version display and sha256 for project built
 */

using Core;
using Core.Encryption;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class VersionInfo : ITerminalCommand
    {
        public string Name => "ver";

        public void Execute(string args)
        {
            var version = string.IsNullOrWhiteSpace(GlobalVariables.version)
                ? Application.ProductVersion
                : GlobalVariables.version;

            var architecture = RuntimeInformation.ProcessArchitecture.ToString();
            var pathExecutable = Path.GetDirectoryName(Application.ExecutablePath);
            var commandsDll = @$"{pathExecutable}\Commands.dll";
            var coreDll = @$"{pathExecutable}\Core.dll";
            var ollamaDll = @$"{pathExecutable}\OllamaInt.dll";
            var openaiDll = @$"{pathExecutable}\OpenAI.Api.Client.dll";
            var openrouterDll = @$"{pathExecutable}\Openrouter.dll";
            var xterminalDll = @$"{pathExecutable}\xTerminal.dll";
            var sha256Commands = File.Exists(commandsDll) ? HashAlgo.GetSHA256(commandsDll).ToUpper() : "File does not exist!";
            var sha256Core = File.Exists(coreDll) ? HashAlgo.GetSHA256(coreDll).ToUpper() : "File does not exist!";
            var sha256Ollama = File.Exists(ollamaDll) ? HashAlgo.GetSHA256(ollamaDll).ToUpper() : "File does not exist!";
            var sha256OpenAi = File.Exists(openaiDll) ? HashAlgo.GetSHA256(openaiDll).ToUpper() : "File does not exist!";
            var sha256Openrouter = File.Exists(openrouterDll) ? HashAlgo.GetSHA256(openrouterDll).ToUpper() : "File does not exist!";
            var verExe = File.Exists(xterminalDll) ? AssemblyName.GetAssemblyName(xterminalDll).Version.ToString() : "File does not exist!";
            var verCore = File.Exists(coreDll) ? AssemblyName.GetAssemblyName(coreDll).Version.ToString() : "File does not exist!";
            var verCommands = File.Exists(commandsDll) ? AssemblyName.GetAssemblyName(commandsDll).Version.ToString() : "File does not exist!";
            var verOllama = File.Exists(ollamaDll) ? AssemblyName.GetAssemblyName(ollamaDll).Version.ToString() : "File does not exist!";
            var verOpenAi = File.Exists(openaiDll) ? AssemblyName.GetAssemblyName(openaiDll).Version.ToString() : "File does not exist!";
            var verOpenrouter = File.Exists(openrouterDll) ? AssemblyName.GetAssemblyName(openrouterDll).Version.ToString() : "File does not exist!";
            var sha256 = HashAlgo.GetSHA256(xterminalDll).ToUpper();
            var versions = new[]
            {
                verExe,
                verCore,
                verCommands,
                verOllama,
                verOpenAi,
                verOpenrouter
            };

            int versionWidth = versions.Max(v => v?.Length ?? 0) + 2;
            int fileNameWidth = 24;

            string Line(string fileName, string? fileVersion, string hash)
            {
                string formattedVersion = $"({fileVersion ?? string.Empty})";

                return $"{fileName.PadRight(fileNameWidth)}" +
                       $"{formattedVersion.PadRight(versionWidth)} - SHA256: {hash}\n";
            }

            var output =
                $"xTerminal version: {version} ({architecture})\n" +
                "__________________________________\n" +
                Line("xTerminal.dll", verExe, sha256) +
                Line("Core.dll", verCore, sha256Core) +
                Line("Commands.dll", verCommands, sha256Commands) +
                Line("OllamaInt.dll", verOllama, sha256Ollama) +
                Line("OpenAI.Api.Client.dll", verOpenAi, sha256OpenAi) +
                Line("Openrouter.dll", verOpenrouter, sha256Openrouter);

            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = output;
            else
                FileSystem.SuccessWriteLine(output);

            GlobalVariables.isErrorCommand = false;
        }
    }
}
