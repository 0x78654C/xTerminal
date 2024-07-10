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
            if (!FileSystem.IsFileOrDirectoryPresent(Path))
            {
                FileSystem.ErrorWriteLine($"Directory/File does not exist: {Path}");
                return;
            }

            if (ListAttributes.Count == 0)
            {
                FileSystem.ErrorWriteLine($"You need to specify at least one attribute!");
                return;
            }
            foreach (var attribute in ListAttributes)
                SetRemoveSingle(attribute.Trim(), Path, remove);
        }

        /// <summary>
        /// Set/Remove single attribute from file.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="path"></param>
        public void SetRemoveSingle(string attribute = "", string path = "", bool remove = false)
        {
            if (!FileSystem.IsFileOrDirectoryPresent(path))
            {
                FileSystem.ErrorWriteLine($"Directory/File does not exist: {path}");
                return;
            }

            FileAttributes attributes = File.GetAttributes(path);
            switch (attribute.ToLower())
            {
                case "archive":
                    if (remove)
                    {
                        attributes = RemoveAttributes(attributes, FileAttributes.Archive);
                        File.SetAttributes(path, attributes);
                        FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    }
                    else
                    {
                        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Archive);
                        FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    }
                    break;
                case "directory":
                    if (remove)
                    {
                        attributes = RemoveAttributes(attributes, FileAttributes.Directory);
                        File.SetAttributes(path, attributes);
                        FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    }
                    else
                    {
                        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Directory);
                        FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    }
                    break;
                case "hidden":
                    if (remove)
                    {
                        attributes = RemoveAttributes(attributes, FileAttributes.Hidden);
                        File.SetAttributes(path, attributes);
                        FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    }
                    else
                    {
                        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
                        FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    }
                    break;
                case "normal":
                    if (remove)
                    {
                        attributes = RemoveAttributes(attributes, FileAttributes.Normal);
                        File.SetAttributes(path, attributes);
                        FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    }
                    else
                    {
                        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Normal);
                        FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    }
                    break;
                case "readonly":
                    if (remove)
                    {
                        attributes = RemoveAttributes(attributes, FileAttributes.ReadOnly);
                        File.SetAttributes(path, attributes);
                        FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    }
                    else
                    {
                        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
                        FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    }
                    break;
                case "system":
                    if (remove)
                    {
                        attributes = RemoveAttributes(attributes, FileAttributes.System);
                        File.SetAttributes(path, attributes);
                        FileSystem.SuccessWriteLine($"Attribute removed: {attribute}");
                    }
                    else
                    {
                        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.System);
                        FileSystem.SuccessWriteLine($"Attribute set: {attribute}");
                    }
                    break;
                default:
                    if (remove)
                        FileSystem.ErrorWriteLine($"This attribute cannot be removed: {attribute}");
                    else
                        FileSystem.ErrorWriteLine($"This attribute cannot be set: {attribute}");
                    break;
            }
        }

        /// <summary>
        /// Get file or directory attributes.
        /// </summary>
        public void GetFileAttributes()
        {
            FileAttributes attributes = File.GetAttributes(Path);
            FileSystem.SuccessWriteLine(attributes);
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
