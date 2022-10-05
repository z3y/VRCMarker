
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace VRCMarker
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class MarkerSync : UdonSharpBehaviour
    {
        public MarkerTrail markerTrail;

        [UdonSynced] public Vector3[] syncedLastTrailPoints = new Vector3[0];

        const int MaxSyncCount = 300;

        private readonly Vector3[] vector3a0 = new Vector3[0];
        public override void OnDeserialization()
        {
            markerTrail.enabled = false;
           
        }

        public void CreateMarkerTrail()
        {
            if (syncedLastTrailPoints.Length < 2)
            {
                markerTrail.AddEndLine();
                return;
            }

            markerTrail.verticesUsed = markerTrail.lastVerticesUsed;
            markerTrail.trianglesUsed = markerTrail.lastTrianglesUsed;

            markerTrail.CreateTrail(syncedLastTrailPoints);
            syncedLastTrailPoints = vector3a0;
        }

        public void SendMarkerPositions()
        {
            if (markerTrail.lastTrailPositions.Length > MaxSyncCount)
            {
                markerTrail.lastTrailPositions = vector3a0;
                return;
            }
            syncedLastTrailPoints = markerTrail.lastTrailPositions;
            RequestSerialization();

            markerTrail.lastTrailPositions = vector3a0;
        }

    }
}
