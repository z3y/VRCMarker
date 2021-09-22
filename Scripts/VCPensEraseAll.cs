using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace z3y.Pens
{
    public class VCPensEraseAll : UdonSharpBehaviour
    {
        [SerializeField] private Transform lines;
        [SerializeField] private VCPensManager penManager;
        [SerializeField] private VCPensPen pens;


        public override void Interact()
        {
            if (Networking.IsOwner(penManager.gameObject) || !pens.isHeld) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(EraseAll));
        }

        public void EraseAll()
        {
            foreach (Transform child in lines)
            {
                Destroy(child.gameObject);
            }
            
        }
    }
}