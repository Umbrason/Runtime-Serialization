using System.Collections.Generic;
using System.Xml.Serialization;


public class XmlDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
{
    public void ReadXml(System.Xml.XmlReader reader)
    {
        var keySerializer = new System.Xml.Serialization.XmlSerializer(typeof(TKey));
        var valueSerializer = new System.Xml.Serialization.XmlSerializer(typeof(TValue));

        bool wasEmpty = reader.IsEmptyElement;
        reader.Read();

        if (wasEmpty)
            return;

        while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
        {
            reader.ReadStartElement("entry");

            reader.ReadStartElement("key");
            TKey key = (TKey)keySerializer.Deserialize(reader);
            reader.ReadEndElement();

            reader.ReadStartElement("value");
            TValue value = (TValue)valueSerializer.Deserialize(reader);
            reader.ReadEndElement();

            this.Add(key, value);

            reader.ReadEndElement();
            reader.MoveToContent();
        }
        reader.ReadEndElement();
    }

    public void WriteXml(System.Xml.XmlWriter writer)
    {
        var keySerializer = new System.Xml.Serialization.XmlSerializer(typeof(TKey));
        var valueSerializer = new System.Xml.Serialization.XmlSerializer(typeof(TValue));

        foreach (TKey key in this.Keys)
        {
            writer.WriteStartElement("entry");

            writer.WriteStartElement("key");
            keySerializer.Serialize(writer, key);
            writer.WriteEndElement();

            writer.WriteStartElement("value");
            TValue value = this[key];
            valueSerializer.Serialize(writer, value);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }

    public System.Xml.Schema.XmlSchema GetSchema() => null;

}

[System.Serializable]
struct Entry<TKey, TValue>
{
    TKey key;
    TValue value;
    public Entry(TKey key, TValue value)
    {
        this.key = key;
        this.value = value;
    }
}
