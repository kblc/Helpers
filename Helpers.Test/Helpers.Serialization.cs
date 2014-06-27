using System;
using Helpers.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Serialization;

namespace Helpers.Test
{
    [XmlRoot("SerializationClass")]
    public class SerializationFakeClassEx : SerializationFakeClass { }

    [Serializable]
    [XmlInclude(typeof(SerializationFakeClass))]
    [XmlRoot("SerializationClass")]
    public class SerializationFakeClass : ISerializable
    {
        public SerializationFakeClass()
        {
            FakeInt = 0;
        }

        [XmlAttribute("FakeInt")]
        public int FakeInt { get; set; }
        public void OnDeserialized()
        {
            Console.WriteLine("ISerializable.OnDeserialized(): serialization done");
        }
    }

    [TestClass]
    public class HelpersSerializationUnitTest
    {
        private void  SerializationBytesTest(bool compressed)
        {
            SerializationFakeClass obj = new SerializationFakeClass();
            obj.FakeInt = 2;
            var bytes = obj.SerializeToBytes(compressed);

            Console.WriteLine("Bytes array length: {0}", bytes.Length);
            Console.WriteLine("Bytes: {0}", bytes);

            SerializationFakeClass outObj;
            typeof(SerializationFakeClass).DeserializeFromBytes(bytes, compressed, out outObj);

            Assert.AreEqual(obj.FakeInt == outObj.FakeInt, true);
        }

        private void SerializationByesBase64Test(bool compressed)
        {
            SerializationFakeClass obj = new SerializationFakeClass();
            obj.FakeInt = 2;
            var base64String = obj.SerializeToBase64(compressed);

            Console.WriteLine("String length: {0}", base64String.Length);
            Console.WriteLine("String: {0}", base64String);

            SerializationFakeClass outObj;
            typeof(SerializationFakeClass).DeserializeFromBase64(base64String, compressed, out outObj);

            Assert.AreEqual(obj.FakeInt == outObj.FakeInt, true);
        }

        private void SerializationXMLTest()
        {
            SerializationFakeClass obj = new SerializationFakeClass();
            obj.FakeInt = 2;
            var xmlString = obj.SerializeToXML();

            Console.WriteLine(xmlString);

            SerializationFakeClass outObj;
            typeof(SerializationFakeClass).DeserializeFromXML(xmlString, out outObj);
            Assert.AreEqual(obj.FakeInt == outObj.FakeInt, true);
        }

        private void SerializationXMLExTest()
        {
            SerializationFakeClassEx obj = new SerializationFakeClassEx();
            obj.FakeInt = 2;
            //var xmlString = obj.SerializeToXML();
            var xmlString = Helpers.Serialization.Extensions.SerializeToXML(obj);

            Console.WriteLine(xmlString);

            SerializationFakeClassEx outObj;
            typeof(SerializationFakeClassEx).DeserializeFromXML(xmlString, out outObj);
            Assert.AreEqual(obj.FakeInt == outObj.FakeInt, true);
        }

        [TestMethod]
        public void SerializationBytesCompressed()
        {
            SerializationBytesTest(true);
        }

        [TestMethod]
        public void SerializationBytesDecompressed()
        {
            SerializationBytesTest(false);
        }

        [TestMethod]
        public void SerializationBase64Compressed()
        {
            SerializationByesBase64Test(true);
        }

        [TestMethod]
        public void SerializationBase64Decompressed()
        {
            SerializationByesBase64Test(false);
        }

        [TestMethod]
        public void SerializationXML()
        {
            SerializationXMLTest();
        }

        [TestMethod]
        public void SerializationXMLEx()
        {
            SerializationXMLExTest();
        }
    }
}
