using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;
using System.Runtime.Serialization;

[Serializable]
public class OrderedSerializableDictionary<TKey, TValue> : ISerializable, IXmlSerializable, IEnumerable<KeyValuePair<TKey, TValue>>
{
    private List<TKey> keyOrder = new List<TKey>();
    private SerializableDictionary<TKey, TValue> serializableDictionary = new SerializableDictionary<TKey, TValue>();
    public int Count { get => keyOrder.Count; }
    public TKey[] Keys { get { return keyOrder.ToArray(); } }
    public TValue[] Values { get { return (from key in Keys select this[key]).ToArray(); } }

    public void Add(TKey key, TValue value)
    {
        serializableDictionary.Add(key, value);
        keyOrder.Add(key);
    }

    public void Remove(TKey key)
    {
        serializableDictionary.Remove(key);
        keyOrder.Remove(key);
    }

    public TValue this[int index]
    {
        get => serializableDictionary[keyOrder[index]];
        set => serializableDictionary[keyOrder[index]] = value;
    }
    public TValue this[TKey key]
    {
        get => serializableDictionary[key];
        set => serializableDictionary[key] = value;
    }

    public bool TryGetValue(TKey key, out TValue value) => serializableDictionary.TryGetValue(key, out value);

    public void SetOrder(IEnumerable<TKey> order)
    {
        var finalOrder = new List<TKey>();
        foreach (TKey key in order)
            if (keyOrder.Contains(key))
                finalOrder.Add(key);
        foreach (TKey key in keyOrder)
            if (!finalOrder.Contains(key))
                finalOrder.Add(key);
        keyOrder = finalOrder;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return (from x in keyOrder select new KeyValuePair<TKey, TValue>(x, serializableDictionary[x])).GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public OrderedSerializableDictionary() { }
    protected OrderedSerializableDictionary(SerializationInfo info, StreamingContext context)
    {
        var count = info.GetInt32("C");
        for (int i = 0; i < count; i++)
        {
            var key = (TKey)info.GetValue("K", typeof(TKey));
            var value = (TValue)info.GetValue("V", typeof(TValue));
            this.Add(key, value);
        }
    }
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("C", this.Count);
        foreach (var key in keyOrder)
        {
            info.AddValue("K", key, typeof(TKey));
            info.AddValue("V", this[key], typeof(TValue));
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

        foreach (TKey key in this.keyOrder)
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
