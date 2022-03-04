using System;
using System.Reflection;

namespace Core.SystemTools
{
    public  class Roslyn
    {
        /// <summary>
        /// Check if file is managed code lib.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsManaged(string path)
        {
            try
            {
                var b = AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch (Exception e)
            {

            }
            return false;
        }
    }
}
