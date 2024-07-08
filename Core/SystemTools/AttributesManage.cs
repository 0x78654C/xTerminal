using System.IO;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Core.SystemTools
{
    /* Set/remove file attributes lib */

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
            if (!IsFileOrDirectoryPresent(Path))
            {
                FileSystem.ErrorWriteLine($"Directory/File does not exist: {Path}");
                return;
            }

            if (ListAttributes.Count == 0)
            {
                FileSystem.ErrorWriteLine($"You need to specify at least one attribute!");
                return;
            }

            if (!remove)
            {
                foreach (var attribute in ListAttributes)
                    AttributeSetSingle(attribute.Trim(), Path);
            }
            else
            {
                foreach (var attribute in ListAttributes)
                    AttributeRemoveSingle(attribute.Trim(), Path);
            }
        }

        /// <summary>
        /// Check if file or folder exist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsFileOrDirectoryPresent(string path) => (Directory.Exists(path) || File.Exists(path));

        /// <summary>
        /// Set single attribute to file or directory.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="path"></param>
        public void AttributeSetSingle(string attribute="", string path="")
        {
            if (!IsFileOrDirectoryPresent(path))
            {
                FileSystem.ErrorWriteLine($"Directory/File does not exist: {path}");
                return;
            }

            switch (attribute.ToLower())
            {
                case "archive":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Archive);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "compressed":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Compressed);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "device":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Device);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "directory":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Directory);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "hidden":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "integritystream":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.IntegrityStream);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "none":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.None);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "normal":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Normal);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "noscrubdata":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.NoScrubData);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "notcontentindexed":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.NotContentIndexed);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "offline":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Offline);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "readonly":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "reparsepoint":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReparsePoint);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "sparsefile":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.SparseFile);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "system":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.System);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                case "temporary":
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Temporary);
                    FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    break;
                default:
                    FileSystem.ErrorWriteLine($"This attribute cannot be set: {attribute}");
                    break;
            }
        }

        /// <summary>
        /// Remove single attribute from file.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="path"></param>
        public void AttributeRemoveSingle(string attribute="", string path="")
        {
            if (!IsFileOrDirectoryPresent(path))
            {
                FileSystem.ErrorWriteLine($"Directory/File does not exist: {path}");
                return;
            }

            FileAttributes attributes = File.GetAttributes(path);
            switch (attribute.ToLower())
            {
                case "archive":
                    attributes = RemoveAttributes(attributes, FileAttributes.Archive);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "compressed":
                    attributes = RemoveAttributes(attributes, FileAttributes.Compressed);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "device":
                    attributes = RemoveAttributes(attributes, FileAttributes.Device);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}"); ;
                    break;
                case "directory":
                    attributes = RemoveAttributes(attributes, FileAttributes.Directory);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "hidden":
                    attributes = RemoveAttributes(attributes, FileAttributes.Hidden);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "integritystream":
                    attributes = RemoveAttributes(attributes, FileAttributes.IntegrityStream);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "none":
                    attributes = RemoveAttributes(attributes, FileAttributes.None);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "normal":
                    attributes = RemoveAttributes(attributes, FileAttributes.Normal);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "noscrubdata":
                    attributes = RemoveAttributes(attributes, FileAttributes.NoScrubData);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}"); ;
                    break;
                case "notcontentindexed":
                    attributes = RemoveAttributes(attributes, FileAttributes.NotContentIndexed);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}"); ;
                    break;
                case "offline":
                    attributes = RemoveAttributes(attributes, FileAttributes.Offline);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "readonly":
                    attributes = RemoveAttributes(attributes, FileAttributes.ReadOnly);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "reparsepoint":
                    attributes = RemoveAttributes(attributes, FileAttributes.ReparsePoint);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "sparsefile":
                    attributes = RemoveAttributes(attributes, FileAttributes.SparseFile);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "system":
                    attributes = RemoveAttributes(attributes, FileAttributes.System);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                case "temporary":
                    attributes = RemoveAttributes(attributes, FileAttributes.Temporary);
                    File.SetAttributes(path, attributes);
                    FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    break;
                default:
                    FileSystem.ErrorWriteLine($"This attribute cannot be removed: {attribute}");
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
