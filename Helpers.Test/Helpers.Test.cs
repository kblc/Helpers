using System;
using Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

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

        private class ConcurrencyTestClass
        {
            public string testString = string.Empty;
            public ConcurrencyTestClass() { }
        }

        [TestMethod]
        public void ConcurrencyObjects()
        {
            bool th1End = false;
            bool th2End = true;
            bool th1First = false;

            ConcurrencyObjects<ConcurrencyTestClass> co = new ConcurrencyObjects<ConcurrencyTestClass>();
            co.Max = 1;
            co.TimeToGetObject = new TimeSpan(0,0,10,0);
            var str1 = co.GetObject();
            str1.testString = "test string 1";
            var th = new Thread(() =>
            {
                Console.WriteLine("Wait thread start");
                Thread.Sleep(5*1000);
                Console.WriteLine("Wait thread end");
                co.ReturnObject(str1);
                Console.WriteLine("Object (1) returns to list");
                th1End = true;
                if (th2End == false)
                    th1First = true;
            });
            th.IsBackground = true;
            th.Start();

            th2End = false;
            Console.WriteLine("Try to get object (2)");
            var str2 = co.GetObject();
            Console.WriteLine(@"Object (2) getted : '{0}'", str1.testString);
            co.ReturnObject(str2);
            Console.WriteLine("Object (2) returns to list");
            th2End = true;

            Assert.AreEqual(th1First && th1End && th2End, true);
        }
    }
}
