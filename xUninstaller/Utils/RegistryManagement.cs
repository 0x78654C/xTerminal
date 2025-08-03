using Microsoft.Win32;
using System.Runtime.Versioning;

namespace xUninstaller
{
    [SupportedOSPlatform("windows")]
    internal class RegistryManagement
    {
        /// <summary>
        /// Delete a subkey by name.
        /// </summary>
        /// <param name="keyName">Main key name.</param>
        /// <param name="subKeyName">Sub key name.</param>
        public static void regKey_Delete(string keyName, string subKeyName, bool isSubKey = false)
        {
            try
            {
                if (isSubKey)
                    Registry.CurrentUser.DeleteSubKeyTree(subKeyName);
                else
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(keyName, true))
                        key?.DeleteSubKeyTree(subKeyName);
                }
            }
            catch { }
        }
    }
}
