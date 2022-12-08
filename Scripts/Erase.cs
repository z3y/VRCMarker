﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace VRCMarker
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Erase : UdonSharpBehaviour
    {
        public MarkerTrail markerTrail;

        private float _lastTime = 0;
        private bool _interactDown;

        const float HoldDelay = 0.3f;

        [UdonSynced] public Vector3 lastRemotePosition = Vector3.zero;
        [UdonSynced] public int eraseCount = 0;

        public override void Interact()
        {
            _lastTime = Time.time;
            _interactDown = true;
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (_interactDown && !value)
            {
                OnInteractUp();
                _interactDown = false;
            }
        }

        private void OnInteractUp()
        {
            float heldTime = Time.time - _lastTime;

            if (heldTime < HoldDelay)
            {
                Undo();
            }
            else
            {
                EraseAllNetworked();
            }
        }

        public void EraseAllNetworked() => SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EraseAll));
        public void EraseAll()
        {
            markerTrail.Clear();
        }

        public void Undo()
        {
            // first check if trail was synced, if the last position locally matches the remote one

            int length = markerTrail.RemoveLastLineConnection();
            eraseCount = length;
            lastRemotePosition = markerTrail.GetLastLinePosition();
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            var lastPositionLocal = markerTrail.GetLastLinePosition();
            if (Equals(lastPositionLocal, lastRemotePosition))
            {
                markerTrail.RemoveLastLines(eraseCount);
            }
        }
    }
}