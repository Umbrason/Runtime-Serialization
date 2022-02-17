using System;
using System.IO;
using System.Xml.Serialization;
using System.Threading.Tasks;

public class XmlSerializer : ISerializer
{
    public void Serialize(object target, string filePath)
    {
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        FileStream file;
        if (File.Exists(filePath))
            file = File.OpenWrite(filePath);
        else file = File.Create(filePath);
        file.SetLength(0);
        XmlSerializerFactory factory = new XmlSerializerFactory();
        System.Xml.Serialization.XmlSerializer serializer = factory.CreateSerializer(target.GetType());
        serializer.Serialize(file, target);
        file.Close();
    }

    public bool TryDeserialize<T>(string filePath, out T target)
    {
        if (TryDeserialize(filePath, typeof(T), out object data) && data is T)
        {
            target = (T)data;
            return true;
        }
        target = default;
        return false;
    }

    public bool TryDeserialize(string filePath, Type type, out object target)
    {
        if (File.Exists(filePath))
        {
            var file = File.Open(filePath, FileMode.Open);
            var factory = new XmlSerializerFactory();
            var serializer = factory.CreateSerializer(type);
            object data = serializer.Deserialize(file);
            file.Close();
            target = Convert.ChangeType(data, type);
            if (target != null)
                return true;
        }
        target = default;
        return false;
    }
    public Func<Task<T>> GetAsyncDeserializationMethod<T>(string filePath) where T : new() => async () =>
        {
            StreamReader reader = File.OpenText(filePath);
            string text = await reader.ReadToEndAsync();

            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            TextReader stringReader = new StringReader(text);
            object data = serializer.Deserialize(stringReader);
            reader.Close();
            return (T)data;
        };
}