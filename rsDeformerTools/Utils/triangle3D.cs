using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Deformer
{
    [System.Serializable]
    public struct Triangle
    {
        public Vector3 pos0;
        public Vector3 pos1;
        public Vector3 pos2;
        public Vector3 normal;
    }
}
