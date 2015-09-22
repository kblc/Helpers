using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helpers.Linq;

namespace Helpers.Test
{
    [TestClass]
    public class HelpersLinqTest
    {
        [TestMethod]
        public void GenericEqualityComparer_Equals()
        {
            var strs = new string[] { "Abr", "Adr", "Afr" };
            var gcc0 = new GenericEqualityComparer<string>(i => i[0], withoutHash: true);
            var gcc1 = new GenericEqualityComparer<string>(i => i[1], withoutHash: true);
            var gcc2 = new GenericEqualityComparer<string>(i => i[2]);

            var cnt0 = strs.Distinct(gcc0).Count();
            var cnt1 = strs.Distinct(gcc1).Count();
            var cnt2 = strs.Distinct(gcc2).Count();

            Assert.AreEqual(1, cnt0, "All strings must equals by first character");
            Assert.AreEqual(3, cnt1, "All strings must not equals by second character");
            Assert.AreEqual(3, cnt2, "All strings must not equals by third character becase we not disable to use hash");
        }

        [TestMethod]
        public void GenericComperer_Compare()
        {
            var strs = new string[] { "a", "aa", "aaa" };
            // Descending sort by length
            var gcc = new GenericComperer<string>(i => 100 - i.Length);
            var resArray = strs.OrderBy(s => s, gcc).ToArray();
            Assert.AreEqual(strs[2], resArray[0], "Items must equals");
            Assert.AreEqual(strs[1], resArray[1], "Items must equals");
            Assert.AreEqual(strs[0], resArray[2], "Items must equals");
        }

        [TestMethod]
        public void ConcatExtension_Concat()
        {
            var str1 = "123";
            var str2 = "456";
            var str3 = "789";
            var res0 = new string[] { str1, str2, str3 }.Concat(i => i);
            Assert.AreEqual(str1+ str2+ str3, res0);

            var res1 = new string[] { str1, str2, str3 }.Concat(i => i, ";");
            Assert.AreEqual(str1 + ";" + str2 + ";" + str3, res1);
        }
    }
}
