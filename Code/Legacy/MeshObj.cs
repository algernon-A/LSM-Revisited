using UnityEngine;

namespace LoadingScreenMod
{
    internal sealed class MeshObj
    {
        internal string name;

        internal Vector3[] vertices;

        internal Color[] colors;

        internal Vector2[] uv;

        internal Vector3[] normals;

        internal Vector4[] tangents;

        internal BoneWeight[] boneWeights;

        internal Matrix4x4[] bindposes;

        internal int[][] triangles;
    }
}
