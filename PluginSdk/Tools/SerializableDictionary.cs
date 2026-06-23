using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using PluginSdk.Config;

namespace PluginSdk.Tools
{
    /// <summary>
    /// A <see cref="Dictionary{TKey,TValue}"/> that supports XML serialization
    /// via <see cref="XmlSerializer"/>. The default XmlSerializer cannot
    /// serialize the generic <see cref="Dictionary{TKey,TValue}"/>, so this
    /// type is required for any dictionary-typed configuration option.
    ///
    /// <para>
    /// Allowed key types: <c>string</c>, <c>int</c>, <c>long</c>. Allowed
    /// value types: the scalar types listed in <see cref="PluginConfig"/>, an
    /// enum, or a user-defined struct.
    /// </para>
    /// </summary>
    [XmlRoot("Dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        public SerializableDictionary() { }

        public SerializableDictionary(IDictionary<TKey, TValue> source) : base(source) { }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            var keySerializer = new XmlSerializer(typeof(TKey));
            var valueSerializer = new XmlSerializer(typeof(TValue));

            bool isEmpty = reader.IsEmptyElement;
            reader.Read();
            if (isEmpty) return;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("Entry");

                reader.ReadStartElement("Key");
                var key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("Value");
                var value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadEndElement(); // </Entry>
                reader.MoveToContent();

                Add(key, value);
            }
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            var keySerializer = new XmlSerializer(typeof(TKey));
            var valueSerializer = new XmlSerializer(typeof(TValue));

            foreach (var kvp in this)
            {
                writer.WriteStartElement("Entry");

                writer.WriteStartElement("Key");
                keySerializer.Serialize(writer, kvp.Key);
                writer.WriteEndElement();

                writer.WriteStartElement("Value");
                valueSerializer.Serialize(writer, kvp.Value);
                writer.WriteEndElement();

                writer.WriteEndElement(); // </Entry>
            }
        }
    }
}
