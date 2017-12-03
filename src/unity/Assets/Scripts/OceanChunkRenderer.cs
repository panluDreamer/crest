﻿// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace OceanResearch
{
    /// <summary>
    /// Sets shader parameters for each geometry tile/chunk.
    /// </summary>
    public class OceanChunkRenderer : MonoBehaviour
    {
        [Tooltip("Expand out renderer bounds if dynamic displacement moves verts outside the mesh BB.")]
        public float _boundsPadding = 24f;

        public bool _drawRenderBounds = false;

        int _lodIndex = -1;
        int _totalLodCount = -1;
        float _baseVertDensity = 32f;

        Renderer _rend;
        Mesh _mesh;

        Bounds _boundsLocal;

        void Start()
        {
            _rend = GetComponent<Renderer>();
            _mesh = GetComponent<MeshFilter>().mesh;

            _boundsLocal = _mesh.bounds;
        }

        // Called when visible to a camera
        void OnWillRenderObject()
        {
            // per instance data

            // blend LOD 0 shape in/out to avoid pop, if the ocean might scale up later (it is smaller than its maximum scale)
            bool needToBlendOutShape = _lodIndex == 0 && OceanRenderer.Instance.ScaleCouldDouble;
            float meshScaleLerp = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 0f;

            // blend furthest normals scale in/out to avoid pop, if scale could reduce
            bool needToBlendOutNormals = _lodIndex == _totalLodCount - 1 && OceanRenderer.Instance.ScaleCouldHalve;
            float farNormalsWeight = needToBlendOutNormals ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            _rend.material.SetVector( "_InstanceData", new Vector4( meshScaleLerp, farNormalsWeight, _lodIndex ) );

            // geometry data
            float squareSize = Mathf.Abs( transform.lossyScale.x ) / _baseVertDensity;
            float normalScrollSpeed0 = Mathf.Log( 1f + 2f * squareSize ) * 1.875f;
            float normalScrollSpeed1 = Mathf.Log( 1f + 4f * squareSize ) * 1.875f;
            _rend.material.SetVector( "_GeomData", new Vector4( squareSize, normalScrollSpeed0, normalScrollSpeed1, _baseVertDensity ) );

            // assign shape textures to shader
            // this relies on the render textures being init'd in CreateAssignRenderTexture::Awake().
            Camera[] shapeCams = OceanRenderer.Instance.Builder._shapeCameras;
            WaveDataCam wdc0 = shapeCams[_lodIndex].GetComponent<WaveDataCam>();
            wdc0.ApplyMaterialParams( 0, _rend.material );
            WaveDataCam wdc1 = (_lodIndex + 1) < shapeCams.Length ? shapeCams[_lodIndex + 1].GetComponent<WaveDataCam>() : null;
            if( wdc1 )
            {
                wdc1.ApplyMaterialParams( 1, _rend.material );
            }
            else
            {
                _rend.material.SetTexture( "_WD_Sampler_1", null );
            }

            // expand mesh bounds - bounds need to completely encapsulate verts after any dynamic displacement
            Bounds bounds = _boundsLocal;
            float expand = _boundsPadding / Mathf.Abs( transform.lossyScale.x );
            bounds.extents += new Vector3( expand, 0f, expand );
            _mesh.bounds = bounds;
            if( _drawRenderBounds )
                DebugDrawRendererBounds();
        }

        public void SetInstanceData( int lodIndex, int totalLodCount, float baseVertDensity )
        {
            _lodIndex = lodIndex; _totalLodCount = totalLodCount; _baseVertDensity = baseVertDensity;
        }

        public void DebugDrawRendererBounds()
        {
            // source: https://github.com/UnityCommunity/UnityLibrary
            // license: mit - https://github.com/UnityCommunity/UnityLibrary/blob/master/LICENSE.md

            // draws mesh renderer bounding box using Debug.Drawline

            var b = _rend.bounds;

            // bottom
            var p1 = new Vector3( b.min.x, b.min.y, b.min.z );
            var p2 = new Vector3( b.max.x, b.min.y, b.min.z );
            var p3 = new Vector3( b.max.x, b.min.y, b.max.z );
            var p4 = new Vector3( b.min.x, b.min.y, b.max.z );

            Debug.DrawLine( p1, p2, Color.blue );
            Debug.DrawLine( p2, p3, Color.red );
            Debug.DrawLine( p3, p4, Color.yellow );
            Debug.DrawLine( p4, p1, Color.magenta );

            // top
            var p5 = new Vector3( b.min.x, b.max.y, b.min.z );
            var p6 = new Vector3( b.max.x, b.max.y, b.min.z );
            var p7 = new Vector3( b.max.x, b.max.y, b.max.z );
            var p8 = new Vector3( b.min.x, b.max.y, b.max.z );

            Debug.DrawLine( p5, p6, Color.blue );
            Debug.DrawLine( p6, p7, Color.red );
            Debug.DrawLine( p7, p8, Color.yellow );
            Debug.DrawLine( p8, p5, Color.magenta );

            // sides
            Debug.DrawLine( p1, p5, Color.white );
            Debug.DrawLine( p2, p6, Color.gray );
            Debug.DrawLine( p3, p7, Color.green );
            Debug.DrawLine( p4, p8, Color.cyan );
        }
    }
}
