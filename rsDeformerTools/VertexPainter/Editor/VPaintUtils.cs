using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using Deformer;

namespace Deformer
{
    public static class VPaintUtils
    {

        public static void Brush_Flood(float strength, Weightmap weightmap)
        {
            float[] weights = weightmap.weights;
            Color[] vertColors = weightmap.vertexColor;

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = strength;
                vertColors[i].r = weights[i];
                vertColors[i].g = weights[i];
                vertColors[i].b = weights[i];
            }
        }

        public static Color Brush_Add(int vertexID, float strength, Weightmap weightmap)
        {
            float[] weights = weightmap.weights;
            Color[] vertColors = weightmap.vertexColor;

            weights[vertexID] = Mathf.Clamp01(weights[vertexID] + Mathf.Clamp01(strength));
            vertColors[vertexID].r = weights[vertexID];
            vertColors[vertexID].g = weights[vertexID];
            vertColors[vertexID].b = weights[vertexID];

            return vertColors[vertexID];
        }

        public static Color Brush_Replace(int vertexID, float strength, Weightmap weightmap)
        {
            float[] weights = weightmap.weights;
            Color[] vertColors = weightmap.vertexColor;

            weights[vertexID] = Mathf.Clamp01(strength);
            vertColors[vertexID].r = weights[vertexID];
            vertColors[vertexID].g = weights[vertexID];
            vertColors[vertexID].b = weights[vertexID];

            return vertColors[vertexID];
        }

        public static Color Brush_Scale(int vertexID, float strength, Weightmap weightmap)
        {
            float[] weights = weightmap.weights;
            Color[] vertColors = weightmap.vertexColor;

            weights[vertexID] = weights[vertexID] * strength;
            vertColors[vertexID].r = weights[vertexID];
            vertColors[vertexID].g = weights[vertexID];
            vertColors[vertexID].b = weights[vertexID];
            vertColors[vertexID].a = 1.0f;
            return vertColors[vertexID];
        }

        public static float LinearFalloff(float distance, float brushSize)
        {
            return Mathf.Clamp01(1 - distance / brushSize);
        }

        public static Material GetVertexPaintMatertial()
        {
            Material vertexMat; 
            string name = "Materials/VertexColor_MAT";
            vertexMat = (Material)Resources.Load<Material>(name);
            return vertexMat;
        }
    }
}