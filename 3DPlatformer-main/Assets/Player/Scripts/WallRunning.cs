using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevelPhysics2D;

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
    
    private float wallRunTimer;
    
    [Header("Input")]
    public InputActionReference moveAction;
    public InputActionReference upRunAction;
    public InputActionReference downRunAction;
    public InputActionReference jumpAction;
    
    private float horizontalInput;
    private float verticalInput;
    private bool upRunning;
    private bool downRunning;

    [Header("Detection")] 
    public float wallCheckDistance;
    public float minJumpHeight;

    private bool wallLeft;
    private bool wallRight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    
    [Header("Exiting")]
    public float exitWallTime;
    
    private float exitWallTimer;
    private bool exitingWall;

    [Header("Gravity")] 
    public float gravityCounterForce;
    public bool useGravity;

    [Header("References")] 
    public Transform orientation;
    public PlayerCam cam;
    
    private Rigidbody rb;
    private PlayerMovement pm;

    private void Start()
    {
        moveAction.action.Enable();
        
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void FixedUpdate()
    {
        if (pm.wallrunning)
            WallRunningMovement();
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void StateMachine()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        
        horizontalInput = input.x;
        verticalInput = input.y;

        upRunning = upRunAction.action.IsPressed();
        downRunning = downRunAction.action.IsPressed();

        //State - Wallrunning
        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
        {
            if (!pm.wallrunning)
                StartWallRun();
            
            if(wallRunTimer > 0)
                wallRunTimer -= Time.deltaTime;

            if (wallRunTimer <= 0 && pm.wallrunning)
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            if (jumpAction.action.WasPressedThisFrame())
                WallJump();
        }
        
        //State 2 - Exiting
        else if (exitingWall)
        {
            if (pm.wallrunning)
                StopWallRun();
            
            if(exitWallTimer > 0)
                if (wallLeft || wallRight)
                    exitWallTimer = exitWallTime;
                exitWallTimer -= Time.deltaTime;
            
            if(exitWallTimer <= 0)
                exitingWall = false;
        }

        //State - None
        else
        {
            if(pm.wallrunning)
                StopWallRun();
        }
    }

    private void StartWallRun()
    {
        pm.wallrunning = true;
        useGravity = true;
        
        wallRunTimer = maxWallRunTime;
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        cam.DoFov(90f);
        
        if (wallLeft) 
            cam.DoTilt(-5f);
        
        if (wallRight) 
            cam.DoTilt(5f);
    }

    private void WallRunningMovement()
    {
        rb.useGravity = useGravity;
        
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
        
        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;
        
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
        
        if(upRunning)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, wallClimbSpeed, rb.linearVelocity.z);
        
        if(downRunning)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallClimbSpeed, rb.linearVelocity.z);
        
        if(!(wallLeft && horizontalInput > 0 && !(wallRight && horizontalInput < 0)))
            rb.AddForce(-wallNormal * 100, ForceMode.Force);
        
        if(useGravity)
            rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
    }

    private void StopWallRun()
    {
        pm.wallrunning = false;
        useGravity = false;
        rb.useGravity = useGravity;
        
        cam.DoFov(80f);
        cam.DoTilt(0f);
    }

    private void WallJump()
    {
        pm.moveSpeed += 5;
        
        exitingWall = true;
        exitWallTimer = exitWallTime;
        
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }

}
