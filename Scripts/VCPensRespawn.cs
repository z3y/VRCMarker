
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace z3y
{


    public class VCPensRespawn : UdonSharpBehaviour
    {
        [SerializeField] private VRCObjectSync pen;
        [SerializeField] private VCPensPen z3yPens;

        public override void Interact()
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(RespawnPen));
        }

        public void RespawnPen() => pen.Respawn();
    }
}
