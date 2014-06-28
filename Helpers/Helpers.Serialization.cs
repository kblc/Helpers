using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Helpers.Serialization
{
    /// <summary>
    /// Interface for deserialization for objects
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// Invoke after deserialization ends
        /// </summary>
        void OnDeserialized();
    }

    public static partial class Extensions
    {
        private static void CheckTypeForInterfaceImplementation(Type typeOfObject)
        {
            if (typeOfObject.GetInterface(typeof(ISerializable).Name) == null)
                throw new Exception("This object not implement Helpers.Serialization.ISerializable interface");
        }

        /// <summary>
        /// Serialize this current object to byte array
        /// </summary>
        /// <typeparam name="type">Type of object to serialize</typeparam>
        /// <param name="source">Object to serialize</param>
        /// <param name="compressed">Use GZip compression</param>
        /// <returns>Byte array of serialized object</returns>
        public static byte[] SerializeToBytes<type>(this type source, bool compressed)
        {
            CheckTypeForInterfaceImplementation(source.GetType());

            byte[] result = new byte[] { };
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, (type)source);
                if (compressed)
                    using (MemoryStream compressedStream = new MemoryStream())
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedStream, CompressionMode.Compress))
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.CopyTo(compressionStream);
                        }
                        result = compressedStream.ToArray();
                    }
                else
                    result = stream.ToArray();
            }
            return result;
        }

        /// <summary>
        /// Serialize this current object to Base64 string
        /// </summary>
        /// <typeparam name="type">Type of object to serialize</typeparam>
        /// <param name="source">Object to serialize</param>
        /// <param name="compressed">Use GZip compression</param>
        /// <returns>Base64 string of object serialization</returns>
        public static string SerializeToBase64<type>(this type source, bool compressed)
        {
            var bytes = SerializeToBytes(source, compressed);
            return System.Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Serialize this current object to XML string
        /// </summary>
        /// <typeparam name="type">Type of object to serialize</typeparam>
        /// <param name="source">Object to serialize</param>
        /// <param name="clean">Is XML result clean</param>
        /// <returns>XML string of object serialization</returns>
        public static string SerializeToXML(this object source, bool clean = true)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            Type sourceType = source.GetType();

            CheckTypeForInterfaceImplementation(sourceType);

            string result = string.Empty;

            System.Xml.Serialization.XmlSerializer s = new System.Xml.Serialization.XmlSerializer(sourceType);
            using (MemoryStream stream = new MemoryStream())
            {
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Indent = !clean,
                    OmitXmlDeclaration = true,
                    Encoding = Encoding.UTF8
                };
                XmlWriter writer = XmlWriter.Create(stream, settings);

                s.Serialize(writer, source, ns);

                //s.Serialize(stream, source, ns);
                stream.Seek(0, SeekOrigin.Begin);
                result = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            }
            return result;
        }

        /// <summary>
        /// Deserialization byte array to selected object. <i>Using: typeof(item).DeserializeFromBytes(...)</i>
        /// </summary>
        /// <typeparam name="type">Object type</typeparam>
        /// <param name="typeOfObject">Object type</param>
        /// <param name="bytes">Byte array to deserialization</param>
        /// <param name="compressed">Is stream was compressed</param>
        /// <param name="result">Deserializated object</param>
        public static void DeserializeFromBytes<type>(this Type typeOfObject, byte[] bytes, bool compressed, out type result)
        {
            CheckTypeForInterfaceImplementation(typeOfObject);

            object res = null;
            using (var compressedStream = new MemoryStream(bytes))
            {
                if (compressed)
                using (var decompressStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    res = (new BinaryFormatter()).Deserialize(decompressStream);
                } else
                    res = (new BinaryFormatter()).Deserialize(compressedStream);

                CheckTypeForInterfaceImplementation(res.GetType());

                result = (type)res;
                (result as ISerializable).OnDeserialized();
            }
        }

        /// <summary>
        /// Deserialization base64 string to selected object. <i>Using: typeof(item).DeserializeFromBase64(...)</i>
        /// </summary>
        /// <typeparam name="type">Object type</typeparam>
        /// <param name="typeOfObject">Object type</param>
        /// <param name="base64String">Base64 string to deserialization</param>
        /// <param name="compressed">Is stream was compressed</param>
        /// <param name="result">Deserializated object</param>
        public static void DeserializeFromBase64<type>(this Type typeOfObject, string base64String, bool compressed, out type result)
        {
            var bytes = System.Convert.FromBase64String(base64String);
            DeserializeFromBytes<type>(typeOfObject, bytes, compressed, out result);
        }

        /// <summary>
        /// Deserialization XML string to selected object. <i>Using: typeof(item).DeserializeFromXML(...)</i>
        /// </summary>
        /// <typeparam name="type">Object type</typeparam>
        /// <param name="typeOfObject">Object type</param>
        /// <param name="xml">Xml string to deserialization</param>
        /// <param name="result">Deserializated object</param>
        public static void DeserializeFromXML<type>(this Type typeOfObject, string xml, out type result)
        {
            CheckTypeForInterfaceImplementation(typeOfObject);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                System.Xml.Serialization.XmlSerializer s = new System.Xml.Serialization.XmlSerializer(typeOfObject);
                var res = s.Deserialize(stream);

                CheckTypeForInterfaceImplementation(res.GetType());

                result = (type)res;
                (result as ISerializable).OnDeserialized();
            }
        }
    }
}
