using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SerializationManager
{
    static ISerializer serializer;
    static SerializationManager()
    {
        serializer = new XmlSerializer();
    }

    public static void Serialize(object target, string filePath) => serializer?.Serialize(target, filePath);

    public static bool TryDeserialize<T>(string filePath, out T target) => serializer.TryDeserialize<T>(filePath, out target);

    public static bool TryDeserialize(string filePath, Type type, out object target) => serializer.TryDeserialize(filePath, type, out target);

}
