using System;
using System.IO;

namespace Core.Network
{
    public class UriSafety
    {
        /// <summary>
        /// Creates a Uri object from a string and ensures that it is an absolute HTTP or HTTPS URL.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Uri CreateHttpUri(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("Download URL must be an absolute HTTP or HTTPS URL.");

            return uri;
        }

        /// <summary>
        /// Resolve a URL to a filename that is guaranteed to remain inside the destination directory.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destinationDirectory"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public static string GetSafeDownloadPath(Uri source, string destinationDirectory)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!source.IsAbsoluteUri
                || (source.Scheme != Uri.UriSchemeHttp && source.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("Download URL must be an absolute HTTP or HTTPS URL.", nameof(source));
            if (string.IsNullOrWhiteSpace(destinationDirectory))
                throw new ArgumentException("Download destination is empty.", nameof(destinationDirectory));

            string localPath = Uri.UnescapeDataString(source.LocalPath);
            string fileName = Path.GetFileName(localPath);
            if (string.IsNullOrWhiteSpace(fileName)
                || fileName == "." || fileName == ".."
                || fileName.EndsWith(".", StringComparison.Ordinal)
                || fileName.EndsWith(" ", StringComparison.Ordinal)
                || IsReservedWindowsFileName(fileName)
                || fileName.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\' }) >= 0
                || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException("The URL does not contain a safe local filename.");

            string root = Path.GetFullPath(destinationDirectory);
            string rootPrefix = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
            string candidate = Path.GetFullPath(Path.Combine(root, fileName));
            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The download filename escapes the destination directory.");

            return candidate;
        }


        /// <summary>
        /// Checks if the given file name is a reserved name in Windows (e.g., CON, PRN, AUX, NUL, COM1, LPT1, etc.).
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static bool IsReservedWindowsFileName(string fileName)
        {
            string stem = Path.GetFileNameWithoutExtension(fileName);
            if (stem.Equals("CON", StringComparison.OrdinalIgnoreCase)
                || stem.Equals("PRN", StringComparison.OrdinalIgnoreCase)
                || stem.Equals("AUX", StringComparison.OrdinalIgnoreCase)
                || stem.Equals("NUL", StringComparison.OrdinalIgnoreCase))
                return true;

            return stem.Length == 4
                && (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                    || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                && stem[3] >= '1' && stem[3] <= '9';
        }
    }
}
