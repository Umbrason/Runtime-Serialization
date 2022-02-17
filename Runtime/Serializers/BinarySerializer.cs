using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

public class BinarySerializer : ISerializer
{
    private BinaryFormatter binaryFormatter = new BinaryFormatter();
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
        using (var fileStream = File.OpenWrite(filePath))
            binaryFormatter.Serialize(fileStream, target);
    }

    public bool TryDeserialize<T>(string filePath, out T target)
    {
        using (var stream = File.OpenRead(filePath))
        {
            var readObject = binaryFormatter.Deserialize(stream);
            if (!(readObject is T))
            {
                target = default;
                return false;
            }
            target = (T)readObject;
            return true;
        }
    }

    public bool TryDeserialize(string filePath, Type type, out object target)
    {
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
}
