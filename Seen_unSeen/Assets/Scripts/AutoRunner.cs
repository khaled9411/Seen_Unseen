using UnityEngine;

public class AutoRunner : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float landAnimDuration = 0.5f;

    [Header("Tags")]
    public string jumpTriggerTag = "JumpArea";
    public string groundTag = "Ground";

    private Rigidbody2D rb;
    private Animator anim;

    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        BackToRun();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
    }

    void Update()
    {
        if (!isGrounded && rb.linearVelocity.y < -0.1f)
        {
            anim.Play("Fall");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(jumpTriggerTag))
        {
            Jump();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag) && !isGrounded)
        {
            isGrounded = true;

            anim.Play("Land");

            CancelInvoke("BackToRun");

            Invoke("BackToRun", landAnimDuration);
        }
    }

    void Jump()
    {
        isGrounded = false;

        CancelInvoke("BackToRun");

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        anim.Play("Jump");
    }

    void BackToRun()
    {
        if (isGrounded)
        {
            anim.Play("Run");
        }
    }
}