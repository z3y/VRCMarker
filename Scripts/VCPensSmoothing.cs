
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace z3y.Pens
{
    public class VCPensSmoothing : UdonSharpBehaviour
    {

        [SerializeField] private VCPensManager penManager;
        [SerializeField] private VCPensPen pen;

        private float _smoothTime;

        private void Start() {
            _smoothTime = penManager.penSmoothing;
            if(_smoothTime == 0f)
            {
                transform.SetParent(pen.transform , false);
                GetComponent<VCPensSmoothing>().enabled = false;
            }
        }

        private void LateUpdate()
        {
            if(pen.isHeld)transform.position = Vector3.Lerp(transform.position, pen.transform.position, Time.deltaTime / _smoothTime);
        }
    }
}