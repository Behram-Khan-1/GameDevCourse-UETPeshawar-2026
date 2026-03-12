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
        // direction = new Vector2(0f, -1f); //Just to make the ball move download in the start, Changed to move down by space.
        // rb.linearVelocity = direction * startSpeed;
    }

    void Update()
    {
        // This runs once.
        if (!hasStarted && Input.GetKeyDown(KeyCode.Space)) //Ball moves when we press Space key, you can change it to any key you want
        {
            StartBall();
        }
    }


    void StartBall()
    {
        hasStarted = true;

        float randomX = Random.Range(-1f, 1f); //Picks a random X direction for the ball, so it doesn't always go straight down
        Vector2 direction = new Vector2(randomX, -1f); //Ball starts moving downwards, you can change it to any direction you want.

        rb.linearVelocity = direction * startSpeed;
    }

    //To increase speed of the ball over time, we can use FixedUpdate, which is called at a fixed interval and is used for physics updates. We can multiply the current velocity by a factor that increases over time.
    // void FixedUpdate()
    // {
    //     if (hasStarted)
    //     {
    //         rb.linearVelocity = rb.linearVelocity * (1f + speedIncreaseRate * Time.fixedDeltaTime);
    //         //5 * (1 + 0.1 * 0.02) = 5 * (1 + 0.002) = 5 * 1.002 = 5.01, so the speed increases by 0.01 every second, which is a 0.2% increase per second.
    //         // This will increase the speed of the ball by a certain percentage every second, based on the speedIncreaseRate variable. You can adjust the speedIncreaseRate to make the ball speed up faster or slower over time.
    //     }
    // }
}
