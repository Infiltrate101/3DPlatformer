using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementScript : MonoBehaviour
{
    public Rigidbody rb;
    public float speed;
    public float rotateSpeed;
    public float jumpHeight;
    public InputActionReference move;
    public InputActionReference jump;
    public InputActionReference sprint;
    public Animator animator;
    public float timer;
    
    private Vector3 _moveDirection;
    private bool _isGrounded;
    private bool _jumpPressed;
    private bool _canWallJump;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

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
    }

    private void FixedUpdate()
    {
        if (timer > 10)
        {
            if (sprint.action.IsPressed() && _isGrounded)
            {
                speed += 2f;
            }
            else if (speed < 6)
            {
                speed = 5;
            }
            else
            {
                speed -= 2f;
            }

            if (speed > 15 && animator.GetBool("isWalking"))
            {
                animator.SetBool("isSprinting", true);
            }
            else
            {
                animator.SetBool("isSprinting", false);
            }

            timer = 0;
        }
        else
        {
            timer += 1;
        }


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

        if (_jumpPressed && _isGrounded || _canWallJump && jump.action.WasPressedThisFrame())
        {
            rb.AddForce(Vector3.up * (jumpHeight + (speed/5)), ForceMode.Impulse);
            _isGrounded = false;
            _jumpPressed = false;
            animator.SetBool("jumpRequested", true);

            if (_canWallJump)
            {
                speed += 10f;
                var rotation = transform.rotation;
                rotation.y = rotation.y - 180f;
                transform.rotation = rotation;
                rb.AddForce(Vector3.forward * (jumpHeight + (speed/5)), ForceMode.Impulse);
                _canWallJump = false;
                Debug.Log(_canWallJump);
            }
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

        if (collision.gameObject.tag == ("Wall"))
        {
            _canWallJump = true;
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