using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityController : MonoBehaviour
{
    public float moveSpeed;
    private Rigidbody2D rb2D;
    void Start()
    {
        //No gravity when we start
        Physics2D.gravity = new Vector2(0, 0);
        rb2D = gameObject.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        onMove();
        onJump();
    }

    void onMove()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            this.transform.position = new Vector2(this.transform.position.x, this.transform.position.y + moveSpeed);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            this.transform.position = new Vector2(this.transform.position.x, this.transform.position.y - moveSpeed);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            this.transform.position = new Vector2(this.transform.position.x + moveSpeed, this.transform.position.y);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            this.transform.position = new Vector2(this.transform.position.x - moveSpeed, this.transform.position.y);
        }
    }

    void onJump()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            rb2D.AddForce(new Vector2(1, 0) * moveSpeed, ForceMode2D.Impulse);
        }
    }
}
