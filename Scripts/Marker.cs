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

        private float cachedUpdateRate = 0;
        const float RemoteUpdateRateMult = 2f; // fix for drawing more lines than synced lines

        void Start()
        {
            cachedUpdateRate = markerTrail.updateRate;
            markerTrail.updateRate = cachedUpdateRate * RemoteUpdateRateMult;
            SetColor();
        }

        public override void OnPickupUseDown()
        {
            markerTrail.StartWriting();
        }

        public void StartWritingRemote()
        {
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
            markerTrail.updateRate = cachedUpdateRate;
            markerTrail.isLocal = true;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Networking.SetOwner(Networking.LocalPlayer, markerSync.gameObject);
            Networking.SetOwner(Networking.LocalPlayer, erase.gameObject);
        }

        public override void OnDrop()
        {
            OnPickupUseUp();
            markerTrail.updateRate = cachedUpdateRate * RemoteUpdateRateMult;
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
