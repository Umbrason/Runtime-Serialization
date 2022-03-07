using System;
using System.Threading.Tasks;

public static class SerializationManager
{
    private static ISerializer serializer = new BinarySerializer();

    public static void Serialize(object target, string filePath) => serializer?.Serialize(target, filePath);
    public static bool TryDeserialize<T>(string filePath, out T target) => serializer.TryDeserialize<T>(filePath, out target);
    public static bool TryDeserialize(string filePath, Type type, out object target) => serializer.TryDeserialize(filePath, type, out target);
    public static async Task<T> DeserializeAsync<T>(string filePath)
    {
        return await serializer.GetAsyncDeserializationMethod<T>(filePath)();
    }
}
