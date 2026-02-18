using Unity.VisualScripting;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float forwardSpeed = 0.1f;
    public float sideSpeed = 0.1f;

    // public float scaleSpeed = 0.01f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.right * forwardSpeed); //Forward movement
        //(1,0,0) is right, (0,1,0) is up and (0,0,1) is forward
        if(Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.forward * sideSpeed); //Left and Right movement
        }
        if(Input.GetKey(KeyCode.D))
        {
            transform.Translate(-Vector3.forward * sideSpeed); //Left and Right movement
        }

        //  float X =Input.GetAxis("Horizontal");
        //  float Y = Input.GetAxis("Vertical");

        // if(Input.GetKey(KeyCode.A))
        // {
        //     transform.Translate(Vector3.left * speed);
        // }

        // if(Input.GetKey(KeyCode.D))
        // {
        //     transform.Translate(Vector3.right * speed);
        // }

        // // transform.position +=  transform.forward * speed;
                
        // transform.localScale = new Vector3(scaleSpeed * transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    void OnCollisionEnter(Collision collisionOther)
    {
        // Debug.Log(collisionOther.gameObject.name);
        // if(collisionOther.gameObject.name == "Obstacle")
        // {
        //     Debug.Log("Collided with Obstacle");
        // }

        if(collisionOther.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("Collided with Obstacle");
        }
      
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Coin"))
        {
            Debug.Log("Collided with Coin");
            Destroy(other.gameObject);
        }
    }
}
