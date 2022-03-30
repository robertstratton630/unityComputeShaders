using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Deformer
{
    class HalfEdge
    {
        public Vertex to = null;
        public Vertex from = null;
        public HalfEdge twin = null;

        public HalfEdge(Vertex to, Vertex from)
        {
            this.to = to;
            this.from = from;
        }

    }

    class Vertex
    {
        int m_id = -1;
        int triID01 = -1;
        int triID02 = -1;

        Vector3 m_position = Vector3.zero;
        HalfEdge m_outGoingHalfEdge = null;

        public List<HalfEdge> incomingHalfEdges = new List<HalfEdge>();
        public List<HalfEdge> outgoingHalfEdges = new List<HalfEdge>();

        public Vertex(int id, Vector3 position)
        {
            m_position = position;
            m_id = id;
        }

        public void SetOutgoingHalfEdge(HalfEdge outgoingHalfEdge)
        {
            m_outGoingHalfEdge = outgoingHalfEdge;
        }

        public Vector3 position { get { return m_position; } set { m_position = value; } }
        public HalfEdge halfEdge { get { return m_outGoingHalfEdge; } set { m_outGoingHalfEdge = value; } }
        public int id { get { return m_id; } set { m_id = value; } }
    }


    public class MeshTopologyHalfEdge
    {
        Mesh m_mesh = null;
        int m_vertexCount = -1;
        int m_expansion = -1;

        public int Iterations { get { return m_expansion; } }

        // topology data
        // half edge data
        List<Vertex> vertices = new List<Vertex>();

        public Vector3[] vertsRaw;

        // data for topology
        int[] offsets = null;
        int[] startIndexes = null;
        List<int> vertVerts = new List<int>();
        List<float> vertVertLengths = new List<float>();

        public int[] GetArrayOffsets { get { return offsets; } }
        public int[] GetArrayStartIndexes { get { return startIndexes; } }
        public int[] GetArrayVertVerts { get { return vertVerts.ToArray(); } }
        public float[] GetArrayVertVertLengths { get { return vertVertLengths.ToArray(); } }


        public Mesh mesh { get { return m_mesh; } }

        public MeshTopologyHalfEdge(Mesh _mesh, int iterations)
        {
            m_mesh = _mesh;
            CalculateHalfEdgeTopology();
            ComputeTopology(iterations);
        }

        void CalculateHalfEdgeTopology()
        {
            if (m_mesh == null) return;

            // get triangles and verts
            vertsRaw = m_mesh.vertices;
            m_vertexCount = vertsRaw.Length;
            int[] triangles = m_mesh.triangles;


            // create all the vertices
            // precompute any overlapping vertices UV borders
            for (int i = 0; i < vertsRaw.Length; i++)
            {
                Vertex v = new Vertex(i, vertsRaw[i]);
                vertices.Add(v);
            }

            // loop through each triangle and get vertex ID's associated with it 
            for (int i = 0; i < triangles.Length; i += 3)
            {
                // get the actual vertexID
                int vertexIDA = triangles[i];
                int vertexIDB = triangles[i + 1];
                int vertexIDC = triangles[i + 2];

                // get the vertex
                Vertex vA = vertices[vertexIDA];
                Vertex vB = vertices[vertexIDB];
                Vertex vC = vertices[vertexIDC];

                // clockwise
                HalfEdge vA_vB = new HalfEdge(vA, vB);
                HalfEdge vB_vC = new HalfEdge(vB, vC);
                HalfEdge vC_vA = new HalfEdge(vC, vA);

                // counterclockwise
                HalfEdge vA_vC = new HalfEdge(vA, vC);
                HalfEdge vC_vB = new HalfEdge(vC, vB);
                HalfEdge vB_vA = new HalfEdge(vB, vA);

                // set twins
                vA_vB.twin = vB_vA;
                vB_vC.twin = vC_vB;
                vC_vA.twin = vA_vC;
                vB_vA.twin = vA_vB;
                vC_vB.twin = vB_vC;
                vA_vC.twin = vC_vA;

                vertices[vertexIDA].outgoingHalfEdges.Add(vA_vB);
                vertices[vertexIDA].outgoingHalfEdges.Add(vA_vC);
                vertices[vertexIDB].outgoingHalfEdges.Add(vB_vC);
                vertices[vertexIDB].outgoingHalfEdges.Add(vB_vA);
                vertices[vertexIDC].outgoingHalfEdges.Add(vC_vA);
                vertices[vertexIDC].outgoingHalfEdges.Add(vC_vB);

                // set incmoing connections
                vertices[vertexIDA].incomingHalfEdges.Add(vC_vA);
                vertices[vertexIDA].incomingHalfEdges.Add(vB_vA);
                vertices[vertexIDB].incomingHalfEdges.Add(vA_vB);
                vertices[vertexIDB].incomingHalfEdges.Add(vC_vB);
                vertices[vertexIDC].incomingHalfEdges.Add(vA_vC);
                vertices[vertexIDC].incomingHalfEdges.Add(vB_vC);
            }
            
            // really hack method to try find connected  triangles on uv seems
            KDTreeAdaptor kda = new KDTreeAdaptor();
            KDTree tree = kda.BuildTree(vertsRaw);

            for (int i = 0; i < vertsRaw.Length; i++)
            {
                List<int> results = kda.GetNearestNeigbours(tree, vertsRaw[i], 10);
                List<HalfEdge> overlaps = new List<HalfEdge>();

                for (int j = 0; j < results.Count; j++)
                {
                    int idx = results[j];

                    if (idx != i && vertsRaw[i]==vertsRaw[idx])
                    {
                        List<HalfEdge> found = vertices[idx].outgoingHalfEdges;

                        for (int k=0;k<found.Count;k++)
                        {
                            vertices[i].outgoingHalfEdges.Add(found[k]);
                        }
                    }
                }
            }
            
        }


        public List<int> _calculateHalfEdgeConnectedVertices(int vertexID)
        {
            List<HalfEdge> halfEdges = vertices[vertexID].outgoingHalfEdges;
            List<int> connectedVertices = new List<int>();
            for (int i = 0; i < halfEdges.Count; i++)
            {
                connectedVertices.Add(halfEdges[i].from.id);
            }

            return connectedVertices.Distinct().ToList();
        }

        public List<int> _calculateHalfEdgeConnectedVerticesExpand(int startVertID, int vertexID, int iter = 0, int count = 0)
        {
            List<int> fullList = new List<int>();
            Transverse(startVertID, vertexID, iter, count);
            return fullList;

            void Transverse(int startVertID, int vertexID, int iter = 0, int count = 0)
            {
                List<int> connectedVertices = _calculateHalfEdgeConnectedVertices(vertexID);
                for (int i = 0; i < connectedVertices.Count; i++)
                {
                    // cull out any previously found vertices
                    int found = 0;
                    for (int j = 0; j < fullList.Count; j++)
                    {
                        if (connectedVertices[i] == fullList[j])
                        {
                            found = 1;
                            break;
                        }
                    }

                    // if not found and not start vert then uniqiue id
                    if (found == 0 & connectedVertices[i] != startVertID)
                    {
                        fullList.Add(connectedVertices[i]);
                    }
                }

                // find all the connected verts
                if (count < iter)
                {
                    count++;
                    for (int i = 0; i < connectedVertices.Count; i++)
                    {
                        Transverse(startVertID, connectedVertices[i], iter, count);
                    }
                }
            }
        }


        public bool ComputeTopology(int expand)
        {
            // cache out the expand so we safe calculate
            if (expand == m_expansion) return false;
            m_expansion = expand;

            // reset all the arrays
            offsets = new int[m_vertexCount + 1];
            startIndexes = new int[m_vertexCount];
            vertVerts.Clear();
            vertVertLengths.Clear();
            int[] connectedVertCounts = new int[m_vertexCount];

            for (int vertID = 0; vertID < m_vertexCount; vertID++)
            {
                List<int> connectedVerts = _calculateHalfEdgeConnectedVerticesExpand(vertID, vertID, expand);
                connectedVertCounts[vertID] = connectedVerts.Count;
                offsets[vertID] = vertID > 0 ? offsets[vertID - 1] + connectedVertCounts[vertID - 1] : 0;

                for (int cVert = 0; cVert < connectedVerts.Count; cVert++)
                {
                    vertVerts.Add(connectedVerts[cVert]);
                }
            }
            // catch last offset
            offsets[m_vertexCount] = offsets[m_vertexCount - 1] + connectedVertCounts[m_vertexCount - 1];
            return true;
        }



        // method for extracting connected verts via flat array
        public int[] GetConnectedVerts(int vertexID)
        {
            int startIndex = offsets[vertexID];
            int connectedVertCount = offsets[vertexID + 1] - offsets[vertexID];

            int[] verts = new int[connectedVertCount];

            int c = 0;
            for (int i = startIndex; i < startIndex + connectedVertCount; i++)
            {
                verts[c] = vertVerts[i];
                c++;
            }

            return verts;
        }
    }
}