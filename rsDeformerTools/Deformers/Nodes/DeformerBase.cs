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
using UnityEditor;

namespace Deformer
{

    [ExecuteInEditMode]
    public abstract class DeformerBase : MonoBehaviour
    {
        [SerializeField] public bool _debugDrawGPUData;
        [SerializeField][Range(0.0f, 1.0f)] public float _envelope = 1.0f;
        [SerializeField][Range(0, 5)] public int _topoRingIter = 0;

        [SerializeField] public string _deformerName;
        [HideInInspector] public MeshDescription _meshDescription;

        // compute shader settings
        [HideInInspector][SerializeField] protected Shader _deformerShader;
        [HideInInspector][SerializeField] protected ComputeShader _computeShader;

        // buffers
        protected MeshSkinBuffer _meshSkinBuffer;
        protected ComputeBuffer _vertexBuffer;
        protected ComputeBuffer _vertexSwapBuffer;
        protected ComputeBuffer _envelopeBuffer;

        // topology based stuff
        protected MeshTopologyHalfEdge _meshTopo;
        protected ComputeBuffer _topoVertVerts;
        protected ComputeBuffer _topoOffsets;
        protected ComputeBuffer _topoStartIndexes;

        protected int _cacheTopoIterations = -1;
        protected int _mpb_positionsBuffer_ID = -1;

        protected Renderer _mr;
        protected MaterialPropertyBlock _mpb;

        //internals
        protected Mesh _mesh;
        protected int _vertCount;
        protected DeformerData _deformerData;

        protected virtual void EditorAwake()
        {
            _deformerName = GenerateUniqueName();
            _deformerShader = Resources.Load("Shaders/DeformerSurfaceShader", typeof(Shader)) as Shader;
            _meshDescription = MeshUtils.CreateMeshDescription(this.gameObject);
            _deformerData = _meshDescription.AddDeformer(_deformerName);
            _deformerData.InitalizeWeightmap("envelope");
        }

        protected virtual void SetKernalProperties()
        {
        }

        protected virtual void Awake()
        {
            if (!Application.isPlaying)
            {
                EditorAwake();
                return;
            }

            // get mesh
            _mesh = MeshUtils.GetMesh(gameObject);
            _vertCount = _mesh.vertexCount;

            SetKernalProperties();
            SetDeformerShader();

            // recatch description and deformer data
            _meshDescription = MeshUtils.CreateMeshDescription(this.gameObject);
            _deformerData = _meshDescription.AddDeformer(_deformerName);

            _meshSkinBuffer = new MeshSkinBuffer(this.gameObject);
            _vertexBuffer = new ComputeBuffer(_vertCount, Marshal.SizeOf(typeof(VertexData)));
            _vertexSwapBuffer = new ComputeBuffer(_vertCount, Marshal.SizeOf(typeof(VertexData)));
            _envelopeBuffer = new ComputeBuffer(_vertCount, Marshal.SizeOf(sizeof(float)));

            _mpb_positionsBuffer_ID = Shader.PropertyToID("vertexBuffer");
            Weightmap env = _deformerData.GetWeightmap("envelope");
            _envelopeBuffer.SetData(env.weights);

            _meshTopo = new MeshTopologyHalfEdge(_mesh, _topoRingIter);
        }

        protected virtual void Start()
        {
            if (!Application.isPlaying) return;
        }

        protected virtual void Update()
        {
            if (!Application.isPlaying) return;
            _computeShader.SetFloat("envelope", _envelope);
            CheckTopology();
            UpdateSkinBuffer();

        }

        protected virtual void LateUpdate()
        {
            if (!Application.isPlaying) return;
            UpdateDeformerShader();
        }

        protected virtual void FixedUpdate()
        {
            if (!Application.isPlaying) return;
        }

        void UpdateSkinBuffer()
        {
            _meshSkinBuffer.Update();
        }

        void CheckTopology()
        {
            if (_topoRingIter != _cacheTopoIterations)
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

        protected virtual string GenerateUniqueName()
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
            if (_meshDescription) _meshDescription.RemoveDeformer(_deformerData);
        }

        protected virtual void DisposeBuffers()
        {
            if (_meshSkinBuffer != null) _meshSkinBuffer.Dispose();
            if (_vertexBuffer != null) _vertexBuffer.Dispose();
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


                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(pos, pos + this.transform.TransformVector(data[i].normal) * 0.1f);
                    Gizmos.DrawSphere(pos + this.transform.TransformVector(data[i].normal) * 0.1f, 0.005f);


                    Handles.color = Color.black;
                    Handles.Label(pos, i.ToString());
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