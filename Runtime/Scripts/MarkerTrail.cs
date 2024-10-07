using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace VRCMarker
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MarkerTrail : UdonSharpBehaviour
    {
        public Marker marker;
        public Transform trailPosition;

        public int trailType = 0;
        public Color color = Color.white;
        public Gradient gradient;


        public float emission = 1f;
        public float minDistance = 0.0025f;
        public float width = 0.003f;
        public float updateRate = 0.03f;
        public float smoothingTime = 0.06f;
        private float _smoothingCached = 1;

        private Vector3[] _vertices = new Vector3[0];
        private int[] _triangles = new int[0];
        private Vector3[] _normals = new Vector3[0];
        //private Vector2[] _uv = new Vector2[0];

        const int _vertexLimit = 32000;
        private int _verticesUsed = 0;
        private int _lastVerticesUsed = 0;
        private int _trianglesUsed = 0;
        private int _lastTrianglesUsed = 0;

        private Mesh _mesh;
        public MeshFilter trailStorage;

        private float _time = 0;
        private Vector3 _previousPosition = Vector3.zero;
        private Vector3 _previousSmoothingPosition = Vector3.zero;
        private Vector3 _smoothingPosition = Vector3.zero;

        public bool isLocal = true;

        private Vector3[] _syncLines = new Vector3[MarkerSync.MaxSyncCount];
        private int _syncLinesUsed = 0;

        const float FloatHalfMax = float.MaxValue / 2.0f;
        private Bounds _infBounds = new Bounds(Vector3.zero, new Vector3(FloatHalfMax, FloatHalfMax, FloatHalfMax));

        private void Start()
        {
            _mesh = new Mesh();
            _mesh.name = "Trail";
            _mesh.MarkDynamic();
            trailStorage.GetComponent<MeshFilter>().sharedMesh = _mesh;

            var propertyBlock = new MaterialPropertyBlock();
            var renderer = trailStorage.GetComponent<MeshRenderer>();

            if (trailType == 0)
            {
#if UNITY_ANDROID
                propertyBlock.SetColor("_Color", color);
#else
            propertyBlock.SetColor("_Color", color * emission);
#endif
            }
            else if (trailType == 1)
            {
                renderer.material.EnableKeyword("_GRADIENT_ENABLED");
                SetGradient(propertyBlock);
            }

            propertyBlock.SetFloat("_Scale", width);
            renderer.SetPropertyBlock(propertyBlock);

            ResetTransforms();

            enabled = false;
        }

        public void SetGradient(MaterialPropertyBlock pb)
        {
            var keys = gradient.colorKeys;
            int gradientLength = keys.Length;

            pb.SetFloat("_GradientLength", gradientLength);

            var gradients = new Vector4[gradientLength];
            for (int i = 0; i < gradients.Length; i++)
            {
                var key = keys[i];
                gradients[i] = key.color;
                gradients[i].w = key.time;
            }

            pb.SetVectorArray("_Gradient", gradients);
        }

        Vector3 _previousUnsmoothedPosition = Vector3.zero;
        Vector3 _previousDirectionChange = Vector3.one.normalized;
        Vector3 _previousDirection = Vector3.one.normalized;

        private void Update()
        {
            if (!enabled) // can end up running for a frame breaking things
            {
                return;
            }

            var t = Time.deltaTime;

            _time += t;

            var tipPosition = trailPosition.position;
            _smoothingPosition = Vector3.Lerp(_previousSmoothingPosition, tipPosition, t / _smoothingCached);
            _previousSmoothingPosition = _smoothingPosition;

            Vector3 currentDirection = tipPosition - _previousUnsmoothedPosition;
            _previousUnsmoothedPosition = tipPosition;


            UpdateLastPosition(_smoothingPosition, _previousPosition);
            UpdateMeshData();

            if (_time <= updateRate || Vector3.Distance(_smoothingPosition, _previousPosition) < minDistance)
            {
                return;
            }
            if (!(_time >= updateRate && Vector3.Distance(_previousPosition, _smoothingPosition) > minDistance &&
                (Vector3.Cross(_previousDirection, tipPosition - _previousPosition).magnitude > 0.001f ||
                 Vector3.Angle(_previousDirectionChange, currentDirection) > 25.0f
                )))
            {
                return;
            }

            if (!currentDirection.Equals(Vector3.zero))
            {
                _previousDirectionChange = currentDirection.normalized;
            }

            _previousDirection = (tipPosition - _previousPosition).normalized;

            CreateTrailLine(_previousPosition, _smoothingPosition);
            StoreLastLinesTransform(_smoothingPosition);
            if (_syncLinesUsed == 6)
            {
                // prevent wrong first lines from object sync
                marker.StartWritingRemote();
            }

            _previousPosition = _smoothingPosition;
            _time = 0;
        }

        public void StartWriting()
        {
            if (enabled) return;

            _time = 0;
            _smoothingCached = smoothingTime;
            _lastVerticesUsed = _verticesUsed;
            _lastTrianglesUsed = _trianglesUsed;

            var position = trailPosition.position;
            _smoothingPosition = position;
            _previousSmoothingPosition = position;
            _previousPosition = position;
            CreateTrailLine(position, position);

            StoreLastLinesTransform(position);
            UpdateMeshData();

            CreateTrailLine(position, position); // for point at tip

            enabled = true;
        }

        public void StopWriting()
        {
            if (!enabled) return;
            enabled = false;

            _time = 0;
            _smoothingCached = 1;


            if (isLocal && GetSyncLines().Length > 1)
            {
                //AddEndCap();
                StoreLastLinesTransform(_smoothingPosition);
                RecalculateMeshBounds();
            }
        }

       /* public void AddEndCap()
        {
            CreateTrailLine(_previousPosition, _smoothingPosition);
            //CreateTrailLine(_smoothingPosition, _previousPosition); // mot needed anymore
            UpdateMeshData();
        }*/




        public void RecalculateMeshBounds() => _mesh.RecalculateBounds();

        public void Clear()
        {
            _time = 0;
            _vertices = new Vector3[0];
            _triangles = new int[0];
            _normals = new Vector3[0];
            //_uv = new Vector2[0];

            _mesh.triangles = _triangles;
            _mesh.normals = _normals;
            //_mesh.SetUVs(0, _uv);
            _mesh.vertices = _vertices;

            _verticesUsed = 0;
            _trianglesUsed = 0;
            _lastVerticesUsed = 0;
            _lastTrianglesUsed = 0;
            ResetSyncLines();
        }
        /*
                // moved to the shader 
                private readonly Vector2 _UV_0 = new Vector2(0, 0);
                private readonly Vector2 _UV_1 = new Vector2(0, 1);
                private readonly Vector2 _UV_2 = new Vector2(1, 1);
                private readonly Vector2 _UV_3 = new Vector2(1, 0);
                private readonly Vector2 _UV_4 = new Vector2(-0.077350269189625764509148780501f, 0);
                private readonly Vector2 _UV_5 = new Vector2(0.5f, 1);
                private readonly Vector2 _UV_6 = new Vector2(1.077350269189625764509148780501f, 0);
        */

        public void CreateTrail(Vector3[] positions)
        {
            RevertUsedVertices();

            CreateTrailLine(positions[0], positions[0]);

            for (int i = 1; i < positions.Length; i++)
            {
                CreateTrailLine(positions[i - 1], positions[i]);
            }


            UpdateUsedVertices();

            UpdateMeshData();
        }
        
        public void RevertUsedVertices()
        {
            _verticesUsed = _lastVerticesUsed;
            _trianglesUsed = _lastTrianglesUsed;
        }

        public void UpdateUsedVertices()
        {
            _lastVerticesUsed = _verticesUsed;
            _lastTrianglesUsed = _trianglesUsed;
        }

        private const int VertexIncrement = 7;
        private const int TriangleIncrement = 9;


        int v0;
        int v1;
        int v2;
        int v3;
        int v4;
        int v5;
        int v6;

        public void CreateTrailLine(Vector3 end, Vector3 start)
        {
            
            UpdateArraySize(VertexIncrement, TriangleIncrement);

            v0 = _verticesUsed;
            v1 = _verticesUsed + 1;
            v2 = _verticesUsed + 2;
            v3 = _verticesUsed + 3;
            v4 = _verticesUsed + 4;
            v5 = _verticesUsed + 5;
            v6 = _verticesUsed + 6;

            int t0 = _trianglesUsed;
            int t1 = _trianglesUsed + 1;
            int t2 = _trianglesUsed + 2;
            int t3 = _trianglesUsed + 3;
            int t4 = _trianglesUsed + 4;
            int t5 = _trianglesUsed + 5;
            int t6 = _trianglesUsed + 6;
            int t7 = _trianglesUsed + 7;
            int t8 = _trianglesUsed + 8;


            // line
            _vertices[v0] = start;
            _vertices[v1] = start;
            _vertices[v2] = end;
            _vertices[v3] = end;

            _triangles[t0] = v0;
            _triangles[t1] = v1;
            _triangles[t2] = v2;
            _triangles[t3] = v0;
            _triangles[t4] = v2;
            _triangles[t5] = v3;
/*
            _uv[v0] = _UV_0;
            _uv[v1] = _UV_1;
            _uv[v2] = _UV_2;
            _uv[v3] = _UV_3;*/

            _normals[v0] = end;
            _normals[v1] = end;
            _normals[v2] = start;
            _normals[v3] = start;

            // triangle
            _vertices[v4] = start;
            _vertices[v5] = start;
            _vertices[v6] = start;

            _triangles[t6] = v6;
            _triangles[t7] = v5;
            _triangles[t8] = v4;

/*            _uv[v4] = _UV_4;
            _uv[v5] = _UV_5;
            _uv[v6] = _UV_6;*/
            
            // already 0
            //_normals[v4] = Vector3.zero;
            //_normals[v5] = Vector3.zero;
            //_normals[v6] = Vector3.zero;

            _verticesUsed += VertexIncrement;
            _trianglesUsed += TriangleIncrement;
        }

        private void UpdateLastPosition(Vector3 start, Vector3 end)
        {
            // temp
            if (v1 == 0)
            {
                return;
            }

            _vertices[v0] = start;
            _vertices[v1] = start;
            _vertices[v2] = end;
            _vertices[v3] = end;

            _vertices[v4] = start;
            _vertices[v5] = start;
            _vertices[v6] = start;

            _normals[v0] = end;
            _normals[v1] = end;
            _normals[v2] = start;
            _normals[v3] = start;
        }

        public void UpdateMeshData()
        {


            _mesh.vertices = _vertices;
            _mesh.triangles = _triangles;
            _mesh.normals = _normals;
            //_mesh.SetUVs(0, _uv);
            //_mesh.RecalculateBounds();
            _mesh.bounds = _infBounds;
        }

        private void StoreLastLinesTransform(Vector3 position)
        {
            if (!isLocal)
            {
                return;
            }

            if (_syncLinesUsed > _syncLines.Length - 1)
            {
                return;
            }
            _syncLines[_syncLinesUsed] = position;
            _syncLinesUsed++;
        }

        public Vector3 GetLastLinePosition()
        {
            if (_verticesUsed == 0)
            {
                return Vector3.zero;
            }

            return _vertices[_verticesUsed];
        }
 
        public int RemoveLastLineConnection()
        {
            if (_verticesUsed == 0)
            {
                return 0;
            }
            int breakCount = _verticesUsed - 500;
            int count = _verticesUsed;


            RemoveLastLine();
            if (!IsLastPositionEndOfLine())
            {
                for (int i = count - 1; i >= breakCount && _verticesUsed > 0; i-=7)
                {
                    RemoveLastLine();
                    if (IsLastPositionEndOfLine())
                    {
                        RemoveLastLine();
                        break;
                    }
                }
            }


            UpdateMeshData();

            return count - _verticesUsed;
        }

        private void RemoveLastLine()
        {
            if (_verticesUsed == 0 || !MarkerInitialized())
            {
                return;
            }

            int newVertexCount = _verticesUsed - VertexIncrement;
            /*for (int i = _verticesUsed; i >= newVertexCount; i--)
            {
                _vertices[i] = Vector3.zero;
            }*/
            Array.Clear(_vertices, newVertexCount, VertexIncrement);
            _verticesUsed = newVertexCount;

        }

        public void RemoveLastLines(int lines)
        {
            if (_verticesUsed == 0 || !MarkerInitialized())
            {
                return;
            }
            int newVertexCount = _verticesUsed - lines;
            if (newVertexCount < 0)
            {
                return;
            }

            for (int i = _verticesUsed; i >= newVertexCount; i--)
            {
                _vertices[i] = Vector3.zero;
            }

            _verticesUsed = newVertexCount;

            UpdateMeshData();
        }

        public bool IsLastPositionEndOfLine()
        {
            if (_verticesUsed <= VertexIncrement)
            {
                return false;
            }

            Vector3 startPos = _vertices[_verticesUsed - 1];
            Vector3 endPos = _vertices[_verticesUsed - 4];
            return Equals(startPos, endPos);
        }

        public bool MarkerInitialized()
        {
            return _vertices.Length != 0 && _verticesUsed != 0;
        }


        public Vector3[] GetSyncLines()
        {
            var arr = new Vector3[_syncLinesUsed];
            Array.Copy(_syncLines, arr, _syncLinesUsed);
            return arr;
        }

        public void ResetSyncLines()
        {
            _syncLinesUsed = 0;
        }

        private void UpdateArraySize(int verticesReserved, int trianglesReserved)
        {
            const int multiplier = 100;
            trianglesReserved *= multiplier;
            verticesReserved *= multiplier;

            int vCount = _verticesUsed + verticesReserved;
            if (vCount > _vertices.Length)
            {
                if (vCount > _vertexLimit)
                {
                    _verticesUsed = 0;
                    _trianglesUsed = 0;
                    return;
                }
                _vertices = ResizeArray(_vertices, verticesReserved);
                _normals = ResizeArray(_normals, verticesReserved);
                //_uv = ResizeArray(_uv, verticesReserved);
            }

            if (_trianglesUsed + trianglesReserved > _triangles.Length)
            {
                _triangles = ResizeArray(_triangles, trianglesReserved);
            }
        }

        private static T[] ResizeArray<T>(T[] sourceArray, int incrementSize)
        {
            var newArray = new T[incrementSize + sourceArray.Length];
            Array.Copy(sourceArray, newArray, sourceArray.Length);
            return newArray;
        }

        /// <summary>
        /// Fix culling and other issues caused by transforms not being at zero when moving the root after Start.
        /// </summary>
        public void ResetTransforms()
        {
            ResetTRS(transform);
            ResetTRS(trailStorage.transform);
        }

        private static void ResetTRS(Transform transform)
        {
            var parent = transform.parent;
            transform.parent = null;
            transform.localScale = Vector3.one;
            transform.parent = parent;

            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}