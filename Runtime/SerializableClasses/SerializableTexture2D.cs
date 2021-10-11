using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Xml.Serialization;


public class SerializableTexture2D : IXmlSerializable
{
    private Texture2D texture;

    public int width { get { return texture != null ? texture.width : 0; } }
    public int height { get { return texture != null ? texture.height : 0; } }
    public Vector2Int Dimensions { get { return new Vector2Int(width, height); } }
    public static implicit operator Texture2D(SerializableTexture2D serializableTexture2D) => serializableTexture2D == null ? null : serializableTexture2D.texture;
    public static implicit operator SerializableTexture2D(Texture2D tex) => new SerializableTexture2D { texture = tex };

    public static implicit operator Sprite(SerializableTexture2D serializableTexture2D) => serializableTexture2D == null ? null : Sprite.Create(serializableTexture2D, new Rect(0, 0, serializableTexture2D.width, serializableTexture2D.height), new Vector2(serializableTexture2D.width / 2f, serializableTexture2D.height / 2f));

    public SerializableTexture2D() { }
    public SerializableTexture2D(int width = 0, int height = 0) => texture = new Texture2D(width, height);

    public void ReadXml(System.Xml.XmlReader reader)
    {
        var imageDeserializer = new System.Xml.Serialization.XmlSerializer(typeof(byte[]));
        var intDeserializer = new System.Xml.Serialization.XmlSerializer(typeof(int));

        reader.ReadStartElement();

        reader.ReadStartElement("value");
        byte[] value = (byte[])imageDeserializer.Deserialize(reader);
        reader.ReadEndElement();

        reader.ReadStartElement("width");
        int height = (int)intDeserializer.Deserialize(reader);
        reader.ReadEndElement();

        reader.ReadStartElement("height");
        int width = (int)intDeserializer.Deserialize(reader);
        reader.ReadEndElement();

        reader.ReadEndElement();

        texture = new Texture2D(width, height, TextureFormat.RGBA32, true, false);
        texture.filterMode = FilterMode.Point;

        ImageConversion.LoadImage(texture, value);
    }

    public void WriteXml(System.Xml.XmlWriter writer)
    {
        var imageSerializer = new System.Xml.Serialization.XmlSerializer(typeof(byte[]));
        var sizeSerializer = new System.Xml.Serialization.XmlSerializer(typeof(int));

        if (texture)
        {
            writer.WriteStartElement("value");
            imageSerializer.Serialize(writer, ImageConversion.EncodeToPNG(texture));
            writer.WriteEndElement();

            writer.WriteStartElement("width");
            sizeSerializer.Serialize(writer, texture.width);
            writer.WriteEndElement();

            writer.WriteStartElement("height");
            sizeSerializer.Serialize(writer, texture.height);
            writer.WriteEndElement();
        }
    }

    public System.Xml.Schema.XmlSchema GetSchema() => null;
}

