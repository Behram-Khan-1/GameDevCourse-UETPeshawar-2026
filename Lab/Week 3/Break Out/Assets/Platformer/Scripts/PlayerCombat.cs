using Unity.VisualScripting;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // Here you can add logic to damage the enemy, play an effect, etc.
            
            collision.GetComponent<CrabEnemy>().Die();
        }
    }
}
