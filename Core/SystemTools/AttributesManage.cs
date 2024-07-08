using System.IO;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Core.SystemTools
{
    [SupportedOSPlatform("windows")]
    public class AttributesManage
    {
        private List<string> ListAttributes { get; set; }
        private string Path { get; set; }
        private string _currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);

        /// <summary>
        /// Set or remvoe attributes from a file or directory.
        /// </summary>
        /// <param name="listAttributes"></param>
        /// <param name="path"></param>
        public AttributesManage(List<string> listAttributes, string path)
        {
            ListAttributes = listAttributes;
            Path = FileSystem.SanitizePath(path, _currentDirectory);
        }

        /// <summary>
        /// Set file or directories attributes.
        /// </summary>
        public void SetRmoveAttributes(bool remove)
        {
            if (IsFileOrDirectoryPresent(Path))
            {
                FileSystem.ErrorWriteLine($"Directory/File does not exist: {Path}");
                return;
            }

            if (ListAttributes.Count == 0)
            {
                FileSystem.ErrorWriteLine($"You need to specify at least one attribute!");
                return;
            }

            if (remove)
            {
                foreach (var attribute in ListAttributes)
                    AttributeSet(attribute, Path);
            }
            else
            {
                foreach (var attribute in ListAttributes)
                    AttributeRemove(attribute, Path);
            }
        }

        private bool IsFileOrDirectoryPresent(string path) => (Directory.Exists(path) || File.Exists(path));

        /// <summary>
        /// Set attribute to file or directory.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="path"></param>
        private void AttributeSet(string attribute, string path)
        {
            switch (attribute.ToLower())
            {
                case "archive":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Archive);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "Compressed":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Compressed);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "Device":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Device);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "Directory":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Directory);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "Encrypted":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Encrypted);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "Hidden":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "IntegrityStream":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.IntegrityStream);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "None":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.None);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "Normal":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Normal);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "NoScrubData":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.NoScrubData);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "NotContentIndexed":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.NotContentIndexed);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "Offline":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Offline);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "ReadOnly":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "ReparsePoint":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReparsePoint);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "SparseFile":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.SparseFile);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "System":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.System);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "Temporary":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Temporary);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
            }
        }

        private void AttributeRemove(string attribute, string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            switch (attribute.ToLower())
            {
                case "archive":
                    attributes = RemoveAttributes(attributes, FileAttributes.Archive);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "Compressed":
                    attributes = RemoveAttributes(attributes, FileAttributes.Compressed);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "Device":
                    attributes = RemoveAttributes(attributes, FileAttributes.Device);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}"); ;
                    break;
                case "Directory":
                    attributes = RemoveAttributes(attributes, FileAttributes.Directory);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "Encrypted":
                    attributes = RemoveAttributes(attributes, FileAttributes.Encrypted);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "Hidden":
                    attributes = RemoveAttributes(attributes, FileAttributes.Hidden);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "IntegrityStream":
                    attributes = RemoveAttributes(attributes, FileAttributes.IntegrityStream);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "None":
                    attributes = RemoveAttributes(attributes, FileAttributes.None);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "Normal":
                    attributes = RemoveAttributes(attributes, FileAttributes.Normal);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "NoScrubData":
                    attributes = RemoveAttributes(attributes, FileAttributes.NoScrubData);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}"); ;
                    break;
                case "NotContentIndexed":
                    attributes = RemoveAttributes(attributes, FileAttributes.NotContentIndexed);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}"); ;
                    break;
                case "Offline":
                    attributes = RemoveAttributes(attributes, FileAttributes.Offline);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "ReadOnly":
                    attributes = RemoveAttributes(attributes, FileAttributes.ReadOnly);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "ReparsePoint":
                    attributes = RemoveAttributes(attributes, FileAttributes.ReparsePoint);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "SparseFile":
                    attributes = RemoveAttributes(attributes, FileAttributes.SparseFile);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "System":
                    attributes = RemoveAttributes(attributes, FileAttributes.System);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "Temporary":
                    attributes = RemoveAttributes(attributes, FileAttributes.Temporary);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
            }
        }

        /// <summary>
        /// Remove attribute from file or directory.
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="attributesToRemove"></param>
        /// <returns></returns>
        FileAttributes RemoveAttributes(FileAttributes attributes, FileAttributes attributesToRemove) => attributes & ~attributesToRemove;
    }
}
