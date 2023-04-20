using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class BallTest : MonoBehaviour
{
    public float speedY;
    public float speedZ;

    private Rigidbody _rb;

    // Start is called before the first frame update
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _rb.velocity = new Vector3(0 * 10f, 9f, -12);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S)) {
            transform.position = new Vector3(0, 0.5f, 10);
            _rb.velocity = new Vector3(0, speedY, -speedZ);
        }
    }
}
