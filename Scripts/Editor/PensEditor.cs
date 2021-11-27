using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace z3y.Pens
{
    [CustomEditor(typeof(VCPensManager))]
    public class PensEditor : Editor
    {
        private Color penColor = Color.white;
        private float minVertexDistance = 0.004f;
        bool firstTimeApply = true;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            VCPensManager pensManager = (VCPensManager) target;

            if (firstTimeApply)
            {
                firstTimeApply = false;
                penColor = pensManager.pens._trailRenderer.colorGradient.Evaluate(0);
                minVertexDistance = pensManager.pens._trailRenderer.minVertexDistance;
            }

            EditorGUI.BeginChangeCheck();
            

            minVertexDistance = EditorGUILayout.Slider("Vertex Distance",minVertexDistance, 0.001f, 0.01f);
            penColor = EditorGUILayout.ColorField("Pen Color", penColor);
            
            if (EditorGUI.EndChangeCheck())
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(penColor, 0.0f)},
                    new GradientAlphaKey[] { new GradientAlphaKey(1, 0.0f),}
                );
                
                pensManager.pens._trailRenderer.colorGradient = gradient;
                pensManager.pens._trailRenderer.minVertexDistance = minVertexDistance;
                pensManager.pens.SetColorPropertyBlock();
            }
        }

        private void OnValidate()
        {
            VCPensManager pensManager = (VCPensManager) target;
            pensManager.pens.SetColorPropertyBlock();
        }
        
    }
}