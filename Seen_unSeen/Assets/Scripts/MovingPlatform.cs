using UnityEngine;
using System.Collections;

public class MovingPlatform : MonoBehaviour
{
    [Header("moving settings")]
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;

    [Header("waiting time")]
    public float stopTime = 2f;

    private Vector3 targetPosition;
    private bool isWaiting = false;

    void Start()
    {
        targetPosition = pointB.position;
    }

    void FixedUpdate()
    {
        if (isWaiting) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.fixedDeltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            StartCoroutine(WaitAtPoint());
        }
    }

    IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        yield return new WaitForSeconds(stopTime);
        targetPosition = targetPosition == pointA.position ? pointB.position : pointA.position;
        isWaiting = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}
