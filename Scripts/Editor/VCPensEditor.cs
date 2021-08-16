using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace z3y
{
    [CustomEditor(typeof(VCPensManager))]
    public class VCPensEditor : Editor
    {
        public override void OnInspectorGUI(){
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();

            VCPensManager pensManager = (VCPensManager) target;

            if (EditorGUI.EndChangeCheck()) // had to make an inspector because SetKeys is not exposed to Udon
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(pensManager.penColor.Evaluate(0), 0.0f)},
                    new GradientAlphaKey[] { new GradientAlphaKey(1, 0.0f),}
                );
                pensManager.dotColor = gradient;

            }

            EditorGUILayout.HelpBox("Set Smoothing to 0 to completely disable the Update function", MessageType.Info);
        }

        private void SetDotColor()
        {
            
        }
    }
}