using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface ISerializer
{
    void Serialize(object target, string filePath);
    bool TryDeserialize<T>(string filePath, out T target);
    bool TryDeserialize(string filePath, Type type, out object target);
    Func<Task<T>> GetAsyncDeserializationMethod<T>(string filePath);
}
