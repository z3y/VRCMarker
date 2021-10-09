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
        private static float inkWidth = 0.004f;
        private float minVertexDistance = 0.004f;
        bool firstTimeApply = true;
        public override void OnInspectorGUI()
        {
            
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            EditorGUILayout.Space();
            VCPensManager pensManager = (VCPensManager) target;

            if (firstTimeApply)
            {
                firstTimeApply = false;
                //inkWidth = pensManager.pens._trailRenderer.widthMultiplier;
                penColor = pensManager.pens._trailRenderer.colorGradient.Evaluate(0);
                minVertexDistance = pensManager.pens._trailRenderer.minVertexDistance;
            }

            
            inkWidth = EditorGUILayout.Slider("Ink Width",inkWidth, 0.001f, 0.01f);
            minVertexDistance = EditorGUILayout.Slider("Vertex Distance",minVertexDistance, 0.001f, 0.01f);
            penColor = EditorGUILayout.ColorField("Pen Color", penColor);
            
            if (EditorGUI.EndChangeCheck())
            {
                pensManager.penColor = penColor;
                pensManager.SetColors();
                
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(penColor, 0.0f)},
                    new GradientAlphaKey[] { new GradientAlphaKey(1, 0.0f),}
                );

                pensManager.pens._trailRenderer.widthMultiplier = inkWidth;
                pensManager.pens._trailRenderer.colorGradient = gradient;
                pensManager.pens._trailRenderer.minVertexDistance = minVertexDistance;
                pensManager.pens._trailRenderer.sharedMaterial.SetFloat("_Width", inkWidth);

                
            }
        }
    }
}