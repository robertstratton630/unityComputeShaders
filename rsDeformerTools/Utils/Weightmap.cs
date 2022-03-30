using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deformer
{
    [System.Serializable]
    public class Weightmap
    {
        [SerializeField] public float[] weights = new float[0];
        [SerializeField] public float[] cacheWeights = new float[0];
        [SerializeField] public Color[] vertexColor = new Color[0];
        [SerializeField] public string name;


        public Weightmap(string _name)
        {
            name = _name;
        }

        public void CacheWeights()
        {
            cacheWeights = (float[])weights.Clone();
        }

        public Color[] GetVertexColorMap()
        {
            if (vertexColor.Length != weights.Length) vertexColor = new Color[weights.Length];

            for (int i = 0; i < weights.Length; i++)
            {
                vertexColor[i] = new Color(weights[i], weights[i], weights[i], 1);
            }
            return vertexColor;
        }
    }
}
