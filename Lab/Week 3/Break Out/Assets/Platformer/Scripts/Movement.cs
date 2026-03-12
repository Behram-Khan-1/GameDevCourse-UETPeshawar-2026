using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    Animator animator;
    private Rigidbody2D rb;
    private bool isGrounded;

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
    void Update()
    {
        HandleMovement();
        HandleJump();
    }

    void FixedUpdate()
    {
        // check ground state using physics in FixedUpdate
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
    }
        bool isIdle;
    private void HandleMovement()
    {
        float horiz = Input.GetAxisRaw("Horizontal");
        Vector2 vel = rb.linearVelocity;
        vel.x = horiz * moveSpeed;
        rb.linearVelocity = vel;
        if(horiz > 0)
        {
            transform.localScale = new Vector3(1, 1, 1); // flip sprite based on direction
            isIdle = false;
        }
        else if(horiz < 0)
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
            rb.linearVelocity = vel;
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
