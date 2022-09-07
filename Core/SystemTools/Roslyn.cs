using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Core.SystemTools
{
    public  class Roslyn
    {

        /// <summary>
        /// Get runtime managed assambly.
        /// </summary>
        /// <returns></returns>
        public static List<MetadataReference> References()
        {
            List<MetadataReference> references = new List<MetadataReference>();
            foreach (var refs in ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator))
                references.Add(MetadataReference.CreateFromFile(refs));
            return references;
        }
    }
}
