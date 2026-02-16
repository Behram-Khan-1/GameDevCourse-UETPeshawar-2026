using UnityEngine;

public class Movement : MonoBehaviour
{
    public float speed = 0.1f;
    // public float scaleSpeed = 0.01f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.right * speed);

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
}
