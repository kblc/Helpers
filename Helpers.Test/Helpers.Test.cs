using System;
using Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helpers.Test
{
    [TestClass]
    public class HelpersTest
    {
        [TestMethod]
        public void Log()
        {
            string fileName = "testlog.log";
            Helpers.Log.LogFileName = fileName;
            Assert.AreEqual(fileName, Helpers.Log.LogFileName);
            Helpers.Log.Add("First test record.");
            Helpers.Log.Clear();
        }
    }
}
