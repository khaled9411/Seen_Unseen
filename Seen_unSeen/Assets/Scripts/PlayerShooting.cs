using UnityEngine;
using DG.Tweening;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private bool isAutomatic = false;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float shootDelayInAnimation = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    private AudioSource audioSource;

    [Header("Visual Effects")]
    [SerializeField] private bool useRecoilEffect = true;
    [SerializeField] private float recoilStrength = 0.1f;
    [SerializeField] private float recoilDuration = 0.1f;

    private Camera mainCamera;
    private bool canShoot = true;
    private bool isAttacking = false;
    private Vector2 aimDirection;
    private int AttackHash;

    private void Start()
    {
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        AttackHash = Animator.StringToHash("Attack");
    }

    private void Update()
    {
        HandleAiming();
        HandleShooting();
    }

    private void HandleAiming()
    {
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        aimDirection = (mousePosition - transform.position).normalized;

        if (firePoint != null)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

            Vector3 scale = transform.localScale;
            if (scale.x < 0)
            {
                angle += 180f;
            }

            firePoint.rotation = Quaternion.Euler(0, 0, angle);
        }
    }


    private void HandleShooting()
    {
        if (isAutomatic)
        {
            if (Input.GetMouseButton(0) && canShoot && !isAttacking)
            {
                StartAttack();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0) && canShoot && !isAttacking)
            {
                StartAttack();
            }
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        canShoot = false;

        if (animator != null)
        {
            animator.Play(AttackHash);
        }

        if (useRecoilEffect)
        {
            ApplyRecoilEffect();
        }

        DOVirtual.DelayedCall(shootDelayInAnimation, () =>
        {
            FireProjectile();
        });

        DOVirtual.DelayedCall(shootCooldown, () =>
        {
            canShoot = true;
            isAttacking = false;
        });
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.attaker = this.transform;
        }

        Vector2 direction = aimDirection;

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0, 0, angle);

        PlaySound(shootSound);
    }

    private void ApplyRecoilEffect()
    {
        Vector3 recoilDirection = -aimDirection * recoilStrength;
        Vector3 originalPosition = transform.position;

        transform.DOMove(transform.position + (Vector3)recoilDirection, recoilDuration / 2)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOMove(originalPosition, recoilDuration / 2).SetEase(Ease.InQuad);
            });
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnDrawGizmos()
    {
        if (firePoint != null && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePoint.position, firePoint.position + (Vector3)aimDirection * 2f);
            Gizmos.DrawWireSphere(firePoint.position, 0.1f);
        }
    }
}