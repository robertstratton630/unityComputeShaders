using UnityEngine;


namespace Deformer
{
    [System.Serializable]
    public struct KDData
    {
        public Vector3 point;

        public int leftIdx;
        public int rightIdx;

        public int triangleIdx;

        public bool IsLeaf;
    }
}