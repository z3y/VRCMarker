using System;
using System.Resources;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace z3y
{
    public class VCPensPen : UdonSharpBehaviour
    {
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private Transform penMesh;
        [SerializeField] private Transform lines;
        private Vector3[] _inkPositions;
        private MeshCollider _meshCollider;
        private int _lineNumber;
        private bool _isWriting;
        public bool isHeld;

        [SerializeField] private VRCObjectSync vrcObjectSync;

        private int _time;

        [SerializeField] private VCPensManager penManager;
    
        private GameObject _newLine;
        [SerializeField] private LineRenderer lineRendererPrefab;

        public void PenInit(Gradient inkColor, float inkWidth, float minVertexDistance)
        {
            _trailRenderer.widthMultiplier = inkWidth;
            _trailRenderer.colorGradient = inkColor;
            _trailRenderer.minVertexDistance = minVertexDistance;
            _time = Networking.GetServerTimeInMilliseconds();
            
           _SetVertexColor();
        }

        public override void OnPickup()
        {
            TransferOwnership();  
            
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnStartHolding));
        }

        private void TransferOwnership()
        {
            if (!Networking.IsOwner(penManager.gameObject)) Networking.SetOwner(Networking.LocalPlayer, penManager.gameObject);
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
            // not sure how much data i can send so theres a limit after which lines wont get sent over the network but handled locally
            // adjust if you think its too much / too little
            if(_trailRenderer.positionCount < 1000 && Networking.GetServerTimeInMilliseconds() - _time > 300) {
                GetLinePositions();
                CreateLineRenderer(_inkPositions);
            }
            else SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StopWritingWithoutSerializing));

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

        public void GetLinePositions()
        {
            _trailRenderer.AddPosition(_trailRenderer.transform.position);
            _inkPositions = new Vector3[_trailRenderer.positionCount];
            _trailRenderer.GetPositions(_inkPositions);
            Array.Reverse(_inkPositions);
        }


        public void StopWritingWithoutSerializing()
        {
            GetLinePositions();
            HandleSerialization(_inkPositions);
        }

        public void CreateLineRenderer(Vector3[] pos)
        {
            penManager.linesArray = pos;
            penManager.serverTime = Networking.GetServerTimeInMilliseconds();
            penManager.RequestSerialization();
            HandleSerialization(pos);
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

        public void Respawn()
        {
            if (!isHeld)
            {
                vrcObjectSync.Respawn();
            }
        }
    }
}
