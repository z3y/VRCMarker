
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
            markerTrail.StartWriting();
            markerSync.state = 0;
            markerSync.SyncMarker();
        }

        public override void OnPickupUseUp()
        {
            markerTrail.StopWriting();
            markerSync.SyncMarker();
        }

        public override void OnPickup()
        {
            markerTrail.isLocal = true;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Networking.SetOwner(Networking.LocalPlayer, markerSync.gameObject);
            Networking.SetOwner(Networking.LocalPlayer, erase.gameObject);
        }

        public override void OnDrop()
        {
            OnPickupUseUp();

            markerTrail.isLocal = false;
            markerTrail.ResetSyncLines();
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
