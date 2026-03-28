using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Attack Settings")]
    [SerializeField] private bool isAttacking;
    [SerializeField] private float attackCooldown = 1f; // Time in seconds between attacks
    [SerializeField] private float lastAttackTime; // Timer to track time since last attack

    Animator animator;
    private Rigidbody2D rb;

    // Called when the script instance is being loaded
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (rb == null)
        {
            Debug.LogError("Movement requires a Rigidbody2D on the same GameObject.");
        }
    }

    // Update is called once per frame
    // FPS is 100 - 200 - 60 - 120
    void Update() 
    {
        HandleMovement();
        HandleJump();
        HandleAttack();
    }

    // Called every fixed framerate frame, good for physics calculations
    //50 times per second by default, but can be changed in Time settings
    void FixedUpdate()
    {
        // check ground state using physics in FixedUpdate
        if (groundCheck != null)
        {
            bool groundResult = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            isGrounded = groundResult;

        }
    }

    void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking) // left mouse button
        {
            isAttacking = true;
            lastAttackTime = 0f; // reset cooldown timer
            animator.SetTrigger("Attack");
        }
        //1 sec -> 0.1, 0.2 0.33, 0.39 , after 1 real sec -> 1 value
        lastAttackTime = lastAttackTime + Time.deltaTime; // increment timer by time since last frame
        if(lastAttackTime >= attackCooldown)
        {
            isAttacking = false; // reset attack state after cooldown
        }
    }

    bool isIdle;
    private void HandleMovement()
    {
        float horizInput = Input.GetAxisRaw("Horizontal");
        Vector2 vel = rb.linearVelocity;
        vel.x = horizInput * moveSpeed;
        rb.linearVelocityX = vel.x;
        if(horizInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1); // flip sprite based on direction
            isIdle = false;
        }
        else if(horizInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1); // flip sprite based on direction
            isIdle = false;
        }
        else
        {
            isIdle = true;
        }
        animator.SetBool("IsIdle", isIdle);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y = jumpForce;
            rb.linearVelocityY = vel.y;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
