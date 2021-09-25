
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace z3y.Pens
{

    public class VCPensEraser : UdonSharpBehaviour
    {
        private bool _isErasing;
        [SerializeField] private Material defaultMat;
        [SerializeField] private Material highlightMat;
        

        private LineRenderer _lineRendererEnter;
        private LineRenderer _lineRendererExit;

        [SerializeField] private VRCObjectSync vrcObjectSync;

        public void Respawn()
        {
            vrcObjectSync.Respawn();
        }


        public override void OnPickupUseDown() => SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StartErasing));
        public override void OnPickupUseUp() => SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StopErasing));
        public void StartErasing() => _isErasing = true;
        public void StopErasing() => _isErasing = false;

        public override void OnPickup()
        {
            TransferOwnership();
        }

        private void TransferOwnership()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == 9 && other.gameObject.name.Contains("-ln"))
            {
                _lineRendererEnter = other.transform.gameObject.GetComponent<LineRenderer>();
                _lineRendererEnter.material = highlightMat;
                if (_isErasing) Destroy(other.transform.gameObject);

            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_isErasing && other.gameObject.layer == 9 && other.gameObject.name.Contains("-ln"))
                Destroy(other.transform.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == 9 && other.gameObject.name.StartsWith("-ln"))
            {
                _lineRendererExit = other.transform.gameObject.GetComponent<LineRenderer>();
                _lineRendererExit.material = defaultMat;
            }
        }
    }
}