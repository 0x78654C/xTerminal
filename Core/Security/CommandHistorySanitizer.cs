using System;
using System.Text.RegularExpressions;

namespace Core.Security
{
    /// <summary>
    /// Removes credential values before commands are retained in interactive or on-disk history.
    /// </summary>
    public static class CommandHistorySanitizer
    {
        private const string ValuePattern = "(?:\"[^\"]*\"|'[^']*'|[^\\s]+)";
        private static readonly Regex s_namedSecret = new Regex(
            "(?<prefix>(?:^|\\s)(?:--?(?:password|passwd|token|api[-_]?key|secret|client[-_]?secret|access[-_]?token))(?:\\s+|=))(?<value>" + ValuePattern + ")",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex s_waifuSecret = new Regex(
            "(?<prefix>(?:^|\\s)(?:-p|-b|-db|-lb|-df|-gf)(?:\\s+|=))(?<value>" + ValuePattern + ")",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex s_assignmentSecret = new Regex(
            "(?<prefix>(?:^|\\s)(?:password|passwd|token|api[-_]?key|secret|client[-_]?secret|access[-_]?token)=)(?<value>" + ValuePattern + ")",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex s_authorization = new Regex(
            "(?<prefix>Authorization\\s*:\\s*(?:Bearer\\s+|Basic\\s+)?)(?<value>[^\"'\\s]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex s_uriPassword = new Regex(
            "(?<prefix>\\b[a-z][a-z0-9+.-]*://[^:/@\\s]+:)(?<value>[^@/\\s]+)(?=@)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex s_waifuCommand = new Regex(
            "^\\s*waifu(?:\\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static string Sanitize(string command)
        {
            if (string.IsNullOrEmpty(command))
                return command ?? string.Empty;

            string result = s_namedSecret.Replace(command, ReplaceValue);
            result = s_assignmentSecret.Replace(result, ReplaceValue);
            result = s_authorization.Replace(result, ReplaceValue);
            result = s_uriPassword.Replace(result, ReplaceValue);
            if (s_waifuCommand.IsMatch(result))
                result = s_waifuSecret.Replace(result, ReplaceValue);

            return result;
        }

        public static string SanitizeHistoryEntry(string entry)
        {
            if (string.IsNullOrEmpty(entry))
                return entry ?? string.Empty;

            int marker = entry.IndexOf(">> ", StringComparison.Ordinal);
            if (entry.StartsWith("<< ", StringComparison.Ordinal) && marker >= 0)
                return entry.Substring(0, marker + 3) + Sanitize(entry.Substring(marker + 3));

            return Sanitize(entry);
        }

        private static string ReplaceValue(Match match)
        {
            return match.Groups["prefix"].Value + "[SECRET]";
        }
    }
}
