using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace z3y
{
    public class VCPensPen : UdonSharpBehaviour
    {
        private TrailRenderer _trailRenderer;
        [SerializeField] private Transform penMesh;
        [SerializeField] private Transform lines;
        private Vector3[] _inkPositions;
        private MeshCollider _meshCollider;
        private int _lineNumber;
        private bool _isWriting;
        public bool isHeld;

        private int _time;

        [SerializeField] private VCPensManager penManager;
    
        private GameObject _newLine;
        [SerializeField] private LineRenderer lineRendererPrefab;

        public void PenInit(Gradient inkColor, float inkWidth)
        {
            _trailRenderer = gameObject.GetComponent<TrailRenderer>();
            _trailRenderer.widthMultiplier = inkWidth;
            _trailRenderer.colorGradient = inkColor;
            _time = Networking.GetServerTimeInMilliseconds();
            
           _SetVertexColor();
        }

        public override void OnPickup()
        {
            if (!Networking.IsOwner(penManager.gameObject)) Networking.SetOwner(Networking.LocalPlayer, penManager.gameObject);
            
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnStartHolding));
        }

        public override void OnDrop() => SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnStopHolding));

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (isHeld) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnStartHolding));
        }

        public void OnStartHolding() => isHeld = true;
        public void OnStopHolding() => isHeld = false;

        private void _SetVertexColor()
        {
            Mesh mesh = penMesh.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Color[] colors = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) colors[i] = _trailRenderer.colorGradient.Evaluate((normals[i].x + 1) / 2);
            mesh.colors = colors;
        }

        public override void OnPickupUseDown()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StartWriting));
            _trailRenderer.AddPosition(transform.position);
            
        }

        public void StartWriting()
        {
           _trailRenderer.transform.position = transform.position;
            _trailRenderer.enabled = true;
            _isWriting = _trailRenderer.enabled;
            _trailRenderer.emitting = true;
        }

    
        public override void OnPickupUseUp()
        {
            _trailRenderer.AddPosition(_trailRenderer.transform.position);
            _inkPositions = new Vector3[_trailRenderer.positionCount];
            _trailRenderer.GetPositions(_inkPositions);
            CreateLineRenderer(_inkPositions);
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StopWriting));
            _time = Networking.GetServerTimeInMilliseconds();
        }

        public void StopWriting()
        {
            _trailRenderer.Clear();
            _trailRenderer.enabled = false;
            _isWriting = _trailRenderer.enabled;
            _trailRenderer.emitting = false;
        }

        public void CreateLineRenderer(Vector3[] pos)
        {
            if (pos.Length < 500 && Networking.GetServerTimeInMilliseconds() - _time > 200)
            {
                Array.Reverse(pos);
                penManager.linesArray = pos;
                penManager.serverTime = Networking.GetServerTimeInMilliseconds();
                penManager.RequestSerialization();
                HandleSerialization(pos);
            }
        }

        public void HandleSerialization(Vector3[] pos)
        {
            _newLine = VRCInstantiate(lineRendererPrefab.gameObject);
            _newLine.transform.SetParent(lines);
            _newLine.name = $"-ln{_lineNumber++}";
            var newLineRend = _newLine.GetComponent<LineRenderer>();
            newLineRend.colorGradient = _trailRenderer.colorGradient;
            newLineRend.widthMultiplier = _trailRenderer.widthMultiplier;

            newLineRend.positionCount = pos.Length;
            newLineRend.SetPositions(pos);

            var eraseCollider = new Mesh();
            newLineRend.BakeMesh(eraseCollider);
            _meshCollider = newLineRend.GetComponent<MeshCollider>();
            _meshCollider.sharedMesh = eraseCollider;
        }
    }
}