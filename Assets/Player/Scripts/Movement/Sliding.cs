using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts.Movement
{
    public class Sliding : MonoBehaviour
    {
        [Header("References")]
        public Transform orientation;
        public Transform playerObj;
    
        private Rigidbody _rb;
        private PlayerMovement _pm;

        [Header("Sliding")]
        public float maxSlideTime;
        public float slideForce;
        public float slideYScale;
    
        private float _slideTimer;
        private float _startYScale;

        [Header("Input")]
        public InputActionReference slideAction;
        public InputActionReference moveAction;
    
        private float _horizontalInput;
        private float _verticalInput;


        private void Start()
        {
            moveAction.action.Enable();
            slideAction.action.Enable();
        
            _rb = GetComponent<Rigidbody>();
            _pm = GetComponent<PlayerMovement>();

            _startYScale = playerObj.localScale.y;
        }

        private void Update()
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();
        
            _horizontalInput = input.x;
            _verticalInput = input.y;

            if (slideAction.action.WasPressedThisFrame() && (_horizontalInput != 0 || _verticalInput != 0))
                StartSlide();

            if (slideAction.action.WasReleasedThisFrame() && _pm.sliding)
                StopSlide();
        }

        private void FixedUpdate()
        {
            if (_pm.sliding)
                SlidingMovement();
        }

        private void StartSlide()
        {
            _pm.sliding = true;

            playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
            _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

            _slideTimer = maxSlideTime;
        }

        private void SlidingMovement()
        {
            Vector3 inputDirection = orientation.forward * _verticalInput + orientation.right * _horizontalInput;

            // sliding normal
            if(!_pm.OnSlope() || _rb.linearVelocity.y > -0.1f)
            {
                _rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

                _slideTimer -= Time.deltaTime;
            }

            // sliding down a slope
            else
            {
                _rb.AddForce(_pm.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
            }

            if (_slideTimer <= 0)
                StopSlide();
        }

        private void StopSlide()
        {
            _pm.sliding = false;

            playerObj.localScale = new Vector3(playerObj.localScale.x, _startYScale, playerObj.localScale.z);
        }
    }
}
