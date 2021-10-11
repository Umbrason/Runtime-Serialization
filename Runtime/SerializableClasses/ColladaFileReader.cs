using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using grendgine_collada;

public class ColladaFileReader
{
    public List<MeshData> meshes;
    public List<SkeletonData> skeletons;
    public string filename;
    public Matrix4x4 upAxisBasedMatrix;

    private enum CoordinateHand
    {
        Right, Left
    }
    private CoordinateHand coordinateHand;
    private enum UpAxis
    {
        X_UP, Y_UP, Z_UP
    }
    private UpAxis upAxis;
    public bool flipFaces;

    public void ReadFileContent(string filePath, bool readMeshes = true, bool readSkeletons = true)
    {
        Grendgine_Collada Collada = Grendgine_Collada.Grendgine_Load_File(filePath);
        upAxisBasedMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
        filename = filePath.Split('\\').Last();
        switch (Collada.Asset.Up_Axis)
        {
            case "Z_UP":
                upAxis = UpAxis.Z_UP;
                //flipFaces = true;
                //upAxisBasedMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90, 0, 0), new Vector3(-1, 1, 1));                    
                break;
            case "Y_UP":
                upAxis = UpAxis.Y_UP;
                break;
            case "X_UP":
                upAxis = UpAxis.X_UP;
                //flipFaces = true;
                break;
        }
        if (readSkeletons)
            ReadSkeletons(Collada);
        if (readMeshes)
            ReadMeshData(Collada);
    }

    private void ReadMeshData(Grendgine_Collada Collada)
    {
        if (Collada.Library_Geometries == null)
            return;
        meshes = new List<MeshData>();
        foreach (Grendgine_Collada_Geometry geo in Collada.Library_Geometries.Geometry)
        {
            Grendgine_Collada_Node node = Collada.Library_Visual_Scene.Visual_Scene[0].Node.SingleOrDefault((Grendgine_Collada_Node n) => n.ID == geo.Name);
            float[] matrixVal = node?.Matrix?.Single((Grendgine_Collada_Matrix m) => m.sID == "transform")?.Value();
            Matrix4x4 matrix = matrixVal == null ? default : new Matrix4x4(
                new Vector4(matrixVal[0], matrixVal[4], matrixVal[8], matrixVal[12]),
                new Vector4(matrixVal[1], matrixVal[5], matrixVal[9], matrixVal[13]),
                new Vector4(matrixVal[2], matrixVal[6], matrixVal[10], matrixVal[14]),
                new Vector4(matrixVal[3], matrixVal[7], matrixVal[11], matrixVal[15])
            );

            Vector3 pivot = matrix == null ? Vector3.zero : ConvertCoordinateSystem(matrix.Position());
            Dictionary<string, Grendgine_Collada_Source> sources = new Dictionary<string, Grendgine_Collada_Source>();
            foreach (Grendgine_Collada_Source source in geo.Mesh.Source)
                sources.Add(source.ID, source);

            if (geo.Mesh.Triangles == null)
                Debug.LogError("Geometry contains no triangles. make sure the mesh is triangulated and the Colladafile uses triangles and not polylists");

            BoneWeight[] boneWeightData = null;
            string[] joints = null;

            Grendgine_Collada_Controller Collada_controller = Collada.Library_Controllers?.Controller.SingleOrDefault((Grendgine_Collada_Controller c) => c.Skin.meshSource == '#' + geo.ID);
            if (Collada_controller != null)
            {
                var Collada_weights = Collada_controller?.Skin.Vertex_Weights;
                var weightSourceID = Collada_weights?.Input.Single((Grendgine_Collada_Input_Shared input) => input.Semantic == Grendgine_Collada_Input_Semantic.WEIGHT).source.Remove(0, 1);
                var Collada_WeightSource = Collada_controller?.Skin.Source.SingleOrDefault((Grendgine_Collada_Source s) => s.ID == weightSourceID);
                boneWeightData = ReadVertexWeights(Collada_weights, Collada_WeightSource.Float_Array);
            }

            foreach (Grendgine_Collada_Triangles triangleArray in geo.Mesh.Triangles)
            {
                //verts
                Grendgine_Collada_Source vertexSource = null;
                Grendgine_Collada_Source normalSource = null;
                Grendgine_Collada_Source uvSource = null;
                foreach (Grendgine_Collada_Input_Shared input in triangleArray.Input)
                {
                    switch (input.Semantic)
                    {
                        case Grendgine_Collada_Input_Semantic.VERTEX:
                            sources.TryGetValue(geo.Mesh.Vertices.Input[0].source.Remove(0, 1), out vertexSource);
                            break;
                        case Grendgine_Collada_Input_Semantic.NORMAL:
                            sources.TryGetValue(input.source.Remove(0, 1), out normalSource);
                            break;
                        case Grendgine_Collada_Input_Semantic.TEXCOORD:
                            sources.TryGetValue(input.source.Remove(0, 1), out uvSource);
                            break;
                    }
                }

                float[] vertexData = vertexSource.Float_Array.Value();
                int vertexCount = triangleArray.Count * 3;
                Vector3[] vertices = new Vector3[vertexCount];
                Vector2[] uv0 = new Vector2[vertexCount];
                float[] uvData = uvSource.Float_Array.Value();

                int[] triangleSourceArray = triangleArray.P.Value();
                int vertexOffset = triangleArray.Input.Single((Grendgine_Collada_Input_Shared item) => item.Semantic == Grendgine_Collada_Input_Semantic.VERTEX).Offset;
                int normalOffset = triangleArray.Input.Single((Grendgine_Collada_Input_Shared item) => item.Semantic == Grendgine_Collada_Input_Semantic.NORMAL).Offset;
                int uvOffset = triangleArray.Input.Single((Grendgine_Collada_Input_Shared item) => item.Semantic == Grendgine_Collada_Input_Semantic.TEXCOORD).Offset;
                int stride = triangleArray.Input.Length;
                int triangleCount = triangleArray.Count;
                int[] triangles = new int[triangleCount * 3];

                float[] normalData = normalSource.Float_Array.Value();
                Vector3[] normals = new Vector3[vertexCount];
                BoneWeight[] boneWeights = new BoneWeight[vertexCount];

                for (int t = 0; t < triangleCount * 3; t++)
                {
                    int sourceIndex = t * stride;

                    int vertexIndex = triangleSourceArray[sourceIndex + vertexOffset];
                    vertices[t] = new Vector3(
                        vertexData[vertexIndex * 3 + 0],
                        vertexData[vertexIndex * 3 + 1],
                        vertexData[vertexIndex * 3 + 2]
                    );
                    vertices[t] = vertices[t].RoundToDigits(5);
                    vertices[t] -= pivot;
                    vertices[t] = ConvertCoordinateSystem(vertices[t]);
                    vertices[t] += pivot;
                    triangles[t] = t;

                    int normalIndex = triangleSourceArray[sourceIndex + normalOffset];
                    normals[t] = new Vector3(
                    normalData[normalIndex * 3],
                    normalData[normalIndex * 3 + 1],
                    normalData[normalIndex * 3 + 2]);
                    normals[t] = normals[triangles[sourceIndex / stride]];
                    normals[t] = normals[t].RoundToDigits(5);
                    normals[t] = ConvertCoordinateSystem(normals[t]);

                    int uvIndex = triangleSourceArray[sourceIndex + uvOffset];
                    uv0[triangles[sourceIndex / stride]] = new Vector2(
                        uvData[uvIndex * 2],
                        uvData[uvIndex * 2 + 1]
                    );
                    uv0[triangles[sourceIndex / stride]] = uv0[triangles[sourceIndex / stride]].RoundToDigits(5);
                    boneWeights[t] = boneWeightData == null ? default : boneWeightData[vertexIndex];
                }
                if (flipFaces)
                    triangles = triangles.Reverse().ToArray();
                meshes.Add(new MeshData(geo.Name, vertices, triangles, uv0, normals, boneWeights, joints));
            }
        }
    }

    private void ReadSkeletons(Grendgine_Collada Collada)
    {
        skeletons = new List<SkeletonData>();
        List<Grendgine_Collada_Node> armatureNodes = Collada.Library_Visual_Scene.Visual_Scene[0].Node.Where((Grendgine_Collada_Node node) => IsArmature(node)).ToList();

        foreach (var controller in Collada.Library_Controllers.Controller)
        {
            var n = armatureNodes.Single((x) => x.Name.Equals(controller.Name));
            SkeletonData skele = new SkeletonData();
            skele.name = controller.Name;
            var jointSourceID = controller.Skin.Joints.Input.Single((Grendgine_Collada_Input_Unshared input) => input.Semantic == Grendgine_Collada_Input_Semantic.JOINT).source.Remove(0, 1);
            var joints = controller.Skin.Source.Single((Grendgine_Collada_Source source) => source.ID == jointSourceID).Name_Array.Value();
            Queue<SkeletonData.Bone> rootBones = new Queue<SkeletonData.Bone>();
            foreach (Grendgine_Collada_Node jointNode in n.node.Where((Grendgine_Collada_Node jn) => jn.Type == Grendgine_Collada_Node_Type.JOINT))
            {
                SkeletonData.Bone bone = BoneFromNode(jointNode, joints, ConvertCoordinateSystem(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one)));
                rootBones.Enqueue(bone);
            }
            skele.RootBone = rootBones.Dequeue();
            #region multiple Root Bones
            // more than one root bone?
            /*while (rootBones.Count > 0)
            {
                SkeletonData.Bone bone = rootBones.Dequeue();
                Vector3 position = bone.localTransformMatrix.Position();
                position = ConvertCoordinateSystem(position);
                bone.localTransformMatrix *= Matrix4x4.Translate(position - bone.localTransformMatrix.Position());
                bone.localTransformMatrix *= skele.RootBone.localTransformMatrix;
                skele.RootBone.children.Add(bone);
            }*/
            #endregion
            skele.boneNames = joints.ToArray();
            skeletons.Add(skele);
        }
    }

    private SkeletonData.Bone BoneFromNode(Grendgine_Collada_Node node, string[] joints, Matrix4x4 parentMatrix)
    {
        var bone = new SkeletonData.Bone();
        bone.name = node.Name;
        var m = node.Matrix[0];
        float[] matrixVal = m.Value();
        bone.boneIndex = joints.ToList().IndexOf(System.Text.RegularExpressions.Regex.Replace(bone.name, @"[^a-zA-Z0-9]+", "_"));
        bone.transformMatrix = parentMatrix * new Matrix4x4(
            new Vector4(matrixVal[0], matrixVal[4], matrixVal[8], matrixVal[12]),
            new Vector4(matrixVal[1], matrixVal[5], matrixVal[9], matrixVal[13]),
            new Vector4(matrixVal[2], matrixVal[6], matrixVal[10], matrixVal[14]),
            new Vector4(matrixVal[3], matrixVal[7], matrixVal[11], matrixVal[15])
        );

        var technique = node.Extra[0].Technique.Single((Grendgine_Collada_Technique t) => t.profile == "blender");
        foreach (var element in technique.Data)
        {
            switch (element.Attributes.GetNamedItem("sid").Value)
            {
                case "roll":
                    bone.roll = float.Parse(element.ChildNodes[0].Value, System.Globalization.NumberFormatInfo.InvariantInfo);
                    break;
                case "tip_x":
                    bone.tip.x = float.Parse(element.ChildNodes[0].Value, System.Globalization.NumberFormatInfo.InvariantInfo);
                    break;
                case "tip_y":
                    bone.tip.y = float.Parse(element.ChildNodes[0].Value, System.Globalization.NumberFormatInfo.InvariantInfo);
                    break;
                case "tip_z":
                    bone.tip.z = float.Parse(element.ChildNodes[0].Value, System.Globalization.NumberFormatInfo.InvariantInfo);
                    break;
            }
        }
        if (bone.tip.magnitude > .05f)
            bone.tip = bone.transformMatrix.Position() + ConvertCoordinateSystem(bone.tip);

        var children = new List<SkeletonData.Bone>();
        if (node.node != null)
            foreach (Grendgine_Collada_Node child in node.node.Where((Grendgine_Collada_Node n) => n.Type == Grendgine_Collada_Node_Type.JOINT))
                children.Add(BoneFromNode(child, joints, bone.transformMatrix));

        if (bone.tip.magnitude < .05f && children.Count > 0)
            bone.tip = children[0].transformMatrix.Position();
        bone.children = children;
        return bone;
    }

    private bool IsArmature(Grendgine_Collada_Node node)
    {
        if (node == null)
            return false;
        if (node.Type == Grendgine_Collada_Node_Type.JOINT)
            return true;
        if (node.node != null)
            foreach (Grendgine_Collada_Node child in node.node)
                if (IsArmature(child))
                    return true;
        return false;
    }

    private BoneWeight[] ReadVertexWeights(Grendgine_Collada_Vertex_Weights collada_Vertex_Weights, Grendgine_Collada_Float_Array collada_Weight_Array)
    {
        List<BoneWeight> boneWeights = new List<BoneWeight>();
        int[] vCounts = collada_Vertex_Weights.VCount.Value();
        int[] v = collada_Vertex_Weights.V.Value();
        int vIndex = 0;
        float[] weightArray = collada_Weight_Array.Value();
        for (int i = 0; i < collada_Vertex_Weights.Count; i++)
        {
            float[] tempBoneWeights = new float[4];
            int[] tempBoneIndices = new int[4];
            for (int j = 0; j < vCounts[i]; j++)
            {
                tempBoneIndices[j] = v[vIndex * 2 + 0];
                tempBoneWeights[j] = weightArray[v[vIndex * 2 + 1]];
                vIndex++;
            }

            BoneWeight bw = new BoneWeight()
            {
                weight0 = tempBoneWeights[0],
                weight1 = tempBoneWeights[1],
                weight2 = tempBoneWeights[2],
                weight3 = tempBoneWeights[3],
                boneIndex0 = tempBoneIndices[0],
                boneIndex1 = tempBoneIndices[1],
                boneIndex2 = tempBoneIndices[2],
                boneIndex3 = tempBoneIndices[3]
            };
            boneWeights.Add(bw);
        }
        return boneWeights.ToArray();
    }

    public static ColladaFileReader CreateFromFile(string filePath, bool readMeshes = true, bool readSkeletons = true)
    {
        ColladaFileReader reader = new ColladaFileReader();
        reader.ReadFileContent(filePath, readMeshes, readSkeletons);
        return reader;
    }

    private Matrix4x4 ConvertCoordinateSystem(Matrix4x4 m)
    {
        return Matrix4x4.TRS(
            ConvertCoordinateSystem(m.Position()),
            ConvertCoordinateSystem(m.rotation),
            m.lossyScale
            );
    }

    private Quaternion ConvertCoordinateSystem(Quaternion rot)
    {
        switch (upAxis)
        {
            case UpAxis.X_UP:
                rot *= Quaternion.Euler(0, -90, 0);
                break;
            case UpAxis.Z_UP:
                rot *= Quaternion.Euler(-90, 0, 0);
                break;
        }
        return rot;
    }

    private Vector3 ConvertCoordinateSystem(Vector3 v)
    {
        switch (upAxis)
        {
            case UpAxis.X_UP:
                v = new Vector3(-v.y, v.x, v.z);
                break;
            case UpAxis.Z_UP:
                v = new Vector3(v.x, v.z, -v.y);
                break;
        }
        return v;
    }

    public struct MeshData
    {
        public string name;
        public Vector3[] vertices;
        public Vector2[] uv0;
        public Vector3[] normals;
        public int[] triangles;
        public BoneWeight[] boneWeights;
        public string[] jointNames;

        public MeshData(string name, Vector3[] vertices, int[] triangles, Vector2[] uv0, Vector3[] normals, BoneWeight[] boneWeights = null, string[] joints = null)
        {
            this.name = name;
            this.vertices = vertices;
            this.uv0 = uv0;
            this.normals = normals;
            this.triangles = triangles;
            this.boneWeights = boneWeights;
            this.jointNames = joints;
        }

        public MeshData Merge(MeshData other)
        {
            var combined = new MeshData();
            combined.name = this.name;
            combined.vertices = this.vertices.Concat(other.vertices).ToArray();
            combined.uv0 = this.uv0.Concat(other.uv0).ToArray();
            combined.normals = this.normals.Concat(other.normals).ToArray();

            var combinedTriangles = triangles.ToList();
            foreach (int triangleIndex in other.triangles)
                combinedTriangles.Add(triangleIndex + triangles.Length);
            combined.triangles = combinedTriangles.ToArray();

            var boneMap = new Dictionary<string, int>();
            foreach (string joint in jointNames.Concat(other.jointNames))
                if (!boneMap.ContainsKey(joint))
                    boneMap.Add(joint, boneMap.Count);
            var combinedBoneWeights = boneWeights.ToList();
            foreach (BoneWeight weight in other.boneWeights)
            {
                combinedBoneWeights.Add(new BoneWeight()
                {
                    boneIndex0 = boneMap[other.jointNames[weight.boneIndex0]],
                    boneIndex1 = boneMap[other.jointNames[weight.boneIndex1]],
                    boneIndex2 = boneMap[other.jointNames[weight.boneIndex2]],
                    boneIndex3 = boneMap[other.jointNames[weight.boneIndex3]],
                    weight0 = weight.weight0,
                    weight1 = weight.weight1,
                    weight2 = weight.weight2,
                    weight3 = weight.weight3
                });
            }
            combined.boneWeights = combinedBoneWeights.ToArray();
            combined.jointNames = boneMap.Keys.ToArray();
            return combined;
        }
        ///<summary>Returns an empty MeshData</summary>
        public static MeshData Empty
        {
            get
            {
                return new MeshData()
                {
                    name = "empty",
                    vertices = new Vector3[0],
                    normals = new Vector3[0],
                    uv0 = new Vector2[0],
                    triangles = new int[0],
                    jointNames = new string[0],
                    boneWeights = new BoneWeight[0]
                };
            }
        }
        public static implicit operator string(MeshData meshData)
        {
            string output = meshData.name;
            output += "\nVertices:";
            output += SerializationUtil.IEnumerableToString<Vector3>(meshData.vertices);
            output += "\nTriangles:";
            output += SerializationUtil.IEnumerableToString<int>(meshData.triangles);
            output += "\nNormals:";
            output += SerializationUtil.IEnumerableToString<Vector3>(meshData.normals);
            output += "\nUV0:";
            output += SerializationUtil.IEnumerableToString<Vector2>(meshData.uv0);
            output += "\nJoints:";
            output += SerializationUtil.IEnumerableToString<string>(meshData.jointNames);
            output += "\nBoneWeights:";
            output += SerializationUtil.IEnumerableToString<BoneWeight>(meshData.boneWeights);
            return output;
        }
    }

    public struct SkeletonData
    {
        public string name;
        public Bone RootBone;
        public string[] boneNames;

        public struct Bone
        {
            public string name;
            public Matrix4x4 transformMatrix;
            public Vector3 tip;
            public float roll;
            public List<Bone> children;
            public int boneIndex;
        }
    }
}
public static class MathExtensions
{
    public static Vector3 RoundToDigits(this Vector3 value, int digits)
    {
        value *= Mathf.Pow(10, digits);
        value = Vector3Int.RoundToInt(value);
        value /= Mathf.Pow(10, digits);
        return value;
    }

    public static Vector2 RoundToDigits(this Vector2 value, int digits)
    {
        value *= Mathf.Pow(10, digits);
        value = Vector2Int.RoundToInt(value);
        value /= Mathf.Pow(10, digits);
        return value;
    }

    public static Vector3 Position(this Matrix4x4 matrix)
    {
        return matrix.GetColumn(3);
    }
}