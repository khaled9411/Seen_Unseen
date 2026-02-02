using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float accelerationTime = 0.1f;
    [SerializeField] private float decelerationTime = 0.2f;
    [SerializeField][Range(0f, 1f)] private float airControlPercentage = 0.6f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 4f;
    [SerializeField] private float timeToJumpApex = 0.4f;
    [SerializeField] private float fallMultiplier = 1.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Advanced Jump")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.4f, 0.1f);

    [Header("Visual Feedback")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private float squashAmount = 0.2f;
    [SerializeField] private float squashDuration = 0.1f;
    [SerializeField] private ParticleSystem dustParticles;
    [SerializeField] private ParticleSystem landingParticles;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;

    [Header("Screen Shake")]
    [SerializeField] private Camera mainCamera;

    // Components
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;
    private PlayerInput playerInput;
    private PlayerHealthController health;

    // Input
    private Vector2 moveInput;
    private bool jumpPressed;

    // Movement
    private float currentSpeed;
    private float velocityXSmoothing;
    private bool facingRight = true;

    // Jump
    private float gravity;
    private float jumpVelocity;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    // Animation Hashes
    private int idleHash;
    private int runHash;
    private int jumpHash;
    private int fallHash;
    private int landHash;

    private void Awake()
    {
        // Get Components
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
        playerInput = new PlayerInput();
        health = GetComponent<PlayerHealthController>();

        // Setup Rigidbody
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Setup Visual Transform if not assigned
        if (visualTransform == null)
        {
            GameObject visualGO = new GameObject("Visual");
            visualTransform = visualGO.transform;
            visualTransform.SetParent(transform);
            visualTransform.localPosition = Vector3.zero;

            // Move sprite renderer to visual transform if exists
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.transform.SetParent(visualTransform);
            }
        }

        // Setup Camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Setup Audio
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Calculate Jump Physics
        CalculateJumpPhysics();

        // Cache Animation Hashes
        idleHash = Animator.StringToHash("Idle");
        runHash = Animator.StringToHash("Run");
        jumpHash = Animator.StringToHash("Jump");
        fallHash = Animator.StringToHash("Fall");
        landHash = Animator.StringToHash("Land");
    }

    private void OnEnable()
    {
        playerInput.Enable();
        playerInput.Player.Move.performed += OnMove;
        playerInput.Player.Move.canceled += OnMove;
        playerInput.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        playerInput.Player.Move.performed -= OnMove;
        playerInput.Player.Move.canceled -= OnMove;
        playerInput.Player.Jump.performed -= OnJump;
        playerInput.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        jumpPressed = true;
        jumpBufferCounter = jumpBufferTime;
    }

    private void Update()
    {
        CheckGrounded();
        UpdateTimers();
        if (health != null && health.canMove)
            HandleJump();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if(health != null && health.canMove)
            HandleMovement();
        ApplyGravity();
    }

    private void CalculateJumpPhysics()
    {
        // Calculate gravity and jump velocity for desired arc
        gravity = (2 * jumpHeight) / (timeToJumpApex * timeToJumpApex);
        jumpVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);

        Debug.Log($"Calculated Gravity: {gravity} | Jump Velocity: {jumpVelocity}");
        Debug.Log($"Recommended Rigidbody2D Gravity Scale: 0 (we handle gravity manually)");
    }

    private void CheckGrounded()
    {
        wasGrounded = isGrounded;

        // Ground check using BoxCast from bottom of capsule
        Vector2 boxCenter = (Vector2)transform.position + capsuleCollider.offset + Vector2.down * (capsuleCollider.size.y / 2);
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCenter,
            groundCheckSize,
            0f,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        isGrounded = hit.collider != null;

        // Landing feedback
        if (isGrounded && !wasGrounded)
        {
            OnLanded();
        }
    }

    private void UpdateTimers()
    {
        // Coyote Time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump Buffer
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        float targetSpeed = moveInput.x * maxSpeed;

        // Apply air control reduction
        float currentAcceleration = accelerationTime;
        float currentDeceleration = decelerationTime;

        if (!isGrounded)
        {
            currentAcceleration /= airControlPercentage;
            currentDeceleration /= airControlPercentage;
        }

        // Smooth acceleration/deceleration
        if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            // Accelerating
            currentSpeed = Mathf.SmoothDamp(
                currentSpeed,
                targetSpeed,
                ref velocityXSmoothing,
                currentAcceleration
            );
        }
        else
        {
            // Decelerating
            currentSpeed = Mathf.SmoothDamp(
                currentSpeed,
                0f,
                ref velocityXSmoothing,
                currentDeceleration
            );
        }

        // Apply horizontal velocity
        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);

        // Handle flipping
        if (currentSpeed > 0.1f && !facingRight)
        {
            Flip();
        }
        else if (currentSpeed < -0.1f && facingRight)
        {
            Flip();
        }

        // Dust particles when moving on ground
        if (isGrounded && Mathf.Abs(currentSpeed) > 0.5f)
        {
            if (dustParticles != null && !dustParticles.isPlaying)
            {
                dustParticles.Play();
            }
        }
        else
        {
            if (dustParticles != null && dustParticles.isPlaying)
            {
                dustParticles.Stop();
            }
        }
    }

    private void HandleJump()
    {
        // Jump if conditions are met
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            PerformJump();
            jumpBufferCounter = 0;
        }

        // Reset jump input
        if (jumpPressed)
        {
            jumpPressed = false;
        }
    }

    private void PerformJump()
    {
        // Set jump velocity
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);

        // Reset coyote time
        coyoteTimeCounter = 0;

        // Visual feedback - Squash down then stretch up
        if (visualTransform != null)
        {
            visualTransform.DOComplete();
            visualTransform.localScale = Vector3.one;

            Sequence jumpSequence = DOTween.Sequence();
            jumpSequence.Append(visualTransform.DOScale(new Vector3(1f + squashAmount, 1f - squashAmount, 1f), squashDuration / 2));
            jumpSequence.Append(visualTransform.DOScale(new Vector3(1f - squashAmount * 0.5f, 1f + squashAmount * 0.5f, 1f), squashDuration / 2));
            jumpSequence.Append(visualTransform.DOScale(Vector3.one, squashDuration));
        }

        // Play jump sound
        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }

        // Play dust particles
        if (dustParticles != null)
        {
            dustParticles.Play();
        }
    }

    private void ApplyGravity()
    {
        // Custom gravity for better jump feel
        if (rb.linearVelocity.y < 0)
        {
            // Falling - apply stronger gravity
            rb.linearVelocity += Vector2.up * (-gravity * fallMultiplier * Time.fixedDeltaTime);
        }
        else if (rb.linearVelocity.y > 0 && !playerInput.Player.Jump.IsPressed())
        {
            // Released jump button while going up - apply even stronger gravity for low jump
            rb.linearVelocity += Vector2.up * (-gravity * lowJumpMultiplier * Time.fixedDeltaTime);
        }
        else
        {
            // Normal gravity
            rb.linearVelocity += Vector2.up * (-gravity * Time.fixedDeltaTime);
        }

        // Terminal velocity (optional, adjust as needed)
        if (rb.linearVelocity.y < -25f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -25f);
        }
    }

    private void OnLanded()
    {
        // Visual feedback - Squash on landing
        if (visualTransform != null)
        {
            visualTransform.DOComplete();
            visualTransform.localScale = Vector3.one;

            Sequence landSequence = DOTween.Sequence();
            landSequence.Append(visualTransform.DOScale(new Vector3(1f + squashAmount, 1f - squashAmount, 1f), squashDuration));
            landSequence.Append(visualTransform.DOScale(Vector3.one, squashDuration));
        }

        // Play landing sound
        if (audioSource != null && landSound != null)
        {
            audioSource.PlayOneShot(landSound);
        }

        // Play landing particles
        if (landingParticles != null)
        {
            landingParticles.Play();
        }

        // Screen shake
        if (mainCamera != null)
        {
            ShakeCamera();
        }

        // Trigger land animation
        if (animator != null)
        {
            animator.Play(landHash);
        }
    }

    private void ShakeCamera()
    {
        //Vector3 originalPos = mainCamera.transform.position;

        //mainCamera.transform.DOComplete();
        //mainCamera.transform.DOShakePosition(shakeDuration, shakeStrength, 10, 90, false, true)
        //    .OnComplete(() => {
        //        mainCamera.transform.position = originalPos;
        //    });

        //mainCamera.GetComponentInParent<CameraFollow>()?.TriggerShake(shakeDuration, shakeStrength);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        if (isGrounded)
        {
            if (Mathf.Abs(currentSpeed) > 2f)
            {
                animator.Play(runHash);
            }
            else
            {
                animator.Play(idleHash);
            }
        }
        else
        {
            if (rb.linearVelocity.y > 0.1f)
            {
                animator.Play(jumpHash);
            }
            else
            {
                animator.Play(fallHash);
            }
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider2D>();
            if (capsuleCollider == null) return;
        }

        // Draw ground check box
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector2 boxCenter = (Vector2)transform.position + capsuleCollider.offset + Vector2.down * (capsuleCollider.size.y / 2 + groundCheckDistance / 2);
        Gizmos.DrawWireCube(boxCenter, groundCheckSize);
    }
}