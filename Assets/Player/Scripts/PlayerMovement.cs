using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        public float speed;
        public float groundDrag;
        public float jumpForce;
        public float jumpCooldown;
        public float airMultiplier;
        public Transform orientation;
        public InputActionReference move;
        public InputActionReference jump;

        [Header("Ground Check")] 
        public float playerHeight;
        public LayerMask whatIsGround;

        private float _horizontalInput;
        private float _verticalInput;
        private Vector3 _moveDirection;
        private Rigidbody _rb;
        private bool _isGrounded;
        private bool _readyToJump;
    

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
            move.action.Enable();
            jump.action.Enable();
            _readyToJump = true;
        }

        private void Update()
        {
            //Ground Check
            _isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
            Debug.DrawRay(transform.position, Vector3.down * (playerHeight * 0.5f + 0.2f), Color.red);
        
            MyInput();
            SpeedControl();
        
            //Drag
            if (_isGrounded)
                _rb.linearDamping = groundDrag;
            else
                _rb.linearDamping = 0;
            
            Debug.Log(_isGrounded);
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

            if (jump.action.WasPressedThisFrame() && _readyToJump && _isGrounded)
            {
                _readyToJump = false;
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }

        private void MovePlayer()
        {
            _moveDirection = orientation.forward * _verticalInput + orientation.right * _horizontalInput;

            switch (_isGrounded)
            {
                //On ground
                case true:
                    _rb.AddForce(_moveDirection.normalized * (speed * 10f), ForceMode.Force);
                    break;
                //In air
                case false:
                    _rb.AddForce(_moveDirection.normalized * (speed * 10f * airMultiplier), ForceMode.Force);
                    break;
            }
        }

        private void SpeedControl()
        {
            Vector3 flatVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

            //Limit velocity
            if (flatVelocity.magnitude > speed)
            {
                Vector3 limitedVelocity = flatVelocity.normalized * speed;
                _rb.linearVelocity = new Vector3(limitedVelocity.x, _rb.linearVelocity.y, limitedVelocity.z);
            }
        }

        private void Jump()
        {
            //Reset y velocity
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }

        private void ResetJump()
        {
            _readyToJump = true;
        }

    }
}
