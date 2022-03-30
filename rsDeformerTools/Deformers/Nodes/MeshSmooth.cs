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
    public class MeshSmooth : DeformerBase
    {
        [SerializeField][Range(0,300)] int _smoothIterations = 0;


        //privates
        private int _copySkinBufferKernal;
        private int _smoothKernal;
        private int _swapVertexBuffersKernal;


        protected override void EditorAwake()
        {
            _computeShader = Resources.Load("ComputeShader/MeshSmooth") as ComputeShader;
            base.EditorAwake();
        }

        protected override void SetKernalProperties()
        {
            base.SetKernalProperties();
            _copySkinBufferKernal = _computeShader.FindKernel("CopySkinBuffer");
            _smoothKernal = _computeShader.FindKernel("ComputeSmooth");
            _swapVertexBuffersKernal = _computeShader.FindKernel("SwapVertexBuffers");
        }

        protected override void Awake()
        {
            base.Awake();
            if (!Application.isPlaying) return;
        }

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
        }

        protected override void Update()
        {
            base.Update();
            if (!Application.isPlaying) return;

            // copy vertexBuffer and vertexSwapBuffer
            CopySkinBuffer();
            ComputeSmooth();
        }


        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!Application.isPlaying) return;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!Application.isPlaying) return;

        }

        protected override string GenerateUniqueName()
        {
            if (_deformerName == null)
            {
                MeshSmooth[] components = Resources.FindObjectsOfTypeAll<MeshSmooth>();
                _deformerName = "MeshSmooth" + components.Length;
            }
            return _deformerName;
        }


        protected override void DisposeBuffers()
        {
            base.DisposeBuffers();
        }


        protected void CopySkinBuffer()
        {
            // copy skin buffer to vertex buffer
            uint threadGroupSizeX;
            _computeShader.GetKernelThreadGroupSizes(_copySkinBufferKernal, out threadGroupSizeX, out _, out _);
            _computeShader.SetBuffer(_copySkinBufferKernal, "skinnedVertex", _meshSkinBuffer.Buffer);
            _computeShader.SetBuffer(_copySkinBufferKernal, "vertexBuffer", _vertexBuffer);
            _computeShader.SetBuffer(_copySkinBufferKernal, "vertexSwapBuffer", _vertexSwapBuffer);
            _computeShader.Dispatch(_copySkinBufferKernal, _vertCount.GetComputeShaderThreads((int)threadGroupSizeX), 1, 1);
        }

        protected void ComputeSmooth()
        {
            // copy skin buffer to vertex buffer
            uint threadGroupSizeX;
            _computeShader.GetKernelThreadGroupSizes(_smoothKernal, out threadGroupSizeX, out _, out _);
            _computeShader.SetBuffer(_smoothKernal, "vertexBuffer", _vertexBuffer);
            _computeShader.SetBuffer(_smoothKernal, "vertexSwapBuffer", _vertexSwapBuffer);
            _computeShader.SetBuffer(_smoothKernal, "topoVertVerts", _topoVertVerts);
            _computeShader.SetBuffer(_smoothKernal, "topoOffsets", _topoOffsets);
            _computeShader.SetBuffer(_smoothKernal, "topoStartIndexes", _topoStartIndexes);

            _computeShader.GetKernelThreadGroupSizes(_swapVertexBuffersKernal, out threadGroupSizeX, out _, out _);
            _computeShader.SetBuffer(_swapVertexBuffersKernal, "vertexBuffer", _vertexBuffer);
            _computeShader.SetBuffer(_swapVertexBuffersKernal, "vertexSwapBuffer", _vertexSwapBuffer);
            

            for(int i=0;i<_smoothIterations;i++)
            {
                _computeShader.Dispatch(_smoothKernal, _vertCount.GetComputeShaderThreads((int)threadGroupSizeX), 1, 1);
                _computeShader.Dispatch(_swapVertexBuffersKernal, _vertCount.GetComputeShaderThreads((int)threadGroupSizeX), 1, 1);  
            }
        }
    }
}