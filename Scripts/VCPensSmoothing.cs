
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace z3y
{
    public class VCPensSmoothing : UdonSharpBehaviour
    {

        [SerializeField] private VCPensManager penManager;
        [SerializeField] private VCPensPen pen;

        private float smoothTime;

        private void Start() {
            smoothTime = penManager.penSmoothing;
            if(smoothTime == 0f) {
                GetComponent<VCPensSmoothing>().enabled = false;
                transform.SetParent(pen.transform , false);
            }
        }


        private void LateUpdate()
        {
            if(pen.isHeld)transform.position = Vector3.Lerp(transform.position, pen.transform.position, Time.deltaTime / smoothTime);
        }
    }
}