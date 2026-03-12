using Player.Scripts.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts.Grappling
{
    public class Swinging : MonoBehaviour
    {
        [Header("Input")]
        public InputActionReference swing;

        [Header("References")] 
        public LineRenderer lr;
        public Transform gunTip;
        public Transform cam;
        public Transform player;
        public LayerMask whatIsGrappleable;
        public PlayerMovement pm;
        public InputActionReference moveAction;
        public InputActionReference shortenCableAction;
    
        [Header("Swinging")]
        public float maxSwingDistance;
        public float maxJointDistance;
        public float minJointDistance;
        public float jointSpring;
        public float jointDamper;
        public float jointMassScale;
    
        private Vector3 _swingPoint;
        private SpringJoint _joint;
        private Vector3 _currentGrapplePosition;

        [Header("Air Movement")] 
        public Transform orientation;
        public Rigidbody rb;
        public float horizontalThrustForce;
        public float forwardThrustForce;
        public float extendCableSpeed;

        [Header("Prediction")] 
        public RaycastHit PredictionHit;
        public float predictionSphereCastRadius;
        public Transform predictionPoint;

        private void Start()
        {
            swing.action.Enable();
            moveAction.action.Enable();
            shortenCableAction.action.Enable();
        }

        private void Update()
        {
            if (swing.action.WasPressedThisFrame())
                StartSwing();
        
            if (swing.action.WasReleasedThisFrame())
                StopSwing();
        
            CheckForSwingPoints();
        
            if (_joint != null )
                AirMovement();
        }

        void LateUpdate()
        {
            DrawRope();
        }

        private void StartSwing()
        {
            if (PredictionHit.point == Vector3.zero)
                return;

            if (GetComponent<global::Player.Scripts.Grappling.Grappling>() != null)
            {
                GetComponent<global::Player.Scripts.Grappling.Grappling>().StopGrapple();
                pm.ResetRestrictions();
            }
        
            pm.swinging = true;
        
            _swingPoint = PredictionHit.point;
            _joint = player.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _swingPoint;
        
            float distanceFromPoint = Vector3.Distance(player.position, _swingPoint);
        
            _joint.maxDistance = distanceFromPoint * maxJointDistance;
            _joint.minDistance = distanceFromPoint * minJointDistance;

            _joint.spring = jointSpring;
            _joint.damper = jointDamper;
            _joint.massScale = jointMassScale;
        
            lr.positionCount = 2;
            _currentGrapplePosition = gunTip.position;
        
        }

        private void StopSwing()
        {
            pm.swinging = false;
            lr.positionCount = 0;
            Destroy(_joint);
        }

        private void DrawRope()
        {
            if (!_joint)
                return;

            _currentGrapplePosition = Vector3.Lerp(_currentGrapplePosition, _swingPoint, Time.deltaTime * 8f);
        
            lr.SetPosition(0, gunTip.position);
            lr.SetPosition(1, _currentGrapplePosition);
        }
    
        private void AirMovement()
        {
            Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

            if (moveInput.x < 0)
                rb.AddForce(-orientation.right * (-horizontalThrustForce * Time.deltaTime));
        
            if (moveInput.x > 0)
                rb.AddForce(orientation.right * (horizontalThrustForce * Time.deltaTime));
        
            if (moveInput.y < 0)
                rb.AddForce(orientation.forward * (forwardThrustForce * Time.deltaTime));
        
            //Shorten Cable
            if (shortenCableAction.action.IsPressed())
            {
                Vector3 directionToPoint = _swingPoint - transform.position;
                rb.AddForce(directionToPoint.normalized * (forwardThrustForce * Time.deltaTime));
            
                float distanceFromPoint = Vector3.Distance(transform.position, _swingPoint);
            
                _joint.maxDistance = distanceFromPoint * maxJointDistance;
                _joint.minDistance = distanceFromPoint * minJointDistance;
            }

            if (moveInput.y > 0)
            {
                float extendedDistanceFromPoint = Vector3.Distance(transform.position, _swingPoint) + extendCableSpeed;
            
                _joint.maxDistance = extendedDistanceFromPoint * maxJointDistance;
                _joint.minDistance = extendedDistanceFromPoint * minJointDistance;
            }
        }

        private void CheckForSwingPoints()
        {
            if (_joint != null)
                return;

            RaycastHit sphereCastHit;
            Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward, 
                out sphereCastHit, maxSwingDistance, whatIsGrappleable);

            RaycastHit raycastHit;
            Physics.Raycast(cam.position, cam.forward, 
                out raycastHit, maxSwingDistance, whatIsGrappleable);

            Vector3 realHitPoint;
        
            if (raycastHit.point != Vector3.zero)
                realHitPoint = raycastHit.point;
        
            else if (sphereCastHit.point != Vector3.zero)
                realHitPoint = sphereCastHit.point;
        
            else 
                realHitPoint = Vector3.zero;

            if (realHitPoint != Vector3.zero)
            {
                predictionPoint.gameObject.SetActive(true);
                predictionPoint.position = realHitPoint;
            }
            else
                predictionPoint.gameObject.SetActive(false);
        
            PredictionHit = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
        }
    }
}
