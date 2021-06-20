using System;
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace z3y
{
    public class VCPensManager : UdonSharpBehaviour
    {
        [SerializeField] private VCPensPen pens;
        

        [Header("Pen Settings (Applied on Start)")]
        [SerializeField] private Gradient penColor;

        
        [Range(0.001f, 0.01f)] [SerializeField] private float inkWidth = 0.004f;
        
        [UdonSynced, NonSerialized] public Vector3[] linesArray = new Vector3[0];
        [UdonSynced, NonSerialized] public int serverTime;

            private void Start()
        {
            pens.PenInit(penColor, inkWidth);
        }


        public override void OnDeserialization()
        {
            if (Networking.GetServerTimeInMilliseconds() - serverTime > 2000)
            {
                return;
            }
            
            pens.HandleSerialization(linesArray);
            pens.StopWriting();
        }
    }
}