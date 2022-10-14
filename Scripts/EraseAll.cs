using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace VRCMarker
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EraseAll : UdonSharpBehaviour
    {
        public MarkerTrail markerTrail;
        public VRC_Pickup markerPickup;

        public override void Interact()
        {
            if (markerPickup.IsHeld && !Networking.IsOwner(Networking.LocalPlayer, markerPickup.gameObject))
            {
                return;
            }

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ClearTrail));
        }

        public void ClearTrail()
        {

            markerTrail.Clear();
        }
    }
}
