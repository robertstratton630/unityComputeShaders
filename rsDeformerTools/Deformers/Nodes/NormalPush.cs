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


namespace Deformer
{

    [ExecuteInEditMode]
    public class NormalPush : MonoBehaviour
    {
        [SerializeField] public bool _debugDrawGPUData;
        [SerializeField][Range(0.0f, 1.0f)] public float _envelope = 1.0f;
        [SerializeField][Range(0, 5)] public int _topoRingIter = 1;
        [SerializeField][Range(0.0f, 10.0f)] public float _pushAmount = 1;
        [SerializeField][Range(0, 300)] public int _averageNormals = 1;

        [HideInInspector] public string _deformerName;
        [HideInInspector] public MeshDescription _meshDescription;

        // compute shader settings
        [HideInInspector][SerializeField] Shader _deformerShader;
        [HideInInspector][SerializeField] ComputeShader _computeShader;
        [HideInInspector][SerializeField] int _computeShaderMainKernal;

        // buffers
        MeshSkinBuffer _meshSkinBuffer;
        ComputeBuffer _vertexBuffer;
        ComputeBuffer _vertexFlipBuffer;

        ComputeBuffer _envelopeBuffer;

        // topology based stuff
        MeshTopologyHalfEdge _meshTopo;
        ComputeBuffer _topoVertVerts;
        ComputeBuffer _topoOffsets;
        ComputeBuffer _topoStartIndexes;

        int _cacheTopoIterations = -1;

        Renderer _mr;
        MaterialPropertyBlock _mpb;
        private int _mpb_positionsBuffer_ID;
        private int _mpb_envelope;
        private int _averageNormalsKernal;
        private int _copyKernal;
        private int _copyFlipKernal;
        private int _flipKernal;
        private int id_FlipVertexBuffers;


        int _pushAmountID;

        //internals
        Mesh _mesh;
        int _vertCount;
        DeformerData _deformerData;

        protected void EditorAwake()
        {
            _deformerName = GenerateUniqueName();

            _deformerShader = Resources.Load("Shaders/DeformerSurfaceShader", typeof(Shader)) as Shader;
            _computeShader = Resources.Load("ComputeShader/NormalPushCS") as ComputeShader;

            _meshDescription = MeshUtils.CreateMeshDescription(this.gameObject);
            _deformerData = _meshDescription.AddDeformer(_deformerName);
            _deformerData.InitalizeWeightmap("envelope");
            _deformerData.InitalizeWeightmap("normalPushMulti");
        }

        protected void SetKernalProperties()
        {
            _computeShaderMainKernal = _computeShader.FindKernel("SolveOutput");
            _averageNormalsKernal = _computeShader.FindKernel("AverageNormals");
            _copyKernal = _computeShader.FindKernel("CopySkinBuffer");
            _copyFlipKernal = _computeShader.FindKernel("CopyFlipBuffer");
            id_FlipVertexBuffers = _computeShader.FindKernel("FlipVertexBuffers");
        }

        protected void Awake()
        {
            if (!Application.isPlaying)
            {
                EditorAwake();
                return;
            }
            Application.targetFrameRate = 400;
            QualitySettings.vSyncCount = 0;

            SetKernalProperties();

            // get mesh
            _mesh = MeshUtils.GetMesh(gameObject);
            _vertCount = _mesh.vertexCount;

            // recatch description and deformer data
            _meshDescription = MeshUtils.CreateMeshDescription(this.gameObject);
            _deformerData = _meshDescription.AddDeformer(_deformerName);

            _meshSkinBuffer = new MeshSkinBuffer(this.gameObject);
            _vertexBuffer = new ComputeBuffer(_vertCount, Marshal.SizeOf(typeof(VertexData)));
            _vertexFlipBuffer = new ComputeBuffer(_vertCount, Marshal.SizeOf(typeof(VertexData)));
            _envelopeBuffer = new ComputeBuffer(_vertCount, Marshal.SizeOf(sizeof(float)));



            SetDeformerShader();

            _mpb_positionsBuffer_ID = Shader.PropertyToID("vertexBuffer");


            Weightmap env = _deformerData.GetWeightmap("envelope");
            _envelopeBuffer.SetData(env.weights);

            _meshTopo = new MeshTopologyHalfEdge(_mesh, _topoRingIter);
        }

        protected void Start()
        {
            if (!Application.isPlaying) return;
        }

        protected void Update()
        {
            if (!Application.isPlaying) return;

            UpdateSkinBuffer();
            CheckTopology();
            CopySkinBuffer();
            ComputeAverageNormals();
            ComputeNormalPush();
        }


        protected void LateUpdate()
        {
            if (!Application.isPlaying) return;
            UpdateDeformerShader();
        }

        protected void FixedUpdate()
        {
            if (!Application.isPlaying) return;
            
        }

        protected void ComputeNormalPush()
        {
            _computeShaderMainKernal = _computeShader.FindKernel("SolveOutput");

            _computeShader.SetBuffer(_computeShaderMainKernal, "skinnedVertex", _meshSkinBuffer.Buffer);
            _computeShader.SetBuffer(_computeShaderMainKernal, "vertexBuffer", _vertexBuffer);
            _computeShader.SetBuffer(_computeShaderMainKernal, "envelopeBuffer", _envelopeBuffer);


            _computeShader.SetFloat("envelope", _envelope);
            _computeShader.SetFloat("pushAmount", _pushAmount);


            uint threadGroupSizeX;
            _computeShader.GetKernelThreadGroupSizes(_computeShaderMainKernal, out threadGroupSizeX, out _, out _);
            _computeShader.Dispatch(_computeShaderMainKernal, _vertCount.GetComputeShaderThreads((int)threadGroupSizeX), 1, 1);
        
        }

        protected void CopySkinBuffer()
        {
            // copy skin buffer to vertex buffer
            uint threadGroupSizeX;
            _computeShader.GetKernelThreadGroupSizes(_copyKernal, out threadGroupSizeX, out _, out _);
            _computeShader.SetBuffer(_copyKernal, "skinnedVertex", _meshSkinBuffer.Buffer);
            _computeShader.SetBuffer(_copyKernal, "vertexBuffer", _vertexBuffer);
            _computeShader.Dispatch(_copyKernal, _vertCount.GetComputeShaderThreads((int)threadGroupSizeX), 1, 1);
        }


        protected void ComputeAverageNormals()
        {
            if (_averageNormals > 0)
            {
                CopyFlipBuffer();

                uint threadGroupSizeX;
                _computeShader.GetKernelThreadGroupSizes(_averageNormalsKernal, out threadGroupSizeX, out _, out _);

                // Connected Vert Data
                _computeShader.SetBuffer(_averageNormalsKernal, "topoVertVerts", _topoVertVerts);
                _computeShader.SetBuffer(_averageNormalsKernal, "topoOffsets", _topoOffsets);
                _computeShader.SetBuffer(_averageNormalsKernal, "topoStartIndexes", _topoStartIndexes);

                // The vertex buffer
                _computeShader.SetBuffer(_averageNormalsKernal, "vertexBuffer", _vertexBuffer);
                _computeShader.SetBuffer(_averageNormalsKernal, "vertexBufferFlip", _vertexFlipBuffer);

                for (int i = 0; i < _averageNormals; i++)
                {
                    _computeShader.Dispatch(_averageNormalsKernal, _vertCount.GetComputeShaderThreads((int)threadGroupSizeX), 1, 1);
                    // setup the flip buffer
                    _computeShader.SetBuffer(id_FlipVertexBuffers, "vertexBuffer", _vertexBuffer);
                    _computeShader.SetBuffer(id_FlipVertexBuffers, "vertexBufferFlip", _vertexFlipBuffer);
               
                    _computeShader.Dispatch(id_FlipVertexBuffers, _vertCount.GetComputeShaderThreads((int)threadGroupSizeX), 1, 1);
                }
            }
        }

        void UpdateSkinBuffer()
        {
            _meshSkinBuffer.Update();
        }

        void CopyFlipBuffer()
        {
            // copy skin buffer to vertex buffer
            uint threadGroupSizeX;
            _computeShader.GetKernelThreadGroupSizes(_copyFlipKernal, out threadGroupSizeX, out _, out _);
            _computeShader.SetBuffer(_copyFlipKernal, "vertexBufferFlip", _vertexFlipBuffer);
            _computeShader.SetBuffer(_copyFlipKernal, "vertexBuffer", _vertexBuffer);
            _computeShader.Dispatch(_copyFlipKernal, _vertCount.GetComputeShaderThreads((int)threadGroupSizeX), 1, 1);
        }

        void CheckTopology()
        {
            if (_topoRingIter != _cacheTopoIterations && _averageNormals > 0)
            {
                _cacheTopoIterations = _topoRingIter;
                _meshTopo.ComputeTopology(_topoRingIter);

                _topoVertVerts = new ComputeBuffer(_meshTopo.GetArrayVertVerts.Length, Marshal.SizeOf(sizeof(int)));
                _topoOffsets = new ComputeBuffer(_meshTopo.GetArrayOffsets.Length, Marshal.SizeOf(sizeof(int)));
                _topoStartIndexes = new ComputeBuffer(_meshTopo.GetArrayStartIndexes.Length, Marshal.SizeOf(sizeof(int)));

                _topoVertVerts.SetData(_meshTopo.GetArrayVertVerts);
                _topoOffsets.SetData(_meshTopo.GetArrayOffsets);
                _topoStartIndexes.SetData(_meshTopo.GetArrayStartIndexes);
            }
        }

        private string GenerateUniqueName()
        {
            if (_deformerName == null)
            {
                NormalPush[] components = Resources.FindObjectsOfTypeAll<NormalPush>();
                _deformerName = "NormalPush" + components.Length;
            }
            return _deformerName;
        }


        private void SetDeformerShader()
        {
            if (_mr == null) _mr = GetComponent<Renderer>();
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            Renderer render = GetComponent<Renderer>();
            render.material.shader = _deformerShader;
        }

        private void UpdateDeformerShader()
        {
            Renderer render = GetComponent<Renderer>();
            _mpb.SetBuffer(_mpb_positionsBuffer_ID, _vertexBuffer);
            _mr.SetPropertyBlock(_mpb);
        }

        private void OnDisable()
        {
            DisposeBuffers();
        }

        private void OnDestroy()
        {
            DisposeBuffers();
        }

        private void DisposeBuffers()
        {
            if (_meshSkinBuffer != null) _meshSkinBuffer.Dispose();
            if (_vertexBuffer != null) _vertexBuffer.Dispose();
            if (_vertexFlipBuffer != null) _vertexFlipBuffer.Dispose();
            if (_envelopeBuffer != null) _envelopeBuffer.Dispose();
            if (_topoVertVerts != null) _topoVertVerts.Dispose();
            if (_topoOffsets != null) _topoOffsets.Dispose();
            if (_topoStartIndexes != null) _topoStartIndexes.Dispose();
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            if (_debugDrawGPUData)
            {
                VertexData[] data = new VertexData[_vertexBuffer.count];
                _vertexBuffer.GetData(data);

                for (int i = 0; i < data.Length; i++)
                {
                    var pos = this.transform.TransformPoint(data[i].position);
                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(pos, pos + this.transform.TransformVector(data[i].normal) * 0.1f);
                    Gizmos.DrawSphere(pos + this.transform.TransformVector(data[i].normal) * 0.1f, 0.005f);
                }

                Gizmos.color = Color.red;
                var sMesh = GetComponent<MeshFilter>() != null ? GetComponent<MeshFilter>().sharedMesh : GetComponent<SkinnedMeshRenderer>() != null ? GetComponent<SkinnedMeshRenderer>().sharedMesh : null;
                if (sMesh)
                {
                    Gizmos.DrawWireCube(transform.TransformPoint(sMesh.bounds.center), sMesh.bounds.size);
                }
            }
        }
    }
}