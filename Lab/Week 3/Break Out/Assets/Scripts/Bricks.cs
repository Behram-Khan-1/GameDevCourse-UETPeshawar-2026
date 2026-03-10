using UnityEngine;

public class Bricks : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Ball"))
        {
            Destroy(gameObject);
        }
    }
}
