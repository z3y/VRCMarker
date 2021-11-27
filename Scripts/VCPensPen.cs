using System;
using System.Resources;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace z3y.Pens
{

    public class VCPensPen : UdonSharpBehaviour
    {
        public TrailRenderer _trailRenderer;
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

        private MaterialPropertyBlock _propertyBlock;
        [SerializeField] private Renderer _renderer;

        [SerializeField] private Transform _inkPosition;
        


        private void Start()
        {
            _time = Networking.GetServerTimeInMilliseconds();
            SetColorPropertyBlock();
#if !UNITY_ANDROID
            _trailRenderer.widthMultiplier = 0;
#endif
            
        }

        public void SetColorPropertyBlock()
        {
            Color penColor = _trailRenderer.colorGradient.Evaluate(0);
            _propertyBlock = new MaterialPropertyBlock();
            _propertyBlock.SetColor("_InkColor", penColor);
            _renderer.SetPropertyBlock(_propertyBlock);
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
        
        public override void OnPickupUseDown()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StartWriting));
            _trailRenderer.AddPosition(_trailRenderer.transform.position);
        }

        public void StartWriting()
        {
            _trailRenderer.transform.position = _inkPosition.transform.position;
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
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StopWriting));
            }
            else SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StopWritingWithoutSerializing));

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
            _trailRenderer.AddPosition(_trailRenderer.transform.position); // add a position to the end even if the min distance is not reached
            _inkPositions = new Vector3[_trailRenderer.positionCount];
            _trailRenderer.GetPositions(_inkPositions);
            Array.Reverse(_inkPositions);
        }


        public void StopWritingWithoutSerializing()
        {
            //_trailRenderer.AddPosition(_trailRenderer.transform.position);
            GetLinePositions();
            HandleSerialization(_inkPositions);
            StopWriting();
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
            
            newLineRend.positionCount = pos.Length;
            newLineRend.SetPositions(pos);
#if UNITY_ANDROID         
            newLineRend.widthMultiplier = _trailRenderer.widthMultiplier;
#else
            if (pos.Length < 3) newLineRend.numCapVertices = 1;
            newLineRend.widthMultiplier = 0.003f;
#endif
            var eraseCollider = new Mesh();
            newLineRend.BakeMesh(eraseCollider);
            _meshCollider = newLineRend.GetComponent<MeshCollider>();
            _meshCollider.sharedMesh = eraseCollider;
#if !UNITY_ANDROID
            newLineRend.numCapVertices = 0;
            newLineRend.widthMultiplier = _trailRenderer.widthMultiplier;
#endif
            
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
