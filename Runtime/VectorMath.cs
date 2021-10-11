using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public class IntRect
{
    public int x, y, w, h;
    public Vector2Int Position { get { return new Vector2Int(x, y); } }
    public Vector2Int Size { get { return new Vector2Int(w, h); } }
    public Vector2Int minCorner { get { return Position; } }
    public Vector2Int maxCorner { get { return Size + Position; } }

    public IntRect(int x = 0, int y = 0, int w = 1, int h = 1)
    {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }
    public IntRect(Vector2Int minCorner, Vector2Int maxCorner) : this(minCorner.x, minCorner.y, maxCorner.x - minCorner.x, maxCorner.y - minCorner.y) { }
    public IntRect(Texture2D tex) : this(0, 0, tex.width, tex.height) { }
    public int Area { get { return w * h; } }
    public static implicit operator IntRect(Texture2D texture2D) => new IntRect(texture2D);
    public static implicit operator string(IntRect rect) => $"({rect.x}, {rect.y}, {rect.w}, {rect.h})";
    public override string ToString() => this;
    public static IntRect operator +(IntRect a, Vector2Int b) => new IntRect(a.x + b.x, a.y + b.y, a.w, a.h);
}
public static class VectorMath
{

    public static Vector3 Min(this IEnumerable<Vector3> vectors)
    {
        var result = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        vectors.Select((x) => result = Vector3.Min(x, result));
        return result;
    }
    public static Vector3 Max(this IEnumerable<Vector3> vectors)
    {
        var result = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);
        vectors.Select((x) => result = Vector3.Max(x, result));
        return result;
    }

    public static Vector2 Min(this IEnumerable<Vector2> vectors)
    {
        var result = new Vector3(Mathf.Infinity, Mathf.Infinity);
        vectors.Select((x) => result = Vector2.Min(x, result));
        return result;
    }
    public static Vector2 Max(this IEnumerable<Vector2> vectors)
    {
        var result = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);
        vectors.Select((x) => result = Vector2.Max(x, result));
        return result;
    }



    public static Vector3 RoundToDigits(Vector3 value, int digits)
    {
        value *= Mathf.Pow(10, digits);
        value = Vector3Int.RoundToInt(value);
        value /= Mathf.Pow(10, digits);
        return value;
    }

    public static Quaternion Rotation(this Matrix4x4 matrix)
    {
        return Quaternion.LookRotation(
            matrix.GetColumn(2),
            matrix.GetColumn(1)
        );
    }

    public static Vector2 Sum(this IEnumerable<Vector2> enumerable)
    {
        var sum = new Vector2();
        foreach (var v2 in enumerable)
            sum += v2;
        return sum;
    }

    public static Vector3 Position(this Matrix4x4 matrix)
    {
        return matrix.GetColumn(3);
    }

    public static Vector2 ComponentDivide(this Vector2 a, Vector2 b)
    {
        return new Vector2(
            a.x / b.x,
            a.y / b.y
        );
    }

    public static Vector3 ComponentDivide(this Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.x / b.x,
            a.y / b.y,
            a.z / b.z
        );
    }

    public static Vector3 Scale(this Matrix4x4 matrix)
    {
        return new Vector3(
            matrix.GetColumn(0).magnitude,
            matrix.GetColumn(1).magnitude,
            matrix.GetColumn(2).magnitude
        );
    }
}
