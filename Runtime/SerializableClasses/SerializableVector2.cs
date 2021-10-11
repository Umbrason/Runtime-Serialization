using UnityEngine;

    [System.Serializable]
    public struct SerializableVector2
    {
        public float x, y;
        public SerializableVector2(Vector2 original)
        {
            this.x = original.x;
            this.y = original.y;            
        }
        public static implicit operator Vector2(SerializableVector2 value) { return new Vector2(value.x, value.y); }
        public static implicit operator SerializableVector2(Vector2 value) { return new SerializableVector2(value); }
    }
