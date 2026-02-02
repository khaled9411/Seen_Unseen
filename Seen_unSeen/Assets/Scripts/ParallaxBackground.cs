using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    private Transform cam;

    [Tooltip("0 = no movement, 1 = moves exactly with camera")]
    public Vector2 parallaxEffectMultiplier;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;

        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        float distX = (cam.position.x * parallaxEffectMultiplier.x);
        float distY = (cam.position.y * parallaxEffectMultiplier.y);

        transform.position = new Vector3(startPosition.x + distX, startPosition.y + distY, transform.position.z);
    }
}