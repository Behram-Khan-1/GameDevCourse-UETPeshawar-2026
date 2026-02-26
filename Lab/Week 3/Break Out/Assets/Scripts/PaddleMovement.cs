using UnityEngine;

public class PaddleMovement : MonoBehaviour
{
    Rigidbody2D rigidbody2D;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
    }
    public float movementSpeed = 5f;
    float movementDirection;

    // Update is called once per frame
    void Update()
    {
        movementDirection = Input.GetAxisRaw("Horizontal");
        if(movementDirection > 0 )
        {
            rigidbody2D.AddForce(Vector2.right * movementSpeed);
            // transform.Translate(Vector3.right * movementSpeed);
        }
        else if(movementDirection < 0)
        {
            rigidbody2D.AddForce(-Vector2.right * movementSpeed);
            // transform.Translate(-Vector3.right * movementSpeed);
        }
        else
        {
            rigidbody2D.linearVelocity = Vector2.zero;
        }

    }
}
