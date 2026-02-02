using UnityEngine;
using DG.Tweening;

public class DraggablePlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    [SerializeField] private Transform rightLimit;
    [SerializeField] private Transform leftLimit;
    [SerializeField] private float smoothTime = 0.1f;

    [Header("Visual")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color dragColor = Color.cyan;
    [SerializeField] private float colorTransitionDuration = 0.2f;

    [Header("Player Detection")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float detectionHeight = 0.5f;

    private SpriteRenderer spriteRenderer;
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private Rigidbody2D rb;
    private Transform currentPlayer;

    private void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (leftLimit != null && rightLimit != null)
        {
            Debug.DrawLine(leftLimit.position, rightLimit.position, Color.green, 999f);
        }
    }

    private void Update()
    {
        CheckPlayerOnPlatform();
    }

    private void OnMouseDown()
    {
        isDragging = true;

        Vector3 mousePos = GetMouseWorldPosition();
        offset = transform.position - mousePos;

        int childes = transform.childCount;
        for (int i = 0; i < childes; i++)
        {
            spriteRenderer = transform.GetChild(i).GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.DOColor(dragColor, colorTransitionDuration);
            }
        }
        //transform.DOScale(Vector3.one * 1.05f, 0.1f);
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = GetMouseWorldPosition();
        Vector3 targetPos = mousePos + offset;

        if (leftLimit != null && rightLimit != null)
        {
            float clampedX = Mathf.Clamp(targetPos.x, leftLimit.position.x, rightLimit.position.x);
            targetPos = new Vector3(clampedX, transform.position.y, transform.position.z);
        }

        Vector3 newPosition = Vector3.Lerp(transform.position, targetPos, smoothTime * 60f * Time.deltaTime);

        Vector3 velocity = (newPosition - transform.position) / Time.deltaTime;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(velocity.x, rb.linearVelocity.y);
        }

        transform.position = newPosition;
    }

    private void OnMouseUp()
    {
        isDragging = false;

        int childes = transform.childCount;
        for (int i = 0; i < childes; i++)
        {
            spriteRenderer = transform.GetChild(i).GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.DOColor(normalColor, colorTransitionDuration);
            }
        }

        //transform.DOScale(Vector3.one, 0.1f);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    private void CheckPlayerOnPlatform()
    {
        BoxCollider2D platformCollider = GetComponent<BoxCollider2D>();
        if (platformCollider == null) return;

        Vector2 boxSize = new Vector2(platformCollider.bounds.size.x - 0.1f, detectionHeight);
        Vector2 boxCenter = new Vector2(transform.position.x, transform.position.y + platformCollider.bounds.extents.y);

        Collider2D hit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, playerLayer);

        if (hit != null)
        {
            if (currentPlayer == null)
            {
                currentPlayer = hit.transform;
                currentPlayer.SetParent(transform);
            }
        }
        else
        {
            if (currentPlayer != null)
            {
                currentPlayer.SetParent(null);
                currentPlayer = null;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (leftLimit != null && rightLimit != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leftLimit.position, rightLimit.position);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(leftLimit.position, 0.2f);
            Gizmos.DrawWireSphere(rightLimit.position, 0.2f);
        }

        BoxCollider2D platformCollider = GetComponent<BoxCollider2D>();
        if (platformCollider != null)
        {
            Gizmos.color = Color.green;
            Vector2 boxSize = new Vector2(platformCollider.bounds.size.x - 0.1f, detectionHeight);
            Vector2 boxCenter = new Vector2(transform.position.x, transform.position.y + platformCollider.bounds.extents.y);
            Gizmos.DrawWireCube(boxCenter, boxSize);
        }
    }
}