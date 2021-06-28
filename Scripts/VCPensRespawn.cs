
using System;
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
        [SerializeField] private GameObject penHolder;
        private VCPensPen[] _vcPens;
        private VCPensEraser[] _eraser;

        private void Start()
        {
            _vcPens = penHolder.GetComponentsInChildren<VCPensPen>();
            _eraser = penHolder.GetComponentsInChildren<VCPensEraser>();
        }


        public override void Interact()
        {
            RespawnPen();
        }

        public void RespawnPen()
        {
            foreach (var a in _vcPens)
            {
                a.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(a.Respawn));
            }

            foreach (var b in _eraser)
            {
                b.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(b.Respawn));
            }
        }
    }
}
