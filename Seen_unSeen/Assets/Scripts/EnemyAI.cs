using UnityEngine;
using DG.Tweening;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Attack,
        Stun
    }

    [Header("State Debug")]
    [SerializeField] private EnemyState currentState = EnemyState.Patrol;

    [Header("Patrol Settings")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float waitTimeAtPoint = 0.5f;
    [SerializeField] private Ease patrolEase = Ease.InOutSine;

    private Transform currentTarget;
    private bool isMovingToB = true;

    [Header("Attack Settings")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float telegraphDuration = 0.5f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 8f;

    private float lastAttackTime = -999f;
    private bool isTelegraphing = false;

    [Header("Stun Settings")]
    [SerializeField] private float stunDuration = 1f;
    [SerializeField] private float stunRecoveryDistance = 7f;
    [SerializeField] private int stunFlashCount = 3;

    private float stunStartTime;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color telegraphColor = Color.red;
    [SerializeField] private Color stunColor = Color.gray;
    [SerializeField] private float squashAmount = 0.9f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Animator animator;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip stunSound;
    [SerializeField] private AudioSource audioSource;

    private Tween currentMovementTween;
    private Vector3 originalScale;

    private readonly int idleHash = Animator.StringToHash("Idle");
    private readonly int runHash = Animator.StringToHash("Run");
    private readonly int attackHash = Animator.StringToHash("Attack");

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrolState();
                break;
            case EnemyState.Attack:
                UpdateAttackState();
                break;
            case EnemyState.Stun:
                UpdateStunState();
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stunRecoveryDistance);

        if (patrolPointA != null && patrolPointB != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(patrolPointA.position, patrolPointB.position);
            Gizmos.DrawWireSphere(patrolPointA.position, 0.3f);
            Gizmos.DrawWireSphere(patrolPointB.position, 0.3f);
        }
    }

    private void Initialize()
    {
        originalScale = transform.localScale;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (animator == null)
            animator = GetComponent<Animator>();

        currentTarget = isMovingToB ? patrolPointB : patrolPointA;

        ChangeState(EnemyState.Patrol);
    }

    private void ChangeState(EnemyState newState)
    {
        ExitState(currentState);
        currentState = newState;
        EnterState(newState);
    }

    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Patrol:
                spriteRenderer.color = normalColor;
                StartPatrol();
                PlayAnimation(runHash);
                break;

            case EnemyState.Attack:
                StopMovement();
                PlayAnimation(idleHash);
                break;

            case EnemyState.Stun:
                StopMovement();
                stunStartTime = Time.time;
                PlayStunEffect();
                PlaySound(stunSound);
                PlayAnimation(idleHash);
                break;
        }
    }

    private void ExitState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Patrol:
                StopMovement();
                break;

            case EnemyState.Attack:
                StopTelegraph();
                break;

            case EnemyState.Stun:
                spriteRenderer.color = normalColor;
                DOTween.Kill(spriteRenderer);
                break;
        }
    }

    private void UpdatePatrolState()
    {
        if (IsPlayerInRange(detectionRange))
        {
            ChangeState(EnemyState.Attack);
            return;
        }
    }

    private void StartPatrol()
    {
        if (patrolPointA == null || patrolPointB == null) return;
        MoveToCurrentTarget();
    }

    private void MoveToCurrentTarget()
    {
        if (currentTarget == null) return;

        float distance = Vector2.Distance(transform.position, currentTarget.position);
        float duration = distance / patrolSpeed;

        FaceDirection(currentTarget.position);

        currentMovementTween = transform.DOMove(currentTarget.position, duration)
            .SetEase(patrolEase)
            .OnComplete(() => OnReachPatrolPoint());
    }

    private void OnReachPatrolPoint()
    {
        if (currentState != EnemyState.Patrol) return;

        isMovingToB = !isMovingToB;
        currentTarget = isMovingToB ? patrolPointB : patrolPointA;

        if (waitTimeAtPoint > 0)
        {
            PlayAnimation(idleHash);
            DOVirtual.DelayedCall(waitTimeAtPoint, () =>
            {
                if (currentState == EnemyState.Patrol)
                {
                    PlayAnimation(runHash);
                    MoveToCurrentTarget();
                }
            });
        }
        else
        {
            MoveToCurrentTarget();
        }
    }

    private void UpdateAttackState()
    {
        if (player == null) return;

        if (!IsPlayerInRange(detectionRange))
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        FaceDirection(player.position);

        if (Time.time >= lastAttackTime + attackCooldown && !isTelegraphing)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        isTelegraphing = true;
        PlayAnimation(attackHash);

        yield return StartCoroutine(PlayTelegraphEffect());

        FireProjectile();
        PlayShootEffect();

        lastAttackTime = Time.time;
        isTelegraphing = false;

        if (currentState == EnemyState.Attack)
            PlayAnimation(idleHash);
    }

    private IEnumerator PlayTelegraphEffect()
    {
        float elapsed = 0f;
        int flashCount = Mathf.CeilToInt(telegraphDuration / 0.15f);

        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = telegraphColor;
            yield return new WaitForSeconds(0.075f);
            spriteRenderer.color = normalColor;
            yield return new WaitForSeconds(0.075f);
            elapsed += 0.15f;

            if (elapsed >= telegraphDuration)
                break;
        }
        spriteRenderer.color = normalColor;
    }

    private void StopTelegraph()
    {
        StopAllCoroutines();
        isTelegraphing = false;
        spriteRenderer.color = normalColor;
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null || player == null) return;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        projectile.GetComponent<Projectile>().attaker = this.transform;

        Vector2 direction = (player.position - firePoint.position).normalized;

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0, 0, angle);

        PlaySound(shootSound);
    }

    private void PlayShootEffect()
    {
        transform.DOScale(new Vector3(originalScale.x, originalScale.y * squashAmount, originalScale.z), 0.1f)
            .OnComplete(() =>
            {
                transform.DOScale(originalScale, 0.1f);
            });
    }

    private void UpdateStunState()
    {
        bool stunTimeOver = Time.time >= stunStartTime + stunDuration;
        bool playerFarEnough = !IsPlayerInRange(stunRecoveryDistance);

        if (stunTimeOver && playerFarEnough)
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    public void TakeDamage(int damage)
    {
        ChangeState(EnemyState.Stun);
    }

    private void PlayStunEffect()
    {
        transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 1);

        Sequence flashSequence = DOTween.Sequence();
        for (int i = 0; i < stunFlashCount; i++)
        {
            flashSequence.Append(spriteRenderer.DOColor(stunColor, 0.1f));
            flashSequence.Append(spriteRenderer.DOColor(normalColor, 0.1f));
        }
        flashSequence.Play();
    }

    private bool IsPlayerInRange(float range)
    {
        if (player == null) return false;
        return Vector2.Distance(transform.position, player.position) <= range;
    }

    private void FaceDirection(Vector3 targetPosition)
    {
        if (targetPosition.x > transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    private void StopMovement()
    {
        if (currentMovementTween != null && currentMovementTween.IsActive())
        {
            currentMovementTween.Kill();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayAnimation(int animHash)
    {
        if (animator != null)
        {
            animator.Play(animHash);
        }
    }

    public void OnHit()
    {
        TakeDamage(1);
    }

    public void ForceState(EnemyState state)
    {
        ChangeState(state);
    }
}