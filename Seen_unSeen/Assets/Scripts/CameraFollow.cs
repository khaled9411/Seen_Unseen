using UnityEngine;
using DG.Tweening;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform cameraTransform;

    [Header("Follow Settings")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private Vector3 offset = new Vector3(0, 1, 0);
    [SerializeField] private Vector2 lookAheadAmount = new Vector2(2f, 0f);

    private Vector3 currentVelocity;
    private float currentLookAheadX;

    private void Start()
    {
        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player").transform;

        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>().transform;

        transform.position = playerTarget.position + offset;
    }

    private void LateUpdate()
    {
        if (playerTarget == null) return;

        float directionSign = Mathf.Sign(playerTarget.localScale.x);

        currentLookAheadX = Mathf.Lerp(currentLookAheadX, directionSign * lookAheadAmount.x, Time.deltaTime * 3f);

        Vector3 targetPosition = playerTarget.position + offset;
        targetPosition.x += currentLookAheadX;

        targetPosition.z = transform.position.z;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }

    public void TriggerShake(float duration, float strength)
    {
        cameraTransform.DOComplete();
        cameraTransform.DOShakePosition(duration, strength, 10, 90, false, true);
    }
}