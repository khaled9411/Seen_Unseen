using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealthController : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invulnerabilityDuration = 1.5f;

    [Header("Game Feel - Knockback")]
    [SerializeField] private Vector2 knockbackForce = new Vector2(5f, 3f);
    [SerializeField] private float knockbackControlLockTime = 0.2f;

    [Header("Game Feel - Juice")]
    [SerializeField] private float hitStopDuration = 0.1f;
    [SerializeField] private float shakeMagnitude = 0.1f;
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Events")]
    public UnityEvent OnTakeDamage;
    public UnityEvent OnHeal;
    public UnityEvent OnDeath;

    private int currentHealth;
    private bool isInvulnerable = false;
    private bool isDead = false;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Camera mainCam;
    private Vector3 originalCamPos;

    [HideInInspector] public bool canMove = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount, Transform attackerTransform)
    {
        if (isInvulnerable || isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnTakeDamage?.Invoke();

        if (currentHealth <= 0)
        {
            HandleDeath();
            return;
        }

        StartCoroutine(DamageSequence(attackerTransform));
    }

    private IEnumerator DamageSequence(Transform attacker)
    {
        isInvulnerable = true;
        canMove = false;

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;

        int direction = (transform.position.x < attacker.position.x) ? -1 : 1;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(direction * knockbackForce.x, knockbackForce.y), ForceMode2D.Impulse);

        StartCoroutine(CameraShake());

        StartCoroutine(FlashSpriteEffect());

        yield return new WaitForSeconds(knockbackControlLockTime);
        canMove = true;

        yield return new WaitForSeconds(invulnerabilityDuration - knockbackControlLockTime);
        isInvulnerable = false;
    }

    private IEnumerator FlashSpriteEffect()
    {
        float flashDelay = 0.1f;
        while (isInvulnerable)
        {
            spriteRenderer.color = new Color(1, 1, 1, 0.5f);
            yield return new WaitForSeconds(flashDelay);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(flashDelay);
        }
        spriteRenderer.color = Color.white;
    }

    private IEnumerator CameraShake()
    {
        if (mainCam == null) yield break;

        originalCamPos = mainCam.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            mainCam.transform.localPosition = new Vector3(originalCamPos.x + x, originalCamPos.y + y, originalCamPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCam.transform.localPosition = originalCamPos;
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHeal?.Invoke();
    }

    private void HandleDeath()
    {
        isDead = true;
        canMove = false;
        rb.linearVelocity = Vector2.zero;
        OnDeath?.Invoke();
        Debug.Log("Player Died!");
    }
}