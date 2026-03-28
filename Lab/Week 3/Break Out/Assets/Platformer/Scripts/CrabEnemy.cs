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
        scale.x = direction ; // flip the x scale to change direction
        transform.localScale = scale;
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
