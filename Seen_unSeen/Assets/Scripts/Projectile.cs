using DG.Tweening;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private AudioClip hitSound;

    [HideInInspector] public Transform attaker;

    private void Start()
    {
        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if hit player
        if (collision.CompareTag("Player"))
        {
            // Deal damage to player
            PlayerHealthController playerHealth = collision.GetComponent<PlayerHealthController>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, attaker);
            }

            // Spawn hit effect
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            // Play hit sound
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            }

            // Destroy projectile
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
        // Destroy on wall/ground collision
        else if (collision.CompareTag("Ground") || collision.CompareTag("Wall"))
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}