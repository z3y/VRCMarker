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

        [Range(0.001f, 0.01f)] [SerializeField] private float inkWidth = 0.006f;
        [Range(0.001f, 0.01f)] [SerializeField] private float minVertexDistance = 0.004f;

        [Header("Set to 0 to completely disable")]
        [Range(0f, 0.2f)] [SerializeField] public float penSmoothing = 0.03f;


        
        [UdonSynced, NonSerialized] public Vector3[] linesArray = new Vector3[0];
        [UdonSynced, NonSerialized] public int serverTime;

        private void Start()
        {
            pens.PenInit(penColor, inkWidth, minVertexDistance);
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