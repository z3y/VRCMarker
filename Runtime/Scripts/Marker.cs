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
        public Erase erase;

        private float _cachedUpdateRate = 0;
        private const float RemoteUpdateRateMult = 2f; // fix for drawing more lines than synced lines

        private void Start()
        {
            _cachedUpdateRate = markerTrail.updateRate;
            markerTrail.updateRate = _cachedUpdateRate * RemoteUpdateRateMult;
            SetColor();

            if (Networking.IsOwner(Networking.LocalPlayer, gameObject))
            {
                erase.DisableInteractive = false;
            }
            else
            {
                erase.DisableInteractive = true;
            }
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
            markerTrail.updateRate = _cachedUpdateRate;
            markerTrail.isLocal = true;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Networking.SetOwner(Networking.LocalPlayer, markerSync.gameObject);
            Networking.SetOwner(Networking.LocalPlayer, erase.gameObject);
        }


        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (Networking.LocalPlayer.Equals(player))
            {
                erase.DisableInteractive = false;
            }
            else
            {
                erase.DisableInteractive = true;
            }
        }

        public override void OnDrop()
        {
            OnPickupUseUp();
            markerTrail.updateRate = _cachedUpdateRate * RemoteUpdateRateMult;
            markerTrail.isLocal = false;
            markerTrail.ResetSyncLines();
        }

        public void SetColor()
        {
            _CreateColorTexture();
        }

        private void OnDestroy()
        {
            if (_colorTexture)
            {
                Destroy(_colorTexture);
            }
        }

        Texture2D _colorTexture;
        [SerializeField] TextureWrapMode _colorTextureWrapMode = TextureWrapMode.Clamp;
        [SerializeField] string _colorTextureProperty = "_DetailAlbedoMap";
        void _CreateColorTexture()
        {
            if (!_colorTexture)
            {
                _colorTexture = new Texture2D(1, 32, TextureFormat.RGBA32, false, false);
            }
            _colorTexture.wrapMode = _colorTextureWrapMode;

            if (markerTrail.trailType == 0)
            {
                for (int i = 0; i < _colorTexture.height; i++)
                {
                    _colorTexture.SetPixel(0, i, markerTrail.color);
                }
            }
            else
            {
                for (int i = 0; i < _colorTexture.height; i++)
                {
                    float t = (float)i / _colorTexture.height;
                    _colorTexture.SetPixel(0, i, markerTrail.gradient.Evaluate(t));
                }
            }

            _colorTexture.Apply();
            var pb = new MaterialPropertyBlock();
            if (markerMesh.HasPropertyBlock())
            {
                markerMesh.GetPropertyBlock(pb);
            }
            pb.SetTexture(_colorTextureProperty, _colorTexture);
            markerMesh.SetPropertyBlock(pb);
        }

    }
}
