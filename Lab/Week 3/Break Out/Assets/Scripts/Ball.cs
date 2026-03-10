// using UnityEngine;

// public class Ball : MonoBehaviour
// {
//     Rigidbody2D rigidbody2D;
//     public float initialSpeed = 5f;
//     Vector2 down = new Vector2(0, -1);
//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
//         rigidbody2D = GetComponent<Rigidbody2D>();
//         rigidbody2D.linearVelocity = down * initialSpeed;
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }
// }

using UnityEngine;

public class Ball : MonoBehaviour
{
    public float startSpeed = 5f;
    public float speedIncreaseRate = 0.1f;

    private Rigidbody2D rb;
    private bool hasStarted = false;
    Vector2 direction;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        direction = new Vector2(0f, -1f).normalized;
        rb.linearVelocity = direction * startSpeed;
    }

    void Update()
    {

        // if (!hasStarted && Input.GetKeyDown(KeyCode.Space))
        // {
        //     StartBall();
        // }
    }

    void StartBall()
    {
        hasStarted = true;

        float randomX = Random.Range(-1f, 1f);
        Vector2 direction = new Vector2(randomX, -1f).normalized;

        rb.linearVelocity = direction * startSpeed;
    }

    void FixedUpdate()
    {
        if (hasStarted)
        {
            rb.linearVelocity *= (1 + speedIncreaseRate * Time.fixedDeltaTime);
        }
    }
}
