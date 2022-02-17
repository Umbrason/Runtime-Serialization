using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable, ISerializable
{
    public SerializableDictionary() { }
    protected SerializableDictionary(SerializationInfo info, StreamingContext context)
    {
        var count = info.GetInt32("C");
        for (int i = 0; i < count; i++)
        {
            var key = (TKey)info.GetValue("K", typeof(TKey));
            var value = (TValue)info.GetValue("V", typeof(TValue));
            this.Add(key, value);
        }
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("C", this.Count);
        foreach (var pair in this)
        {
            info.AddValue("K", pair.Key);
            info.AddValue("V", pair.Value);
        }
    }


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

[Serializable]
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
