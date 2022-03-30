using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug= UnityEngine.Debug;

namespace Deformer
{
    public class KDDebug : MonoBehaviour
    {
        [SerializeField] int temp;
        [SerializeField] Transform debugObj;

        List<int> results = new List<int>();
        Vector3[] verts;
        int[] triangles;


        // Start is called before the first frame update
        void Start()
        {
            Mesh _mesh = Utils.GetMesh(gameObject);
            verts = _mesh.vertices;
            triangles = _mesh.triangles;

            KDTreeAdaptor kdAdaptor = new KDTreeAdaptor();

            Stopwatch st = new Stopwatch();
            st.Start();
            KDTree tree = kdAdaptor.BuildTree(verts);

            List<KDData> flat = kdAdaptor.convertToGPUTree(tree);
            Debug.Log("KDData size" +flat.Count);
            Debug.Log("triangles size "+triangles.Length/3);
             Debug.Log("m_count "+tree.m_count);
            
            st.Stop();
            Debug.Log(string.Format("MyMethod took {0} ms to complete", st.ElapsedMilliseconds));

            results = kdAdaptor.GetNearestNeigbours(tree, verts[6], 10);
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enabled) return;

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(this.transform.TransformVector(verts[6]), 0.005f);


            for (int i = 0; i < results.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(this.transform.TransformVector(verts[results[i]]), 0.005f);

            }
        }
    }
}