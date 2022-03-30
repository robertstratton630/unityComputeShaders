using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;



/*
    class for getting direct access to the GPU skin buffer


*/


namespace Deformer
{
    public class MeshSkinBuffer
    {
        // store the type of mesh
        private GameObject _obj;
        private SkinnedMeshRenderer _skinnedMesh;
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        public MeshType _meshType;
        private Transform _rootBone;

        private int _vertexCount;
        private ComputeShader _computeShader;

        public ComputeBuffer Buffer { get { return _meshVertsOut; } }

        private GraphicsBuffer _vertexBuffer;
        internal ComputeBuffer _meshVertsOut;

        private int _vertexStride;
        public int vertCount { get { return _vertexCount; } }

        public void Dispose()
        {
            if (_vertexBuffer != null) _vertexBuffer.Dispose();
            if (_meshVertsOut != null) _meshVertsOut.Dispose();
        }

        public MeshSkinBuffer(GameObject obj)
        {
            Initialize(obj);
        }

        public void Initialize(GameObject obj)
        {
            Debug.Log("Initalizing skin buffer");
            _obj = obj;
            // create the compute shader to get gpu skin data
            if (this._computeShader == null) this._computeShader = Resources.Load("ComputeShader/GetGPUSkinData") as ComputeShader;

            // chekc if skinned mesh exists
            _skinnedMesh = obj.GetComponent<SkinnedMeshRenderer>();
            _meshFilter = obj.GetComponent<MeshFilter>();

            if (_skinnedMesh)
            {
                _skinnedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                _meshType = MeshType.SkinnedMeshRenderer;
                _rootBone = _skinnedMesh.rootBone;
            }
            else if (_meshFilter)
            {
                _meshFilter.sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                _meshType = MeshType.MeshFilter;
            }
            else
            {
                _meshType = MeshType.None;
            }

            // cache out the mesh
            Mesh mesh = MeshUtils.GetMesh(obj);
            mesh.RecalculateNormals();
            _vertexCount = mesh.vertexCount;
            _meshVertsOut = new ComputeBuffer(_vertexCount, Marshal.SizeOf(typeof(VertexData)));
        }

        public void Update()
        {
            if (_meshType == MeshType.None) return;

            if (_meshType == MeshType.MeshFilter)
            {

                if (_vertexBuffer == null)
                {
                    _vertexBuffer = _meshFilter.sharedMesh.GetVertexBuffer(0);
                    _vertexStride = _meshFilter.sharedMesh.GetVertexBufferStride(0);
                    _meshFilter.sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                }
            }

            if (_meshType == MeshType.SkinnedMeshRenderer)
            {

                if (_vertexBuffer == null)
                {
                    _vertexBuffer = _skinnedMesh.GetVertexBuffer();
                    _vertexStride = _skinnedMesh.sharedMesh.GetVertexBufferStride(0);
                    _skinnedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                }

                if (_vertexBuffer != null && _vertexBuffer.IsValid())
                {

                    _computeShader.SetVector("g_RootRot", QuatToVec(_rootBone.localRotation));
                    _computeShader.SetVector("g_RootPos", _rootBone.localPosition);
                }
            }

            if (_vertexBuffer != null)
            {
                int kernalID = _computeShader.FindKernel("CopyMeshSkinnedPositions");
                _computeShader.SetInt("g_VertCount", _vertexCount);
                _computeShader.SetInt("g_VertStride", _vertexStride);
                _computeShader.SetBuffer(kernalID, "g_VertexData", _vertexBuffer);
                _computeShader.SetBuffer(kernalID, "g_MeshVertsOut", _meshVertsOut);

                uint threadGroupSizeX;
                _computeShader.GetKernelThreadGroupSizes(kernalID, out threadGroupSizeX, out _, out _);
                _computeShader.Dispatch(kernalID, _vertexCount.GetComputeShaderThreads((int)threadGroupSizeX), 1, 1);
            }
        }

        private Vector4 QuatToVec(Quaternion rot)
        {
            Vector4 rotVec;
            rotVec.x = rot.x;
            rotVec.y = rot.y;
            rotVec.z = rot.z;
            rotVec.w = rot.w;
            return rotVec;
        }

        public void Clear()
        {
            _vertexBuffer.Dispose();
            _meshVertsOut.Dispose();
        }
    }
}