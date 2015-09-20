﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }
}