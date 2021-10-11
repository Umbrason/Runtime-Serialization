using System;
using System.Collections.Generic;

namespace Game
{
    public static class SerializationUtil
    {
        public static string IEnumerableToString<T>(IEnumerable<T> enumerable)
        {
            string output = "[";
            if (enumerable != null)
                foreach (T item in enumerable)
                {
                    output += $"{item?.ToString()}, ";
                }
            else output += "null";
            if(output.Length >= 2)
                output.Remove(output.Length - 2, 2);
            output += "]";
            return output;
        }
    }
}