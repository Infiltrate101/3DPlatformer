using Player.Scripts.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts.Grappling
{
    public class Grappling : MonoBehaviour
    {
        [Header("References")] 
        public Transform cam;
        public Transform gunTip;
        public LayerMask whatIsGrappleable;
        public LineRenderer lr;
        public Swinging swinging;
    
        private PlayerMovement _pm;

        [Header("Grappling")] 
        public float maxGrappleDistance;
        public float grappleDelayTime;
        public float overshootYAxis;

        private Vector3 _grapplePoint;
        private bool _isGrappling;

        [Header("Cooldown")] 
        public float grappleCd;

        private float _grappleCdTimer;
    
        [Header("Input")]
        public InputActionReference grappleAction;
    
        private void Start()
        {
            _pm = GetComponent<PlayerMovement>();
            grappleAction.action.Enable();
        }

        private void Update()
        {
            if (grappleAction.action.WasPressedThisFrame())
                StartGrapple();
        
            if (_grappleCdTimer > 0)
                _grappleCdTimer -= Time.deltaTime;
        }

        private void LateUpdate()
        {
            if (_isGrappling)
                lr.SetPosition(0, gunTip.position);
        }

        private void StartGrapple()
        {
            if (_grappleCdTimer > 0)
                return;
        
            _isGrappling = true;

            grappleAction.action.Disable();
        
            if (swinging.PredictionHit.point != Vector3.zero)
            {
                _grapplePoint = swinging.PredictionHit.point;
                Invoke(nameof(ExecuteGrapple), grappleDelayTime);
            }
            else
            {
                _grapplePoint = cam.position + cam.forward * maxGrappleDistance;
                Invoke(nameof(StopGrapple), grappleDelayTime);
            }

            lr.enabled = true;
            lr.SetPosition(1, _grapplePoint);
        }

        private void ExecuteGrapple()
        {
        
            Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);
        
            float grapplePointRelativeYPos = _grapplePoint.y - lowestPoint.y;
            float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;
        
            if (grapplePointRelativeYPos < 0)
                highestPointOnArc = overshootYAxis;
        
            _pm.JumpToPosition(_grapplePoint, highestPointOnArc);
        
            Invoke(nameof(StopGrapple), 1f);
        
        }

        public void StopGrapple()
        {
            _isGrappling = false;
            _grappleCdTimer = grappleCd;
        
            lr.enabled = false;
            grappleAction.action.Enable();
        }
    }
}
