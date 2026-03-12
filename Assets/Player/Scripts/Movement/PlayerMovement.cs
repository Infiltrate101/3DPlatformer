using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts.Movement
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed;
        public float walkSpeed;
        public float sprintSpeed;
        public float slideSpeed;
        public float wallrunSpeed;
        public float swingSpeed;
        public float speedIncreaseMultiplier;
        public float slopeIncreaseMultiplier;
        public float groundDrag;
        public bool activeGrapple;
        public bool swinging;
        public Transform orientation;
        public MovementState state;

        private float _desiredMoveSpeed;
        private float _lastDesiredMoveSpeed;
        private float _horizontalInput;
        private float _verticalInput;
        private bool _enableMovementOnNextTouch;
        private Rigidbody _rb;
        private Vector3 _moveDirection;
        private Vector3 _velocityToSet;
    
        [Header("Jumping")]
        public float jumpForce;
        public float jumpCooldown;
        public float airMultiplier;
    
        private bool _readyToJump;

        [Header("Crouching")]
        public float crouchSpeed;
        public float crouchYScale;
    
        private float _startYScale;

        [Header("Keybinds")]
        public InputActionReference moveAction;
        public InputActionReference jumpAction;
        public InputActionReference sprintAction;
        public InputActionReference crouchAction;

        [Header("Ground Check")]
        public float playerHeight;
        public LayerMask whatIsGround;
        public bool grounded;

        [Header("Slope Handling")]
        public float maxSlopeAngle;
    
        private RaycastHit _slopeHit;
        private bool _exitingSlope;
    
        public enum MovementState
        {
            Grappling,
            Swinging,
            Walking,
            Sprinting,
            Wallrunning,
            Crouching,
            Sliding,
            Air
        }

        public bool sliding;
        public bool wallrunning;

        private void Start()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
            sprintAction.action.Enable();
            crouchAction.action.Enable();
        
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;

            _readyToJump = true;

            _startYScale = transform.localScale.y;
        }

        private void Update()
        {
            // ground check
            grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

            MyInput();
            SpeedControl();
            StateHandler();

            // handle drag
            if (grounded && !activeGrapple)
                _rb.linearDamping = groundDrag;
            else
                _rb.linearDamping = 0;
        }

        private void FixedUpdate()
        {
            MovePlayer();
        }

        private void MyInput()
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();
        
            _horizontalInput = input.x;
            _verticalInput = input.y;
        

            // when to jump
            if(jumpAction.action.IsPressed() && _readyToJump && grounded)
            {
                _readyToJump = false;

                Jump();

                Invoke(nameof(ResetJump), jumpCooldown);
            }

            // start crouch
            if (crouchAction.action.WasPressedThisFrame())
            {
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            }

            // stop crouch
            if (crouchAction.action.WasReleasedThisFrame())
            {
                transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
            }
        }

        private void StateHandler()
        {
            //Mode - Grappling
            if (activeGrapple)
            {
                state = MovementState.Grappling;
                _desiredMoveSpeed = sprintSpeed;
            }
            
            else if (swinging)
            {
                state = MovementState.Swinging;
                _desiredMoveSpeed = swingSpeed;
            }

            //Mode - Wallrunning
            else if (wallrunning)
            {
                state = MovementState.Wallrunning;
                _desiredMoveSpeed = wallrunSpeed;
            }

            // Mode - Sliding
            else if (sliding)
            {
                state = MovementState.Sliding;

                if (OnSlope() && _rb.linearVelocity.y < 0.1f)
                    _desiredMoveSpeed = slideSpeed;

                else
                    _desiredMoveSpeed = sprintSpeed;
            }

            // Mode - Crouching
            else if (crouchAction.action.IsPressed())
            {
                state = MovementState.Crouching;
                _desiredMoveSpeed = crouchSpeed;
            }

            // Mode - Sprinting
            else if(grounded && sprintAction.action.IsPressed())
            {
                state = MovementState.Sprinting;
                _desiredMoveSpeed = sprintSpeed;
            }

            // Mode - Walking
            else if (grounded)
            {
                state = MovementState.Walking;
                _desiredMoveSpeed = walkSpeed;
            }

            // Mode - Air
            else
            {
                state = MovementState.Air;
            }

            // check if desiredMoveSpeed has changed drastically
            if(Mathf.Abs(_desiredMoveSpeed - _lastDesiredMoveSpeed) > ((sprintSpeed - walkSpeed) + 1) && moveSpeed != 0)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                moveSpeed = _desiredMoveSpeed;
            }

            _lastDesiredMoveSpeed = _desiredMoveSpeed;
        }

        private IEnumerator SmoothlyLerpMoveSpeed()
        {
            // smoothly lerp movementSpeed to desired value
            float time = 0;
            float difference = Mathf.Abs(_desiredMoveSpeed - moveSpeed);
            float startValue = moveSpeed;

            while (time < difference)
            {
                moveSpeed = Mathf.Lerp(startValue, _desiredMoveSpeed, time / difference);

                if (OnSlope())
                {
                    float slopeAngle = Vector3.Angle(Vector3.up, _slopeHit.normal);
                    float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                    time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
                }
                else
                    time += Time.deltaTime * speedIncreaseMultiplier;

                yield return null;
            }

            moveSpeed = _desiredMoveSpeed;
        }

        private void MovePlayer()
        {
            if (activeGrapple) return;
            
            // calculate movement direction
            _moveDirection = orientation.forward * _verticalInput + orientation.right * _horizontalInput;

            // on slope
            if (OnSlope() && !_exitingSlope)
            {
                _rb.AddForce(GetSlopeMoveDirection(_moveDirection) * (moveSpeed * 20f), ForceMode.Force);

                if (_rb.linearVelocity.y > 0)
                    _rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }

            // on ground
            else if(grounded)
                _rb.AddForce(_moveDirection.normalized * (moveSpeed * 10f), ForceMode.Force);

            // in air
            else if(!grounded)
                _rb.AddForce(_moveDirection.normalized * (moveSpeed * 10f * airMultiplier), ForceMode.Force);

            // turn gravity off while on slope
            if(!wallrunning) 
                _rb.useGravity = !OnSlope();
        }

        private void SpeedControl()
        {
            if (activeGrapple)
                return;
            
            // limiting speed on slope
            if (OnSlope() && !_exitingSlope)
            {
                if (_rb.linearVelocity.magnitude > moveSpeed)
                    _rb.linearVelocity = _rb.linearVelocity.normalized * moveSpeed;
            }

            // limiting speed on ground
            else
            {
                Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

                // limit velocity if needed
                if (flatVel.magnitude > moveSpeed)
                {
                    Vector3 limitedVel = flatVel.normalized * moveSpeed;
                    _rb.linearVelocity = new Vector3(limitedVel.x, _rb.linearVelocity.y, limitedVel.z);
                }
            }
        }

        private void Jump()
        {
            _exitingSlope = true;

            // reset y velocity
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

            _rb.AddForce(transform.up * (jumpForce + (moveSpeed / 2f)) , ForceMode.Impulse);
        }
        private void ResetJump()
        {
            _readyToJump = true;

            _exitingSlope = false;
        }

        public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
        {
            activeGrapple = true;
            
            _velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
            Invoke(nameof(SetVelocity), 0.1f);
            
            Invoke(nameof(ResetRestrictions), 3f);
        }

        private void SetVelocity()
        {
            _enableMovementOnNextTouch = true;
            _rb.linearVelocity = _velocityToSet;
        }

        public void ResetRestrictions()
        {
            activeGrapple = false;
        }

        private void OnCollisionEnter()
        {
            if (_enableMovementOnNextTouch)
            {
                _enableMovementOnNextTouch = false;
                ResetRestrictions();

                GetComponent<global::Player.Scripts.Grappling.Grappling>().StopGrapple();
            }
        }

        public bool OnSlope()
        {
            if(Physics.Raycast(transform.position, Vector3.down, out _slopeHit, playerHeight * 0.5f + 0.3f))
            {
                float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
                return angle < maxSlopeAngle && angle != 0;
            }

            return false;
        }

        public Vector3 GetSlopeMoveDirection(Vector3 direction)
        {
            return Vector3.ProjectOnPlane(direction, _slopeHit.normal).normalized;
        }

        public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
        {
            float gravity = Physics.gravity.y;
            float displacementY = endPoint.y - startPoint.y;
            Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

            Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
            Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
                                                   + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));
            
            return velocityXZ + velocityY;
        }
    }
}