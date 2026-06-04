using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform shootPoint;

    public float fireRate = 0.2f;
    private float nextFireTime;

    public int maxAmmo = 20;
    private int currentAmmo;

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Mouse.current.leftButton.isPressed
           && Time.time >= nextFireTime)
        {
            Shoot();

            nextFireTime =
                Time.time + fireRate;
        }
    }

    void Shoot()
    {
        Instantiate(
            bulletPrefab,
            shootPoint.position,
            shootPoint.rotation);

        
    //     RaycastHit hit;

    //     if(Physics.Raycast( shootPoint.position, shootPoint.forward, out hit, 100f))
    //     {
    //         Debug.Log(hit.collider.name);
    //     }

    //     if(hit.collider.CompareTag("Enemy"))
    //     {
    //         Debug.Log("Enemy Hit!");
    //     }
    //     else
    //     {
    //         Debug.Log("Enemy Not Hit");
    //     }

    //     Debug.DrawRay(
    // shootPoint.position,
    // shootPoint.forward * 100,
    // Color.red,
    // 1f);
        

        currentAmmo--;
    }

    IEnumerator Reload()
    {
        yield return new WaitForSeconds(1f);

        currentAmmo = maxAmmo;
    }
}