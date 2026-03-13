using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    [Header("References")] 
    public InputActionReference shootAction;
    public Transform gunTip;
    public Transform cam;
    public Transform shotHit;

    [Header("Shooting")] 
    public float fireCd;
    public float maxShootDistance;
    
    private float fireTimer;
    
    void Start()
    {
        shootAction.action.Enable();
        shotHit.gameObject.SetActive(false);
    }
    
    void Update()
    {
        if (fireTimer > 0)
            fireTimer -= Time.deltaTime;
        
        if (shootAction.action.WasPressedThisFrame() && fireTimer <= 0)
        {
            ShootGun();
            fireTimer = fireCd;
        }
    }

    private void ShootGun()
    {
        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxShootDistance))
        {
            shotHit.gameObject.SetActive(true);
            shotHit.position = hit.point;
            Invoke(nameof(destroyShotHit), 0.5f);
        }
    }

    private void destroyShotHit()
    {
        shotHit.gameObject.SetActive(false);
    }
}
