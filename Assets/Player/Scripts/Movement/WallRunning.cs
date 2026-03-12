using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts.Movement
{
    public class WallRunning : MonoBehaviour
    {
        [Header("Wallrunning")] 
        public float wallRunForce;
        public float maxWallRunTime;
        public float wallClimbSpeed;
        public float wallJumpUpForce;
        public float wallJumpSideForce;
        public LayerMask whatIsWall;
        public LayerMask whatIsGround;
    
        private float _wallRunTimer;
    
        [Header("Input")]
        public InputActionReference moveAction;
        public InputActionReference upRunAction;
        public InputActionReference downRunAction;
        public InputActionReference jumpAction;
    
        private float _horizontalInput;
        private float _verticalInput;
        private bool _upRunning;
        private bool _downRunning;

        [Header("Detection")] 
        public float wallCheckDistance;
        public float minJumpHeight;

        private bool _wallLeft;
        private bool _wallRight;
        private RaycastHit _leftWallHit;
        private RaycastHit _rightWallHit;
    
        [Header("Exiting")]
        public float exitWallTime;
    
        private float _exitWallTimer;
        private bool _exitingWall;

        [Header("Gravity")] 
        public float gravityCounterForce;
        public bool useGravity;

        [Header("References")] 
        public Transform orientation;
        public PlayerCam cam;
    
        private Rigidbody _rb;
        private PlayerMovement _pm;

        private void Start()
        {
            moveAction.action.Enable();
        
            _rb = GetComponent<Rigidbody>();
            _pm = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            CheckForWall();
            StateMachine();
        }

        private void FixedUpdate()
        {
            if (_pm.wallrunning)
                WallRunningMovement();
        }

        private void CheckForWall()
        {
            _wallRight = Physics.Raycast(transform.position, orientation.right, out _rightWallHit, wallCheckDistance, whatIsWall);
            _wallLeft = Physics.Raycast(transform.position, -orientation.right, out _leftWallHit, wallCheckDistance, whatIsWall);
        }

        private bool AboveGround()
        {
            return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
        }

        private void StateMachine()
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();
        
            _horizontalInput = input.x;
            _verticalInput = input.y;

            _upRunning = upRunAction.action.IsPressed();
            _downRunning = downRunAction.action.IsPressed();

            //State - Wallrunning
            if ((_wallLeft || _wallRight) && _verticalInput > 0 && AboveGround() && !_exitingWall)
            {
                if (!_pm.wallrunning)
                    StartWallRun();
            
                if(_wallRunTimer > 0)
                    _wallRunTimer -= Time.deltaTime;

                if (_wallRunTimer <= 0 && _pm.wallrunning)
                {
                    _exitingWall = true;
                    _exitWallTimer = exitWallTime;
                }

                if (jumpAction.action.WasPressedThisFrame())
                    WallJump();
            }
        
            //State 2 - Exiting
            else if (_exitingWall)
            {
                if (_pm.wallrunning)
                    StopWallRun();
            
                if(_exitWallTimer > 0)
                    if (_wallLeft || _wallRight)
                        _exitWallTimer = exitWallTime;
                _exitWallTimer -= Time.deltaTime;
            
                if(_exitWallTimer <= 0)
                    _exitingWall = false;
            }

            //State - None
            else
            {
                if(_pm.wallrunning)
                    StopWallRun();
            }
        }

        private void StartWallRun()
        {
            _pm.wallrunning = true;
            useGravity = true;
        
            _wallRunTimer = maxWallRunTime;
        
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        
            cam.DoFov(90f);
        
            if (_wallLeft) 
                cam.DoTilt(-5f);
        
            if (_wallRight) 
                cam.DoTilt(5f);
        }

        private void WallRunningMovement()
        {
            _rb.useGravity = useGravity;
        
            Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;
        
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
        
            if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
                wallForward = -wallForward;
        
            _rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
        
            if(_upRunning)
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, wallClimbSpeed, _rb.linearVelocity.z);
        
            if(_downRunning)
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -wallClimbSpeed, _rb.linearVelocity.z);
        
            if(!(_wallLeft && _horizontalInput > 0 && !(_wallRight && _horizontalInput < 0)))
                _rb.AddForce(-wallNormal * 100, ForceMode.Force);
        
            if(useGravity)
                _rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
        }

        private void StopWallRun()
        {
            _pm.wallrunning = false;
            useGravity = false;
            _rb.useGravity = useGravity;
        
            cam.DoFov(80f);
            cam.DoTilt(0f);
        }

        private void WallJump()
        {
            _pm.moveSpeed += 5;
        
            _exitingWall = true;
            _exitWallTimer = exitWallTime;
        
            Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;
            Vector3 forceToApply = (transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce) * (_pm.moveSpeed / 5f);
        
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(forceToApply, ForceMode.Impulse);
        }

    }
}
