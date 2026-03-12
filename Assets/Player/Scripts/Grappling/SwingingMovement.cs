using Player.Scripts.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts.Grappling
{
    public class SwingingMovement : MonoBehaviour
    {
        [Header("References")]
        public LineRenderer lr;
        public Transform gunTip, cam, player;
        public LayerMask whatIsGrappleable;
        public PlayerMovement pm;

        [Header("Swinging")]
        private Vector3 _currentGrapplePosition;
        private float _maxSwingDistance = 25f;
        private Vector3 _swingPoint;
        private SpringJoint _joint;

        [Header("OdmGear")]
        public Transform orientation;
        public Rigidbody rb;
        public float horizontalThrustForce;
        public float forwardThrustForce;
        public float extendCableSpeed;

        [Header("Prediction")]
        public RaycastHit PredictionHit;
        public float predictionSphereCastRadius;
        public Transform predictionPoint;

        [Header("Input")]
        public InputActionReference swingAction;


        private void Update()
        {
            if (swingAction.action.WasPressedThisFrame()) StartSwing();
            if (swingAction.action.WasReleasedThisFrame()) StopSwing();

            CheckForSwingPoints();

            if (_joint != null) OdmGearMovement();
        }

        private void LateUpdate()
        {
            DrawRope();
        }

        private void CheckForSwingPoints()
        {
            if (_joint != null) return;

            RaycastHit sphereCastHit;
            Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward, 
                out sphereCastHit, _maxSwingDistance, whatIsGrappleable);

            RaycastHit raycastHit;
            Physics.Raycast(cam.position, cam.forward, 
                out raycastHit, _maxSwingDistance, whatIsGrappleable);

            Vector3 realHitPoint;

            // Option 1 - Direct Hit
            if (raycastHit.point != Vector3.zero)
                realHitPoint = raycastHit.point;

            // Option 2 - Indirect (predicted) Hit
            else if (sphereCastHit.point != Vector3.zero)
                realHitPoint = sphereCastHit.point;

            // Option 3 - Miss
            else
                realHitPoint = Vector3.zero;

            // realHitPoint found
            if (realHitPoint != Vector3.zero)
            {
                predictionPoint.gameObject.SetActive(true);
                predictionPoint.position = realHitPoint;
            }
            // realHitPoint not found
            else
            {
                predictionPoint.gameObject.SetActive(false);
            }

            PredictionHit = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
        }


        private void StartSwing()
        {
            // return if predictionHit not found
            if (PredictionHit.point == Vector3.zero) return;

            // deactivate active grapple
            if(GetComponent<Grappling>() != null)
                GetComponent<Grappling>().StopGrapple();
            pm.ResetRestrictions();

            pm.swinging = true;

            _swingPoint = PredictionHit.point;
            _joint = player.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _swingPoint;

            float distanceFromPoint = Vector3.Distance(player.position, _swingPoint);

            // the distance grapple will try to keep from grapple point. 
            _joint.maxDistance = distanceFromPoint * 0.8f;
            _joint.minDistance = distanceFromPoint * 0.25f;

            // customize values as you like
            _joint.spring = 4.5f;
            _joint.damper = 7f;
            _joint.massScale = 4.5f;

            lr.positionCount = 2;
            _currentGrapplePosition = gunTip.position;
        }

        public void StopSwing()
        {
            pm.swinging = false;

            lr.positionCount = 0;

            Destroy(_joint);
        }

        private void OdmGearMovement()
        {

            if (Keyboard.current.dKey.isPressed) rb.AddForce(orientation.right * (horizontalThrustForce * Time.deltaTime));

            if (Keyboard.current.aKey.isPressed) rb.AddForce(-orientation.right * (horizontalThrustForce * Time.deltaTime));

            if (Keyboard.current.wKey.isPressed) rb.AddForce(orientation.forward * (horizontalThrustForce * Time.deltaTime));
            
            if (Keyboard.current.spaceKey.isPressed)
            {
                Vector3 directionToPoint = _swingPoint - transform.position;
                rb.AddForce(directionToPoint.normalized * (forwardThrustForce * Time.deltaTime));

                float distanceFromPoint = Vector3.Distance(transform.position, _swingPoint);

                _joint.maxDistance = distanceFromPoint * 0.8f;
                _joint.minDistance = distanceFromPoint * 0.25f;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                float extendedDistanceFromPoint = Vector3.Distance(transform.position, _swingPoint) + extendCableSpeed;

                _joint.maxDistance = extendedDistanceFromPoint * 0.8f;
                _joint.minDistance = extendedDistanceFromPoint * 0.25f;
            }
        }

        private void DrawRope()
        {
            if (!_joint) return;

            _currentGrapplePosition = 
                Vector3.Lerp(_currentGrapplePosition, _swingPoint, Time.deltaTime * 8f);

            lr.SetPosition(0, gunTip.position);
            lr.SetPosition(1, _currentGrapplePosition);
        }
    }
}

