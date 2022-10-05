
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace VRCMarker
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Marker : UdonSharpBehaviour
    {
        [Header("Only edit properties on the root marker object")]
        public MeshRenderer markerMesh;
        public MarkerTrail markerTrail;
        public MarkerSync markerSync;
        public EraseAll erase;

        void Start()
        {
            SetColor();
        }

        public override void OnPickupUseDown()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableMarker));
        }

        public override void OnPickupUseUp()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DisableMarker));
            markerSync.SendMarkerPositions();
        }


        public void EnableMarker() => markerTrail.enabled = true;
        public void DisableMarker() => markerTrail.enabled = false;

        public override void OnPickup()
        {
            markerTrail.isLocal = true;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Networking.SetOwner(Networking.LocalPlayer, markerSync.gameObject);
            Networking.SetOwner(Networking.LocalPlayer, erase.gameObject);
        }

        public override void OnDrop()
        {
            markerTrail.isLocal = false;
        }

        public void SetColor()
        {
            var pb = new MaterialPropertyBlock();
            if (markerMesh.HasPropertyBlock())
            {
                markerMesh.GetPropertyBlock(pb);
            }
            pb.SetColor("_Color", markerTrail.color);
            markerMesh.SetPropertyBlock(pb);
        }
    }
}
