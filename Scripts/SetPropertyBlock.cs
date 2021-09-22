#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace z3y.Pens
{
    [ExecuteInEditMode]
    public class SetPropertyBlock : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trailRenderer;
        void Start()
        {
            Color penColor = _trailRenderer.colorGradient.Evaluate(0);
            Renderer _renderer = GetComponent<Renderer>();
            MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
            _propertyBlock.SetColor("_InkColor", penColor);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

    }
}
#endif