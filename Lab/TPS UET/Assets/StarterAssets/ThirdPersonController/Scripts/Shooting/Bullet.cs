using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, 3f);
    }
  

    // Update is called once per frame
    void Update()
    {
        rb.linearVelocity = transform.forward * speed;
    }
}
