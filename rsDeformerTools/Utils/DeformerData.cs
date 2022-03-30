using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Deformer
{
    [System.Serializable]
    public class DeformerData
    {
        [SerializeField] private string _deformerName;
        [SerializeField] private int _vertCount;
        [SerializeField] StringWeightmapDictionary _weightmaps;
        [SerializeField] public string name { get { return _deformerName; } }

        public DeformerData(string name,
                            int vertCount)
        {
            _deformerName = name;
            _vertCount = vertCount;
            if (_weightmaps == null) _weightmaps = new StringWeightmapDictionary();
        }

        public void InitalizeWeightmap(string name)
        {
            string fullPath = name;

            if (_weightmaps == null) _weightmaps = new StringWeightmapDictionary();

            // if weightmap exists and vertcount same return
            if (_weightmaps.ContainsKey(fullPath) && _weightmaps[fullPath].weights.Length == _vertCount)
            {
                return;
            }

            Debug.Log("Initalizing Weightmap " + fullPath);

            // create weightmap object
            if (!_weightmaps.ContainsKey(fullPath))
                _weightmaps.Add(fullPath, new Weightmap(fullPath));
            Weightmap wmap = _weightmaps[fullPath];

            wmap.weights = new float[_vertCount];
            wmap.vertexColor = new Color[_vertCount];
            wmap.cacheWeights = new float[_vertCount];

            for (int i = 0; i < wmap.weights.Length; i++)
            {
                wmap.cacheWeights[i] = 1.0f;
                wmap.weights[i] = 1.0f;
                wmap.vertexColor[i] = Color.black;
            }
        }

        public void CacheWeightMap(string name)
        {
            Weightmap wmap;
            if (_weightmaps.TryGetValue(name, out wmap))
            {
                wmap.CacheWeights();
            }
        }

        public Dictionary<string, Weightmap> Weightmap { get { return _weightmaps; } }
        public List<string> keyList { get { return new List<string>(_weightmaps.Keys); } }
        public List<Weightmap> Weightmaps { get { return new List<Weightmap>(_weightmaps.Values); } }
        public List<string> weightmapNames
        {
            get
            {
                List<string> names = new List<string>();
                for (int i = 0; i < keyList.Count; i++)
                {
                    names.Add(_deformerName + "_" + keyList[i]);
                }
                return names;
            }
        }

        public Weightmap GetWeightmap(string name)
        {
            if (!_weightmaps.ContainsKey(name)) return null;
            return _weightmaps[name];
        }
    }
}