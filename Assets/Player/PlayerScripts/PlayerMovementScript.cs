using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementScript : MonoBehaviour
{
    public Rigidbody rb;
    public float moveSpeed;
    public float rotateSpeed;
    public float jumpHeight;
    public InputActionReference move;
    public InputActionReference jump;
    public Animator animator;
    public CharacterController controller;
    
    private Vector3 _moveDirection;
    private bool _isGrounded;
    private bool _jumpPressed;
    
    private void Update()
    {
        _moveDirection = move.action.ReadValue<Vector3>();

        if (jump.action.WasPressedThisFrame() && _isGrounded)
            _jumpPressed = true;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(
            _moveDirection.x * moveSpeed,
            rb.linearVelocity.y,
            _moveDirection.z * moveSpeed
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
        }

        //if (rb.linearVelocity.y < 0);
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
        }
    }
}
