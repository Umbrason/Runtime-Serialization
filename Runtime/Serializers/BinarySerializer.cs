using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

public class BinarySerializer : ISerializer
{
    private static BinaryFormatter binaryFormatter = new BinaryFormatter();
    public Func<Task<T>> GetAsyncDeserializationMethod<T>(string filePath)
    {
        return async () =>
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                var task = new Task<T>(() => (T)binaryFormatter.Deserialize(fileStream));
                task.Start();
                return await task;
            }
        };
    }

    public void Serialize(object target, string filePath)
    {
        using (var fileStream = File.Exists(filePath) ? File.OpenWrite(filePath) : File.Create(filePath))
            binaryFormatter.Serialize(fileStream, target);
    }


    public bool TryDeserialize(string filePath, Type type, out object target)
    {
        if (!File.Exists(filePath))
            return false;
        using (var stream = File.OpenRead(filePath))
        {
            var readObject = binaryFormatter.Deserialize(stream);
            if (readObject.GetType() != type)
            {
                target = default;
                return false;
            }
            target = readObject;
            return true;
        }
    }

    public bool TryDeserialize<T>(string filePath, out T target)
    {
        var success = TryDeserialize(filePath, typeof(T), out object data);
        target = success ? (T)data : default;
        return success;

    }
}
