using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;


namespace CoreTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CheckDirAccessTest()
        {
            string pathDirectory = @"c:\Windows\system32";
            Assert.IsFalse(FileSystem.CheckDirectoryAccess(pathDirectory));
        }
    }
}
