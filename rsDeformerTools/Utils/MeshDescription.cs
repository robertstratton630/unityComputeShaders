using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace Deformer
{

    [CreateAssetMenu(menuName = "rsTools/MeshObject")]
    public class MeshDescription : ScriptableObject
    {
        [SerializeField] Mesh _mesh;
        [SerializeField] private int _vertCount;
        [SerializeField] private string _objName;
        [SerializeField] private string _objNameNice;
        //[SerializeField] Dictionary<string, DeformerData> _deformers = new Dictionary<string, DeformerData>();

        [SerializeField] StringDeformerDataDictionary _deformers;


        public MeshDescription(int vertCount, string objName, Mesh mesh)
        {
            _vertCount = vertCount;
            _objName = objName;
            _mesh = mesh;
            if (_deformers == null) _deformers = new StringDeformerDataDictionary();
        }

        public DeformerData AddDeformer(string name)
        {
            if (_deformers == null) _deformers = new StringDeformerDataDictionary();

            string fullPath = _objName + "_" + name;

            if (!_deformers.ContainsKey(fullPath))
            {
                Debug.Log("Adding Deformer " + fullPath);
                DeformerData def = new DeformerData(fullPath, _vertCount);
                _deformers.Add(fullPath, def);
            }
            return _deformers[fullPath];
        }

        public void RemoveDeformer(string name)
        {
            string fullPath = _objName + "_" + name;

            if (_deformers != null)
            {
                if (_deformers.ContainsKey(fullPath))
                {
                    _deformers.Remove(fullPath);
                }
            }
        }

        public void RemoveDeformer(DeformerData deformer)
        {
 
            if (_deformers != null)
            {
                if (_deformers.ContainsValue(deformer))
                {
                    string key = _deformers.FindFirstKeyByValue(deformer);
                    _deformers.Remove(key);
                }
            }
        }


        public void CacheAllWeightmaps()
        {
            string fullPath = _objName + "_" + name;

            foreach (DeformerData p in _deformers.Values)
            {
                foreach (Weightmap w in p.Weightmaps)
                {
                    w.CacheWeights();
                }
            }
        }

        public List<string> KeyList { get { return new List<string>(_deformers.Keys); } }
        public List<DeformerData> DeformerList { get { return new List<DeformerData>(_deformers.Values); } }
        public int VertCount { get { return _vertCount; } set { _vertCount = value; } }
        public string Name { get { return _objName; } set { _objName = value; } }
        public string NiceName { get { return _objNameNice; } set { _objNameNice = value; } }
       
        public Mesh Mesh { get { return _mesh; } set { _mesh = value; } }

        public DeformerData GetDeformer(string name)
        {
            if (!_deformers.ContainsKey(name)) return null;
            return _deformers[name];
        }

        public Weightmap GetWeightmap(string deformerName, string name)
        {
            DeformerData data = GetDeformer(deformerName);
            if (data != null)
            {
                return data.GetWeightmap(name);
            }
            return null;
        }

        public Weightmap FindWeightmap(string name)
        {
            foreach (DeformerData p in _deformers.Values)
            {
                foreach (Weightmap w in p.Weightmaps)
                {
                    if (name == w.name)
                    {
                        return w;
                    }
                }
            }
            return null;
        }

        public List<string> GetWeightmaps()
        {
            List<string> weightmaps = new List<string>();
            foreach (DeformerData p in _deformers.Values)
            {
                foreach (Weightmap w in p.Weightmaps)
                {
                    weightmaps.Add(w.name);
                }
            }
            return weightmaps;
        }

        public void RemoveDeformer(string name, DeformerData deformer)
        {
            if (_deformers.ContainsValue(deformer))
            {
                _deformers.Remove(name);
            }
        }
    }
}