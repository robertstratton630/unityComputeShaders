using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Deformer
{
    public class KDTreeAdaptor
    {

        public List<Triangle> triangles = new List<Triangle>();
        public List<KDData> kdData = new List<KDData>();

        private List<Triangle> CreateTriangles(GameObject rootMeshObject)
        {
            var meshFilters = rootMeshObject.GetComponentsInChildren<MeshFilter>();

            return meshFilters.SelectMany(mf =>
            {
                var mesh = mf.sharedMesh;
                var triangles = mesh.triangles;

                var trans = mf.transform;
                var worldVertices = mesh.vertices.Select(vtx => trans.TransformPoint(vtx)).ToList();

                return Enumerable.Range(0, triangles.Length / 3).Select(i =>
                {
                    var pos0 = worldVertices[triangles[i * 3 + 0]];
                    var pos1 = worldVertices[triangles[i * 3 + 1]];
                    var pos2 = worldVertices[triangles[i * 3 + 2]];

                    var normal = -Vector3.Cross(pos0 - pos1, pos2 - pos1).normalized;
                    return new Triangle()
                    {
                        pos0 = pos0,
                        pos1 = pos1,
                        pos2 = pos2,
                        normal = normal
                    };
                });
            }).ToList();
        }


        public KDTree BuildTriangleCentroidTree(GameObject rootMeshObject)
        {
            triangles = CreateTriangles(rootMeshObject);

            KDTree vertexTree = new KDTree(3);
            for (int i = 0; i < triangles.Count; ++i)
            {
                Vector3 triCenter = (triangles[i].pos0 + triangles[i].pos1 + triangles[i].pos2) / 3.0f;
                vertexTree.insert(triCenter, i);
            }
            return vertexTree;
        }


        public List<KDData> convertToGPUTree(KDTree tree)
        {
            kdData = new List<KDData>();

            KDTree.KDNode root = tree.root;
            buildGPUTreeRecursive(tree.root, kdData);
            return kdData;
        }

        static void buildGPUTreeRecursive(KDTree.KDNode node, List<KDData> datas)
        {
            var data = new KDData()
            {
                point = Vector3.zero,
                leftIdx = -1,
                rightIdx = -1,
                triangleIdx = -1,
            };

            data.IsLeaf = node.left == null && node.right == null;

            if (data.IsLeaf)
            {
                data.triangleIdx = node.point.id;
                data.point = node.point.position;
                datas.Add(data);
            }
            else
            {
                data.triangleIdx = node.point.id;
                data.point = node.point.position;

                var dataIdx = datas.Count;
                datas.Add(default); // reserve my data idx

                if (node.left != null)
                {
                    data.leftIdx = datas.Count;
                    buildGPUTreeRecursive(node.left, datas);
                }

                if (node.right != null)
                {
                    data.rightIdx = datas.Count;
                    buildGPUTreeRecursive(node.right, datas);
                }

                datas[dataIdx] = data;
            }
        }

        public KDTree BuildTree(Vector3[] verts)
        {
            KDTree vertexTree = new KDTree(3);
            for (int i = 0; i < verts.Length; ++i)
            {
                var vec = verts[i];
                vertexTree.insert(vec, i);
            }
            return vertexTree;
        }

        public List<int> GetNearestNeigbours(KDTree tree, Vector3 query, int num)
        {
            var vec = query;
            List<int> tempHash = new List<int>();
            tree.nearestAddNbrs(query, num, ref tempHash);
            return tempHash;
        }
    }
}
