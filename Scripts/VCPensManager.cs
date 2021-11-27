using System;
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace z3y.Pens
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class VCPensManager : UdonSharpBehaviour
    {
        public VCPensPen pens;

        [Header("Pen Settings")]
        [Range(0f, 0.2f)] [SerializeField] public float penSmoothing = 0.04f;

        
        [UdonSynced, NonSerialized] public Vector3[] linesArray = new Vector3[0];
        [UdonSynced, NonSerialized] public int serverTime;

        public override void OnDeserialization()
        {
            if (Networking.GetServerTimeInMilliseconds() - serverTime > 2000)
            {
                return;
            }

            if (Networking.LocalPlayer.IsOwner(pens.gameObject)) return;
            
            pens.HandleSerialization(linesArray);
            pens.StopWriting();
        }

        // public void SetColors() => pens.SetColorPropertyBlock();
        
    }
}