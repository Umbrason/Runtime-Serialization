using UnityEngine;

    [System.Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;
        public SerializableVector3(Vector3 original)
        {
            this.x = original.x;
            this.y = original.y;
            this.z = original.z;
        }
        public static implicit operator Vector3(SerializableVector3 value) { return new Vector3(value.x, value.y, value.z); }
        public static implicit operator SerializableVector3(Vector3 value) { return new SerializableVector3(value); }        
    }
