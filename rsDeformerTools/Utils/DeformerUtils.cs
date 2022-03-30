using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.Rendering;
using System.Reflection;
using UnityEditor;

namespace Deformer
{
    public enum MeshType
    {
        None,
        MeshFilter,
        SkinnedMeshRenderer
    }
    public struct VertexData
    {
        public Vector3 position;
        public Vector3 normal;
    }

    public static class Extensions
    {
        public static K FindFirstKeyByValue<K, V>(this Dictionary<K, V> dict, V val)
        {
            return dict.FirstOrDefault(entry =>
                EqualityComparer<V>.Default.Equals(entry.Value, val)).Key;
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static void ClearBuffer(this ComputeBuffer buffer)
        {
            if (buffer != null)
                buffer.Release();
            buffer = null;
        }
        public static bool ExistsAndEnabled(this MonoBehaviour comp, out MonoBehaviour outComp)
        {
            if (comp != null && comp.enabled)
            {
                outComp = comp;
                return true;
            }
            outComp = null;
            return false;
        }

        public static int GetComputeShaderThreads(this int count, int threads = 64)
        {
            return (count + threads - 1) / threads;
        }



        public static Vector3[] GetMeshVertices(this Mesh _mesh, Transform _transform = null)
        {
            Vector3[] verts = _mesh.vertices;
            if (_transform == null) return verts;

            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = _transform.TransformPoint(verts[i]);
            }

            return verts;
        }
    }


    public static class Utils
    {
        public static Mesh GetMesh(GameObject obj)
        {
            Mesh curMesh = null;
            if (obj == null) return curMesh;
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();

            if (smr)
                curMesh = smr.sharedMesh;
            else if (mf)
                curMesh = mf.sharedMesh;

            return curMesh;
        }

        
        public static bool Swap<T>(ref T x, ref T y)
        {
            try
            {
                T t = y;
                y = x;
                x = t;
                return true;
            }
            catch
            {
                return false;
            }
        }


        public static void StoreTopology(GameObject obj)
        {
            Mesh mesh = GetMesh(obj);


        }

        public static string GetNiceName(string name)
        {
            string meshNiceName = null;
            if (name.Contains(":"))
            {
                string[] split = name.Split(":");
                meshNiceName = split[split.Length - 1];
                return meshNiceName;
            }
            else
            {
                return name;
            }
        }
    }
}