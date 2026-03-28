using UnityEngine;

public class EnemyCrab : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] public float chaseRange = 5f;
    public Transform player;

    
    Rigidbody2D rb;
    public CrabState currentState;

    float direction = 1f; // 1 = right, -1 = left


    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentState = CrabState.Patrol;
    }


    void Update()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case CrabState.Patrol:
                Patrol();

                if (distance < chaseRange)
                    currentState = CrabState.Chase;
                break;

            case CrabState.Chase:
                Chase();

                if (distance > chaseRange)
                    currentState = CrabState.Patrol;
                break;
        }
    }

    public bool hasGround = false;
    void Patrol()
    {
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        hasGround = Physics2D.OverlapCircle( groundCheck.position, checkRadius, groundLayer );

        if (hasGround)
        {
            direction *= -1;
            Flip();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        
    }

    void Chase()
    {
        float Playerdir = player.position.x > transform.position.x ? 1 : -1;

        direction = Playerdir; // IMPORTANT → updates patrol direction later

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        Flip();
    }

    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x = direction;
        transform.localScale = scale;
    }
}


public enum CrabState
{
    Patrol,
    Chase
}