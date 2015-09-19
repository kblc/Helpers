using System;
using Helpers.CSV;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace Helpers.Test
{
    [TestClass]
    public class HelpersLogTest
    {
        [TestMethod]
        public void HelperLog_Log()
        {
            var sessionName = "test session";
            var logList = new List<string>();
            using (var log = Helpers.Log.Session(sessionName, s => logList.AddRange(s)))
            {
                log.Add("testData1");
                Assert.AreEqual(0, logList.Count);
            }
            Assert.AreEqual(5, logList.Count);
            Assert.AreEqual(true, logList.Any(s => s.Contains("testData1")));
        }
    }
}
