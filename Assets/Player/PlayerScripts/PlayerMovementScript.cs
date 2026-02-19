using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementScript : MonoBehaviour
{
    public Rigidbody rb;
    public float moveSpeed;
    public float sprintSpeed;
    public float rotateSpeed;
    public float jumpHeight;
    public InputActionReference move;
    public InputActionReference jump;
    public InputActionReference sprint;
    public Animator animator;
    
    private Vector3 _moveDirection;
    private bool _isGrounded;
    private bool _jumpPressed;
    private bool _isSprinting;
    
    private void Update()
    {
        Vector2 input = move.action.ReadValue<Vector2>();

        Transform cam = Camera.main.transform;
        
        Vector3 cameraForward = cam.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cam.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();
        
        _moveDirection = cameraForward * input.y + cameraRight * input.x;

        if (_moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
        }

        if (jump.action.WasPressedThisFrame() && _isGrounded)
            _jumpPressed = true;
        
        _isSprinting = sprint.action.IsPressed() && _isGrounded;
        animator.SetBool("isSprinting", _isSprinting);
    }

    private void FixedUpdate()
    {
        float speed = _isSprinting ? sprintSpeed : moveSpeed;
        
        rb.linearVelocity = new Vector3(
            _moveDirection.x * speed,
            rb.linearVelocity.y,
            _moveDirection.z * speed
        );
        
        if (_moveDirection != Vector3.zero)
        {
            animator.SetBool("isWalking", true);
            
            var targetRotation = Quaternion.LookRotation(new Vector3
                (_moveDirection.x, 0, _moveDirection.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }

        if (_jumpPressed && _isGrounded)
        {
            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
            _isGrounded = false;
            _jumpPressed = false;
            animator.SetBool("jumpRequested", true);
        }

        if (rb.linearVelocity.y < 0)
        {
            animator.SetBool("jumpRequested", false);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == ("Ground"))
        {
            _isGrounded = true;
            animator.SetBool("isGrounded", true);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == ("Ground"))
        {
            _isGrounded = false;
            animator.SetBool("isGrounded", false);
            animator.SetBool("isWalking", false);
        }
    }
}
