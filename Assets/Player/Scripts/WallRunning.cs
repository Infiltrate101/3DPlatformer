using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevelPhysics2D;

public class WallRunning : MonoBehaviour
{
    [Header("Wallrunning")] 
    public float wallRunForce;
    public float maxWallRunTime;
    public float wallClimbSpeed;
    public LayerMask whatIsWall;
    public LayerMask whatIsGround;
    
    private float wallRunTimer;
    
    [Header("Input")]
    public InputActionReference moveAction;
    public InputActionReference upRunAction;
    public InputActionReference downRunAction;
    
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

    [Header("References")] 
    public Transform orientation;
    
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

        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround())
        {
            if (!pm.wallrunning)
                StartWallRun();
        }
        else
        {
            if(pm.wallrunning)
                StopWallRun();
        }
    }

    private void StartWallRun()
    {
        pm.wallrunning = true;
    }

    private void WallRunningMovement()
    {
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
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
    }

    private void StopWallRun()
    {
        pm.wallrunning = false;
        rb.useGravity = true;
    }

}
