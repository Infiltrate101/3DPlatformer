using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")] 
        public float walkSpeed;
        public float sprintSpeed;
        public float groundDrag;
        public Transform orientation;
        public MovementState movementState;
        
        private float _speed;
        private float _horizontalInput;
        private float _verticalInput;
        private Vector3 _moveDirection;
        private Rigidbody _rb;

        [Header("Jump")]
        public float jumpForce;
        public float jumpCooldown;
        public float airMultiplier;
        
        private bool _readyToJump;

        [Header("Crouch")] 
        public float crouchSpeed;
        public float crouchYScale;
        
        private float _startYScale;
        
        [Header("Keybindings")]
        public InputActionReference move;
        public InputActionReference sprint;
        public InputActionReference jump;
        public InputActionReference crouch;

        [Header("Ground Check")] 
        public float playerHeight;
        public LayerMask whatIsGround;
        
        private bool _isGrounded;
        
        [Header("Slope Handling")]
        public float maxSlopeAngle;

        private bool _exitingSlope;
        private RaycastHit _slopeHit;

        public enum MovementState
        {
            Walking,
            Sprinting,
            Crouching,
            Air
        }

        private void Start()
        {
            move.action.Enable();
            jump.action.Enable();
            sprint.action.Enable();
            crouch.action.Enable();
            
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
            
            _readyToJump = true;
            
            _startYScale = transform.localScale.y;
        }

        private void Update()
        {
            //Ground Check
            _isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
            Debug.DrawRay(transform.position, Vector3.down * (playerHeight * 0.5f + 0.2f), Color.red);
        
            MyInput();
            SpeedControl();
            StateHandler();
        
            //Drag
            if (_isGrounded)
                _rb.linearDamping = groundDrag;
            else
                _rb.linearDamping = 0;
        }

        private void FixedUpdate()
        {
            MovePlayer();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void MyInput()
        {
            Vector2 input = move.action.ReadValue<Vector2>();
        
            _horizontalInput = input.x;
            _verticalInput = input.y;

            //Jump
            if (jump.action.IsPressed() && _readyToJump && _isGrounded)
            {
                _readyToJump = false;
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            
            //Crouch
            if (crouch.action.WasPressedThisFrame())
            {
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            }

            if (crouch.action.WasReleasedThisFrame())
            {
                transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
            }
        }

        private void StateHandler()
        {
            if (crouch.action.IsPressed())
            {
                movementState = MovementState.Crouching;
                _speed = crouchSpeed;
            }

            //Sprint Mode
            else if (_isGrounded && sprint.action.IsPressed())
            {
                movementState = MovementState.Sprinting;
                _speed = sprintSpeed;
            }
            //Walk Mode
            else if (_isGrounded)
            {
                movementState = MovementState.Walking;
                _speed = walkSpeed;
            }
            //Air Mode
            else
            {
                movementState = MovementState.Air;
            }
        }

        private void MovePlayer()
        {
            _moveDirection = orientation.forward * _verticalInput + orientation.right * _horizontalInput;

            //On slope
            if (OnSlope() && !_exitingSlope)
            {
                _rb.AddForce(GetSlopeMoveDirection() * (_speed * 20f), ForceMode.Force);
                
                if(_rb.linearVelocity.y > 0)
                    _rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
            else if (_isGrounded)
            {
                _rb.AddForce(_moveDirection.normalized * (_speed * 10f), ForceMode.Force);
            }
            else if (!_isGrounded)
            {
                _rb.AddForce(_moveDirection.normalized * (_speed * 10f * airMultiplier), ForceMode.Force);
            }
            
            //Turn off gravity when on a slope
            _rb.useGravity = !OnSlope();
        }

        private void SpeedControl()
        {
            //Limiting speed on slopes
            if (OnSlope() && !_exitingSlope)
            {
                if (_rb.linearVelocity.magnitude > _speed)
                    _rb.linearVelocity = _rb.linearVelocity.normalized * _speed;
            }
            //Limit speed on ground or air
            else
            {
                Vector3 flatVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

                //Limit velocity
                if (flatVelocity.magnitude > _speed)
                {
                    Vector3 limitedVelocity = flatVelocity.normalized * _speed;
                    _rb.linearVelocity = new Vector3(limitedVelocity.x, _rb.linearVelocity.y, limitedVelocity.z);
                }
            }
        }

        private void Jump()
        {
            _exitingSlope = true;
            
            //Reset y velocity
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }

        private void ResetJump()
        {
            _readyToJump = true;
            _exitingSlope = false;
        }

        private bool OnSlope()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, playerHeight * 0.5f + 0.3f))
            {
                float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
                return angle < maxSlopeAngle && angle != 0;
            }
            return false;
        }

        private Vector3 GetSlopeMoveDirection()
        {
            return Vector3.ProjectOnPlane(_moveDirection, _slopeHit.normal).normalized;
        }
    }
}
