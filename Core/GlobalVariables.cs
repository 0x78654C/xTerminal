using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.Versioning;
using Microsoft.Extensions.AI;
using System.Windows.Forms;

namespace Core
{
    [SupportedOSPlatform("Windows")]
    /// <summary>
    /// Declaration of necessary global variables
    /// </summary>
    public class GlobalVariables
    {
        private static string process = Process.GetCurrentProcess().Id.ToString();
        public static string regKeyName { get; set; } = "xTerminal";
        public static string regCurrentEitor = "CurrentEditor";
        public static string regUI = "UI";
        public static string regUIcd = "UICD";
        public static string regUIsc = "UISC";
        public static string regCportTimeOut = "cportTimeOut";
        public static string regOpenAI_APIKey = "OpenAI_APIKey";
        public static string regOpenAI_Model = "OpenAI_Model";
        public static string regOllama_Model = "Ollama_Model";
        public static string regAutoUpdate = "AutoUpdate";
        public static string ollamaUri = "http://localhost:11434/";
        public static List<ChatMessage> chatHistory = new();
        public static string regHistoryLimitSize = "historyLimitSize";
        public static int historyLimitSize { get; set; } = 2000;
        public static string commandOut { get; set; }
        public static string version { get; set; }
        public static string aliasParameters { get; set; } = string.Empty;
        public static readonly string accountName = Environment.UserName;
        public static string rootPath = $"{Path.GetPathRoot(Environment.SystemDirectory)}Users\\{accountName}\\";
        public static readonly string computerName = Environment.MachineName;
        public static string terminalWorkDirectory = $"{Path.GetPathRoot(Environment.SystemDirectory)}Users\\{accountName}\\AppData\\Local\\xTerminal";
        public static string passwordManagerDirectory = $"{terminalWorkDirectory}\\Pwm\\";
        public static string unpackUpdate = $"{terminalWorkDirectory}\\Unpack\\";
        public static string aliasFile = $"{terminalWorkDirectory}\\alias.json";
        public static string currentDirectory { get; set; } = terminalWorkDirectory + $"\\{process}cDir.t";
        public static string uiSettings = terminalWorkDirectory + $"\\{process}ui.t";
        public static string historyFile = terminalWorkDirectory + "\\History.db";
        public static string addonDirectory = Application.StartupPath + "Add-ons";
        public static List<string> excludeDirectories = new List<string>() { "System Volume Information", "$Recycle.Bin" };
        public static List<string> excludeFiles = new List<string>() { "pagefile.sys" };
        public static bool eventCancelKey = false;
        public static bool autoSuggestion = false;
        public static bool aliasRunFlag = false;
        public static List<string> aliasInParameter = new List<string>();
        public static bool eventKeyFlagX = false;
        public static readonly string magicNunmbers = Application.StartupPath + "ext_list.txt";
        public static bool isPipeCommand = false;
        public static string pipeCmdOutput { get; set; }
        public static bool isPipeVar = false;
        public static int pipeCmdCount { get; set; } = 0;
        public static int pipeCmdCountTemp { get; set; } = 0;
        public static int fileHexLength { get; set; } = 0;
        public static string successColorOutput = "Gray";
        public static CompressionLevel compressionLevel = CompressionLevel.Fastest;
        public static int lengthPS1 = 0;
        public enum TypeSuggestions
        {
            File = 1,
            Directory = 2,
            All = 3
        };
        public static int tabPressCount = 0;
        public static bool isErrorCommand = false;
        public static string bgProcessListFile = terminalWorkDirectory + "\\bgPids.db";
        public static string workingDirectory { get; set; }
        public static bool isNewVersion { get; set; } = false;
        public static string versionNew { get; set; } = string.Empty;
        public static string releaseBody { get; set; } = string.Empty;


        //----------------------- Commands parameters --------------------------------------
        // The first item is the command; the remaining items are its fixed options and subcommands.
        // Paths, other user-supplied values and options forwarded to external programs are not listed.
        public static List<string> addon_p = new List<string>() { "!", "-h", "-p", "-add", "-c", "-del", "-list" };
        public static List<string> start_p = new List<string>() { "./", "-h", "-param", "-u", "-we", "-wi", "-swd", "-wd" };
        public static List<string> repeat_p = new List<string>() { "+" };
        public static List<string> alias_p = new List<string>() { "alias", "-h", "-add", "-del", "-list", "-clear", "-update" };
        public static List<string> attr_p = new List<string>() { "attr", "-h", "-s", "-r" };
        public static List<string> bc_p = new List<string>() { "bc", "-h" };
        public static List<string> bench_p = new List<string>() { "bench", "-h", "-n" };
        public static List<string> bios_p = new List<string>() { "bios", "-h", "-r" };
        public static List<string> cal_p = new List<string>() { "cal", "-h" };
        public static List<string> cat_p = new List<string>() { "cat", "-h", "-t", "-b", "-l", "-s", "-so", "-sa", "-sao", "-sm", "-smo", "-lc", "-lfc", "-con", "-st", "-eq", "-ed", "-f", "-o" };
        public static List<string> ccs_p = new List<string>() { "ccs", "-h", "-p" };
        public static List<string> cd_p = new List<string>() { "cd" };
        public static List<string> cgpt_p = new List<string>() { "cgpt", "-h", "-setkey", "-setmodel", "-currm", "-l", "-sm", "-cm", "-o" };
        public static List<string> ch_p = new List<string>() { "ch", "-h", "-d", "-sz", "-rz" };
        public static List<string> chain_p = new List<string>() { "chain", "-h", "-create", "-run", "-add", "-show", "-list", "-del" };
        public static List<string> chistory_p = new List<string>() { "chistory" };
        public static List<string> clear_p = new List<string>() { "clear" };
        public static List<string> cmd_p = new List<string>() { "cmd" };
        public static List<string> cmp_p = new List<string>() { "cmp", "-h" };
        public static List<string> cp_p = new List<string>() { "cp" };
        public static List<string> cport_p = new List<string>() { "cport", "-h", "-p", "--noping" };
        public static List<string> ctx_p = new List<string>() { "ctx", "-h", "save", "load", "list", "show", "del" };
        public static List<string> del_p = new List<string>() { "del", "-h", "-a", "-af", "-ad" };
        public static List<string> diff_p = new List<string>() { "diff", "-h", "-verbose", "-f" };
        public static List<string> dspoof_p = new List<string>() { "dspoof", "-h" };
        public static List<string> echo_p = new List<string>() { "echo", "-h", "-con", "-o", "-e", ">", ">>" };
        public static List<string> edit_p = new List<string>() { "edit", "-h", "-set", "-current" };
        public static List<string> enc_p = new List<string>() { "enc", "-h", "-current" };
        public static List<string> env_p = new List<string>() { "env", "-h", "-t", "-get", "-set", "-del", "-addval", "-delval", "-load", "-export" };
        public static List<string> exif_p = new List<string>() { "exif", "-h" };
        public static List<string> exit_p = new List<string>() { "exit" };
        public static List<string> extip_p = new List<string>() { "extip", "-h" };
        public static List<string> fcopy_p = new List<string>() { "fcopy", "-h", "-o", "-ca" };
        public static List<string> file_p = new List<string>() { "file", "-h", "-ext" };
        public static List<string> flappy_p = new List<string>() { "flappy" };
        public static List<string> fmove_p = new List<string>() { "fmove", "-h", "-o", "-ma" };
        public static List<string> fsmon_p = new List<string>() { "fsmon", "-h", "-r", "-l" };
        public static List<string> fw_p = new List<string>() { "fw", "-h", "-list", "-in", "-out", "-add", "-del", "-en", "-dis", "-n", "-p", "-pf", "-di", "-a", "-lP", "-rP", "-lA", "-rA", "-pr", "-de", "-e" };
        public static List<string> fxp_p = new List<string>() { "fxp", "-h", "--help" };
        public static List<string> hash_p = new List<string>() { "hash", "-h", "-sha256", "-sha512" };
        public static List<string> help_p = new List<string>() { "help" };
        public static List<string> hex_p = new List<string>() { "hex", "-h", "-o" };
        public static List<string> icheck_p = new List<string>() { "icheck", "-h" };
        public static List<string> ifconfig_p = new List<string>() { "ifconfig", "-h", "-i" };
        public static List<string> ispeed_p = new List<string>() { "ispeed" };
        public static List<string> kill_p = new List<string>() { "kill", "-h", "-e", "-i" };
        public static List<string> latmon_p = new List<string>() { "latmon", "-h", "-n" };
        public static List<string> ln_p = new List<string>() { "ln", "-h", "-o" };
        public static List<string> locate_p = new List<string>() { "locate", "-h", "-s", "-e", "-eq", "-d", "-o" };
        public static List<string> lock_p = new List<string>() { "lock" };
        public static List<string> logoff_p = new List<string>() { "logoff" };
        public static List<string> ls_p = new List<string>() { "ls", "-h", "-d", "-f", "-s", "-c", "-cf", "-cd", "-hl", "-o", "-ct", "-la", "-dup", "-n" };
        public static List<string> md5_p = new List<string>() { "md5", "-h", "-d", "-o" };
        public static List<string> mkdir_p = new List<string>() { "mkdir", "-h" };
        public static List<string> mkfile_p = new List<string>() { "mkfile", "-h" };
        public static List<string> mv_p = new List<string>() { "mv", "-h", "-o" };
        public static List<string> note_p = new List<string>() { "note", "-h", "add", "list", "del", "clear" };
        public static List<string> nt_p = new List<string>() { "nt", "-h", "-u" };
        public static List<string> odir_p = new List<string>() { "odir", "-h" };
        public static List<string> pcinfo_p = new List<string>() { "pcinfo" };
        public static List<string> ping_p = new List<string>() { "ping", "-h", "-t" };
        public static List<string> pjson_p = new List<string>() { "pjson", "-h", "-o" };
        public static List<string> plist_p = new List<string>() { "plist", "-h" };
        public static List<string> ps_p = new List<string>() { "ps" };
        public static List<string> pwd_p = new List<string>() { "pwd", "-h" };
        public static List<string> pwm_p = new List<string>() { "pwm", "-h", "-createv", "-delv", "-listv", "-addapp", "-dela", "-updatea", "-lista" };
        public static List<string> reboot_p = new List<string>() { "reboot", "-h", "-f", "-m" };
        public static List<string> sc_p = new List<string>() { "sc", "-h", "-list", "--noinfo", "-des", "-status", "-stop", "-start", "-restart", "-r" };
        public static List<string> shred_p = new List<string>() { "shred", "-h", "-i" };
        public static List<string> shutdown_p = new List<string>() { "shutdown", "-h", "-f", "-m" };
        public static List<string> sinfo_p = new List<string>() { "sinfo", "-h", "-r" };
        public static List<string> sleep_p = new List<string>() { "sleep" };
        public static List<string> snake_p = new List<string>() { "snake" };
        public static List<string> snap_p = new List<string>() { "snap", "-h", "save", "diff", "list", "del" };
        public static List<string> sort_p = new List<string>() { "sort", "-h", "-a", "-d", "-o", "-t" };
        public static List<string> ssh_p = new List<string>() { "ssh", "-h" };
        public static List<string> tee_p = new List<string>() { "tee", "-h", "-a" };
        public static List<string> time_p = new List<string>() { "time" };
        public static List<string> trace_p = new List<string>() { "trace", "-h", "-ipv6", "-hops", "-timeout" };
        public static List<string> ui_p = new List<string>() { "ui", "-h", "-u", "-c", "-i", "-s", "-cd", "-r", "-p" };
        public static List<string> uninstall_p = new List<string>() { "uninstall", "-h", "-list" };
        public static List<string> ver_p = new List<string>() { "ver" };
        public static List<string> waifu_p = new List<string>() { "waifu", "-h", "-cb", "-db", "-df", "-gf", "-lb", "-lr", "-u", "-b", "-p", "-o", "-e" };
        public static List<string> watch_p = new List<string>() { "watch", "-h", "-n" };
        public static List<string> wget_p = new List<string>() { "wget", "-h", "--tls", "-o", "--noping" };
        public static List<string> wol_p = new List<string>() { "wol", "-h", "-ip", "-mac", "-port" };
        public static List<string> wtop_p = new List<string>() { "wtop", "-h", "-tree" };
        public static List<string> xcel_p = new List<string>() { "xcel", "-h" };
        public static List<string> xclip_p = new List<string>() { "xclip", "-h" };
        public static List<string> xt_p = new List<string>() { "xt", "-h", "-p", "-new", "-edit", "-check", "-ver"};
        public static List<string> xte_p = new List<string>() { "xte", "-h", "-new", "-syntax" };
        public static List<string> xup_p = new List<string>() { "xup", "-h", "-e", "-d", "-s", "-c" };
        public static List<string> zip_p = new List<string>() { "zip", "-h", "-n", "-list", "-x", "-c" };

        public static Dictionary<string, List<string>> masterParamCommands =
            new Dictionary<string, List<string>>
            {
                { "!", addon_p },
                { "./", start_p },
                { "+", repeat_p },
                { "alias", alias_p },
                { "attr", attr_p },
                { "bc", bc_p },
                { "bench", bench_p },
                { "bios", bios_p },
                { "cal", cal_p },
                { "cat", cat_p },
                { "ccs", ccs_p },
                { "cd", cd_p },
                { "cgpt", cgpt_p },
                { "ch", ch_p },
                { "chain", chain_p },
                { "chistory", chistory_p },
                { "clear", clear_p },
                { "cmd", cmd_p },
                { "cmp", cmp_p },
                { "cp", cp_p },
                { "cport", cport_p },
                { "ctx", ctx_p },
                { "del", del_p },
                { "diff", diff_p },
                { "dspoof", dspoof_p },
                { "echo", echo_p },
                { "edit", edit_p },
                { "enc", enc_p },
                { "env", env_p },
                { "exif", exif_p },
                { "exit", exit_p },
                { "extip", extip_p },
                { "fcopy", fcopy_p },
                { "file", file_p },
                { "flappy", flappy_p },
                { "fmove", fmove_p },
                { "fsmon", fsmon_p },
                { "fw", fw_p },
                { "fxp", fxp_p },
                { "hash", hash_p },
                { "help", help_p },
                { "hex", hex_p },
                { "icheck", icheck_p },
                { "ifconfig", ifconfig_p },
                { "ispeed", ispeed_p },
                { "kill", kill_p },
                { "latmon", latmon_p },
                { "ln", ln_p },
                { "locate", locate_p },
                { "lock", lock_p },
                { "logoff", logoff_p },
                { "ls", ls_p },
                { "md5", md5_p },
                { "mkdir", mkdir_p },
                { "mkfile", mkfile_p },
                { "mv", mv_p },
                { "note", note_p },
                { "nt", nt_p },
                { "odir", odir_p },
                { "pcinfo", pcinfo_p },
                { "ping", ping_p },
                { "pjson", pjson_p },
                { "plist", plist_p },
                { "ps", ps_p },
                { "pwd", pwd_p },
                { "pwm", pwm_p },
                { "reboot", reboot_p },
                { "sc", sc_p },
                { "shred", shred_p },
                { "shutdown", shutdown_p },
                { "sinfo", sinfo_p },
                { "sleep", sleep_p },
                { "snake", snake_p },
                { "snap", snap_p },
                { "sort", sort_p },
                { "ssh", ssh_p },
                { "tee", tee_p },
                { "time", time_p },
                { "trace", trace_p },
                { "ui", ui_p },
                { "uninstall", uninstall_p },
                { "ver", ver_p },
                { "waifu", waifu_p },
                { "watch", watch_p },
                { "wget", wget_p },
                { "wol", wol_p },
                { "wtop", wtop_p },
                { "xcel", xcel_p },
                { "xclip", xclip_p },
                { "xt", xt_p },
                { "xte", xte_p },
                { "xup", xup_p },
                { "zip", zip_p }
            };

    }
}
