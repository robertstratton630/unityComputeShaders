using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Deformer
{
    public class VertexPainter_menus : MonoBehaviour
    {
        [MenuItem("rsTools/VertexPainter", false, 10)]
        static void LaunchVertexPainter()
        {
            VertexPainter_window.LaunchVertexPainter();
        }
    }
}
