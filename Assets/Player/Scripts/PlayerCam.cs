using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;
    public Transform orientation;
    public Transform camHolder;
    public InputActionReference look;
    
    private float _xRotation;
    private float _yRotation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        look.action.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        //Get Mouse Input
        Vector2 input = look.action.ReadValue<Vector2>();
        
        float mouseX = input.x * sensX * Time.deltaTime;
        float mouseY = input.y * sensY * Time.deltaTime;
        
        _yRotation += mouseX;
        _xRotation -= mouseY;
        
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        
        //Rotate camera and orientation
        camHolder.rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0f, _yRotation, 0f);
    }

    public void DoFov(float endValue)
    {
        GetComponent<Camera>().DOFieldOfView(endValue, 0.25f);
    }

    public void DoTilt(float zTilt)
    {
        transform.DOLocalRotate(new Vector3(0f, 0f, zTilt), 0.25f);
    }
}
