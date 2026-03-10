using UnityEngine;

public class Bricks : MonoBehaviour
{

    public int health = 2;
    private SpriteRenderer sr;

    // void Start()
    // {
    //     sr = GetComponent<SpriteRenderer>();
    //     UpdateColor();
    // }
    // void UpdateColor()
    // {
    //     if (health == 2)
    //         sr.color = Color.green;
    //     else if (health == 1)
    //         sr.color = Color.red;
    // }



    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            // health--;
            // UpdateColor();
            // if (health <= 0)
            // {
            //     Destroy(gameObject);
            // }
            Destroy(gameObject);
        }
    }
}
