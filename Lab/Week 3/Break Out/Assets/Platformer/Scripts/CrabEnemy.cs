using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class CrabEnemy : MonoBehaviour
{
    Rigidbody2D rb;
    [SerializeField] float moveSpeed = 2f;
    float direction = 1f; // 1 for right, -1 for left

    public Transform wallCheck;
    public float wallCheckRadius = 0.2f;
    public LayerMask wallLayer;
    bool onWall;

    public Transform player;
    [SerializeField] float chaseRange = 5f;


    public CrabState currentState;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentState = CrabState.Patrol;
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case CrabState.Patrol:
                Patrol();
                if (distance < chaseRange)
                {
                    currentState = CrabState.Chase;
                }
                break;

            case CrabState.Chase:
                ChasePlayer();

                if (distance >= chaseRange)
                {
                    currentState = CrabState.Patrol;
                }
                break;
        }
    }

    private void ChasePlayer()
    {
        float PlayerDirection = player.position.x > transform.position.x ? 1f : -1f;
        direction = PlayerDirection;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocityY);

    }

    private void Patrol()
    {
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocityY);

        onWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);
        if (onWall)
        {
            direction *= -1; // flip direction
            Flip();
        }
    }

    void Flip()
    {
        Vector3 scale = transform.localScale; // 1,1,1
        scale.x = direction; // flip the x scale to change direction
        transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Check if player is above the enemy
            float playerBottomY = collision.transform.position.y - collision.collider.bounds.extents.y;
            float enemyTopY = transform.position.y + GetComponent<Collider2D>().bounds.extents.y;

            // Player jumped on top of enemy
            if (playerBottomY > enemyTopY - 0.1f)
            {
                Die();

                // Give player a bounce
                Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 8f);
                }


            }
        }
    }
    public void Die()
    {
        // Play death animation or effect here if needed
        Destroy(gameObject); // Remove enemy from scene

    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
    }
}

public enum CrabState
{
    Patrol,
    Chase
}
