using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CheckType = Core.FileSystem.CheckType;

namespace CoreTest
{
    [TestClass]
    public class FileSystem
    {
        [TestMethod]
        public void CheckPermission_Test()
        {
            string pathDirectory = @"c:\Windows\system32";
            string pathFile = @"c:\Windows\system32\cmd.exe";
            Assert.IsFalse(Core.FileSystem.CheckPermission(pathDirectory,CheckType.Directory));
            Assert.IsFalse(Core.FileSystem.CheckPermission(pathFile,CheckType.File));
        }

        [TestMethod]
        public void GetFileSize_Test()
        {
            string fileName = @"F:\twitch\Projects\xTerminal\Release\paint.net.4.2.16.install.zip";
            double fileSize = 12.28;
            double roundCheck = Math.Round(Double.Parse(Core.FileSystem.GetFileSize(fileName, true)),2);
            Assert.AreEqual(roundCheck, fileSize);
        }
    }
}
