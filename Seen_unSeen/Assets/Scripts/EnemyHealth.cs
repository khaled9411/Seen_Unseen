using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 0.5f;
    [SerializeField] private bool destroyOnDeath = true;

    [Header("Visual Feedback")]
    [SerializeField] private bool useDamageFlash = true;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private bool useShakeOnDamage = true;
    [SerializeField] private float shakeStrength = 0.2f;
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip deathSound;
    private AudioSource audioSource;

    [Header("Events")]
    public UnityEvent OnDamageTaken;
    public UnityEvent OnDeath;
    public UnityEvent<int> OnHealthChanged;

    // References
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        OnDamageTaken?.Invoke();
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth > 0)
        {
            PlayDamageEffects();
            PlaySound(damageSound);
        }
        else
        {
            Die();
        }

        Debug.Log($"{gameObject.name} Health: {currentHealth}/{maxHealth}");
    }

    private void PlayDamageEffects()
    {
        if (useDamageFlash && spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.DOColor(damageFlashColor, flashDuration / 2)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    spriteRenderer.DOColor(originalColor, flashDuration / 2)
                        .SetEase(Ease.InQuad);
                });
        }

        if (useShakeOnDamage)
        {
            Vector3 originalPosition = transform.position;
            transform.DOShakePosition(shakeDuration, shakeStrength, 20, 90, false, true)
                .SetEase(Ease.OutQuad);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"{gameObject.name} died!");

        OnDeath?.Invoke();

        PlaySound(deathSound);

        PlayDeathEffects();

        if (destroyOnDeath)
        {
            Destroy(gameObject, deathDelay);
        }
    }

    private void PlayDeathEffects()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.DOFade(0f, deathDelay)
                .SetEase(Ease.InQuad);
        }

        transform.DOScale(Vector3.zero, deathDelay)
            .SetEase(Ease.InBack);

        transform.DORotate(new Vector3(0, 0, 360), deathDelay, RotateMode.FastBeyond360)
            .SetEase(Ease.InQuad);
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"{gameObject.name} healed! Health: {currentHealth}/{maxHealth}");
    }

    public void InstantKill()
    {
        currentHealth = 0;
        Die();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;

    private void OnDrawGizmosSelected()
    {
        Vector3 healthBarPosition = transform.position + Vector3.up * 1.5f;
        float barWidth = 1f;
        float barHeight = 0.1f;

        Gizmos.color = Color.black;
        Gizmos.DrawCube(healthBarPosition, new Vector3(barWidth, barHeight, 0));

        if (Application.isPlaying)
        {
            float healthWidth = barWidth * GetHealthPercentage();
            Gizmos.color = Color.green;
            Vector3 healthPosition = healthBarPosition - new Vector3((barWidth - healthWidth) / 2, 0, 0);
            Gizmos.DrawCube(healthPosition, new Vector3(healthWidth, barHeight, 0));
        }
    }
}