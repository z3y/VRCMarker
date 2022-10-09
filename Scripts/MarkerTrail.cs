
using System;
using System.Numerics;
using System.Text;
using UdonSharp;
using UnityEngine;
using UnityEngine.Timeline;
using VRC.SDKBase;
using VRC.Udon;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace VRCMarker
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MarkerTrail : UdonSharpBehaviour
    {
        public MarkerSync markerSync;
        public Transform trailPosition;

        public Color color = Color.white;
        [Range(0.001f, 0.01f)] public float minDistance = 0.002f;
        [Range(0.001f, 0.01f)] public float width = 0.003f;
        [Range(0.02f, 0.2f)] public float updateRate = 0.03f;
        [Range(0f, 1f)] public float smoothing = 0.67f;
        private float _smoothingCached;

        private Vector3 _previousPosition;
        private Mesh _mesh;

        private const string Version = "1";

        //when we get lists im rewriting this lol
        private Vector3[] _vertices = new Vector3[0];
        private int[] _triangles = new int[0];
        private Vector3[] _normals = new Vector3[0];
        private Vector2[] _uv = new Vector2[0];
        public const int VertexIncrement = 7;
        public const int TriangeIncrement = 9;
        const int VerticesReserved = VertexIncrement * 126;
        const int TrianglesReserved = TriangeIncrement * 126;
        public int vertexLimit = 32000;
        [HideInInspector] public int verticesUsed = 0;
        [HideInInspector] public int lastVerticesUsed = 0;
        [HideInInspector] public int trianglesUsed = 0;
        [HideInInspector] public int lastTrianglesUsed = 0;

        private float _time = 0;

        private Mesh _trailing;
        public MeshFilter trailingMesh;
        public MeshFilter trailStorage;

        private Vector3 _previousSmoothingPosition = Vector3.zero;
        private Vector3 _smoothingPosition = Vector3.zero;

        [HideInInspector] public Vector3[] lastTrailPositions;

        public bool isLocal = false;

        public void Start()
        {

            _mesh = new Mesh();
            _mesh.name = "Trail";
            _mesh.MarkDynamic();
            trailStorage.GetComponent<MeshFilter>().sharedMesh = _mesh;


            _trailing = new Mesh();
            _trailing.name = "Traling";
            _trailing.MarkDynamic();
            trailingMesh.sharedMesh = _trailing;
            

            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor("_Color", color);
            propertyBlock.SetFloat("_Scale", width);
            trailStorage.GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
            trailingMesh.GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);

            _vertices = new Vector3[VerticesReserved];
            _triangles = new int[TrianglesReserved];
            _normals = new Vector3[VerticesReserved];
            _uv = new Vector2[VerticesReserved];

            ResetTRS(transform);
            ResetTRS(trailingMesh.transform);
            ResetTRS(trailStorage.transform);

            _smoothingCached = smoothing;

            CreateTrailingLineConstants();

            _smoothingPosition = trailPosition.position;
            _previousSmoothingPosition = trailPosition.position;
            _previousPosition = _smoothingPosition;

            enabled = false;
            Clear();
        }

        private void Update()
        {
            _time += Time.deltaTime;


            _smoothingPosition = Vector3.Lerp(trailPosition.position, _previousSmoothingPosition, _smoothingCached);
            _previousSmoothingPosition = _smoothingPosition;

            UpdateTrailingLine(_smoothingPosition, _previousPosition);

            if (Vector3.Distance(_smoothingPosition, _previousPosition) < minDistance ||
                _time <= updateRate)
            {
                return;
            }

            AddLine(_previousPosition, _smoothingPosition);
            StoreLineTransform(_previousPosition);

            _previousPosition = _smoothingPosition;
            _time = 0;
        }

        private void OnEnable()
        {
            _smoothingCached = smoothing;
            _smoothingPosition = trailPosition.position;
            _previousSmoothingPosition = trailPosition.position;
            _previousPosition = trailPosition.position;
            trailingMesh.gameObject.SetActive(true);
            StoreLineTransform(_smoothingPosition);

            if (verticesUsed > 0)
            {
                UpdateTrailingLine(_smoothingPosition, _previousPosition);
            }
        }

        private void OnDisable()
        {
            _time = 0;
            trailingMesh.gameObject.SetActive(false);
            _smoothingCached = 1;
            if (isLocal)
            {
                StoreLineTransform(_previousPosition);
                StoreLineTransform(_smoothingPosition);
                AddLine(_previousPosition, _previousPosition);
                AddLine(_smoothingPosition, _previousPosition);

                _smoothingPosition = Vector3.zero;
                _previousPosition = Vector3.zero;
                _previousSmoothingPosition = Vector3.zero;
            }
            
            
            if (!isLocal)
            {
                markerSync.CreateMarkerTrail();
            }

            lastVerticesUsed = verticesUsed;
            lastTrianglesUsed = trianglesUsed;
        }

        public void AddEndLine()
        {
            AddLine(_previousPosition, _previousPosition);
            AddLine(_smoothingPosition, _previousPosition);
            lastVerticesUsed = verticesUsed;
            lastTrianglesUsed = trianglesUsed;
        }

        public void Clear()
        {
            _time = 0;
            _vertices = new Vector3[VerticesReserved];
            _triangles = new int[TrianglesReserved];
            _normals = new Vector3[VerticesReserved];
            _uv = new Vector2[VerticesReserved];

            _mesh.triangles = _triangles;
            _mesh.normals = _normals;
            _mesh.SetUVs(0, _uv);
            _mesh.vertices = _vertices;

            verticesUsed = 0;
            lastVerticesUsed = 0;
            trianglesUsed = 0;
            lastTrianglesUsed = 0;
            lastTrailPositions = new Vector3[0];
        }

        private void CreateTrailingLineConstants()
        {
            var vertices = new Vector3[10];
            // lines
            vertices[0] = Vector3.zero;
            vertices[1] = Vector3.zero;
            vertices[2] = Vector3.zero;
            vertices[3] = Vector3.zero;
            // connections
            vertices[4] = Vector3.zero;
            vertices[5] = Vector3.zero;
            vertices[6] = Vector3.zero;
            vertices[7] = Vector3.zero;
            vertices[8] = Vector3.zero;
            vertices[9] = Vector3.zero;

            var triangles = new int[12];
            // lines
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 0;
            triangles[4] = 2;
            triangles[5] = 3;
            // connections
            triangles[6] = 6;
            triangles[7] = 5;
            triangles[8] = 4;
            triangles[9] = 9;
            triangles[10] = 8;
            triangles[11] = 7;

            var uv = new Vector2[10];
            // lines
            uv[0] = _UV_0;
            uv[1] = _UV_1;
            uv[2] = _UV_2;
            uv[3] = _UV_3;
            // connections
            uv[4] = _UV_4;
            uv[5] = _UV_5;
            uv[6] = _UV_6;
            uv[7] = _UV_4;
            uv[8] = _UV_5;
            uv[9] = _UV_6;

            var normals = new Vector3[10];
            // lines
            normals[0] = Vector3.zero;
            normals[1] = Vector3.zero;
            normals[2] = Vector3.zero;
            normals[3] = Vector3.zero;
            // connections
            //normals[4] = Vector3.zero;
            //normals[5] = Vector3.zero;
            //normals[6] = Vector3.zero;

            _trailing.vertices = vertices;
            _trailing.triangles = triangles;
            _trailing.normals = normals;
            _trailing.SetUVs(0, uv);
        }

        private void UpdateTrailingLine(Vector3 startPos, Vector3 endPos)
        {
            var vertices = new Vector3[10];
            // lines
            vertices[0] = startPos;
            vertices[1] = startPos;
            vertices[2] = endPos;
            vertices[3] = endPos;
            // connections
            vertices[4] = startPos;
            vertices[5] = startPos;
            vertices[6] = startPos;
            vertices[7] = endPos;
            vertices[8] = endPos;
            vertices[9] = endPos;

            var normals = new Vector3[10];
            // lines
            normals[0] = endPos;
            normals[1] = endPos;
            normals[2] = startPos;
            normals[3] = startPos;
            // connections
            //normals[4] = Vector3.zero;
            //normals[5] = Vector3.zero;
            //normals[6] = Vector3.zero;

            _trailing.vertices = vertices;
            _trailing.normals = normals;
            _trailing.RecalculateBounds();
        }

        private readonly Vector2 _UV_0 = new Vector2(0, 0);
        private readonly Vector2 _UV_1 = new Vector2(0, 1);
        private readonly Vector2 _UV_2 = new Vector2(1, 1);
        private readonly Vector2 _UV_3 = new Vector2(1, 0);
        private readonly Vector2 _UV_4 = new Vector2(-0.077350269189625764509148780501f, 0);
        private readonly Vector2 _UV_5 = new Vector2(0.5f, 1);
        private readonly Vector2 _UV_6 = new Vector2(1.077350269189625764509148780501f, 0);

        public void CreateTrail(Vector3[] positions)
        {
            for (int i = 1; i < positions.Length; i++)
            {
                AddLine(positions[i-1], positions[i], false);
            }
            AddLine(positions[positions.Length - 1], positions[positions.Length - 2], true);
            lastVerticesUsed = verticesUsed;
            lastTrianglesUsed = trianglesUsed;
        }

        private void AddLine(Vector3 startPos, Vector3 endPos, bool updateMesh = true)
        {
            if (verticesUsed + VertexIncrement > _vertices.Length)
            {
                if (verticesUsed > vertexLimit)
                {
                    trianglesUsed = 0;
                    verticesUsed = 0;
                }
                else
                {
                    IncreaseMeshSize();
                }
            }

            //int vOffset = _verticesUsed;
            //int tOffset = _trianglesUsed;
            //var vertices = new Vector3[7];
            // lines
            _vertices[0 + verticesUsed] = startPos;
            _vertices[1 + verticesUsed] = startPos;
            _vertices[2 + verticesUsed] = endPos;
            _vertices[3 + verticesUsed] = endPos;
            // connections
            _vertices[4 + verticesUsed] = startPos;
            _vertices[5 + verticesUsed] = startPos;
            _vertices[6 + verticesUsed] = startPos;

            //var triangles = new int[9];
            // lines

            _triangles[trianglesUsed + 0] = 0 + verticesUsed;
            _triangles[trianglesUsed + 1] = 1 + verticesUsed;
            _triangles[trianglesUsed + 2] = 2 + verticesUsed;
            _triangles[trianglesUsed + 3] = 0 + verticesUsed;
            _triangles[trianglesUsed + 4] = 2 + verticesUsed;
            _triangles[trianglesUsed + 5] = 3 + verticesUsed;
            // connections
            _triangles[trianglesUsed + 6] = 6 + verticesUsed;
            _triangles[trianglesUsed + 7] = 5 + verticesUsed;
            _triangles[trianglesUsed + 8] = 4 + verticesUsed;

            //var uv = new Vector2[7];
            // lines
            _uv[0 + verticesUsed] = _UV_0;
            _uv[1 + verticesUsed] = _UV_1;
            _uv[2 + verticesUsed] = _UV_2;
            _uv[3 + verticesUsed] = _UV_3;
            // connections
            _uv[4 + verticesUsed] = _UV_4;
            _uv[5 + verticesUsed] = _UV_5;
            _uv[6 + verticesUsed] = _UV_6;

            //var normals = new Vector3[7];
            // lines
            _normals[0 + verticesUsed] = endPos;
            _normals[1 + verticesUsed] = endPos;
            _normals[2 + verticesUsed] = startPos;
            _normals[3 + verticesUsed] = startPos;
            // connections
            //normals[4] = Vector3.zero;
            //normals[5] = Vector3.zero;
            //normals[6] = Vector3.zero;

            verticesUsed += VertexIncrement;
            trianglesUsed += TriangeIncrement;

            if (updateMesh)
            {
                _mesh.vertices = _vertices;
                _mesh.triangles = _triangles;
                _mesh.normals = _normals;
                _mesh.SetUVs(0, _uv);
                _mesh.RecalculateBounds();
            }

        }

        private void StoreLineTransform(Vector3 position)
        {
            if (!isLocal)
            {
                return;
            }

            var positions = new Vector3[lastTrailPositions.Length + 1];
            Array.Copy(lastTrailPositions, positions, lastTrailPositions.Length);
            positions[positions.Length - 1] = position;
            lastTrailPositions = positions;
        }

        private void IncreaseMeshSize()
        {
            _vertices = ResizeArray(_vertices, VerticesReserved);
            _triangles = ResizeArray(_triangles, TrianglesReserved);
            _normals = ResizeArray(_normals, VerticesReserved);
            _uv = ResizeArray(_uv, VerticesReserved);
        }

        private void ResetTRS(Transform transform)
        {
            var parent = transform.parent;
            transform.parent = null;
            transform.localScale = Vector3.one;
            transform.parent = parent;

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        private static T[] ResizeArray<T>(T[] sourceArray, int incrementSize)
        {
            var newArray = new T[incrementSize + sourceArray.Length];
            Array.Copy(sourceArray, newArray, sourceArray.Length);
            return newArray;
        }

    }
}