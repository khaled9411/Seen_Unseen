using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "NextScene";
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Color fadeColor = Color.white;
    [SerializeField] private int explosionCircles = 15;
    [SerializeField] private bool withKey = true;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private GameObject pressEText;

    private bool playerInTrigger = false;
    private bool isTransitioning = false;
    private GameObject fadeCanvas;

    private void Start()
    {
        if (pressEText != null)
            pressEText.SetActive(false);
    }

    private void Update()
    {
        if (withKey)
        {
            if (playerInTrigger && !isTransitioning && Input.GetKeyDown(interactionKey))
            {
                StartTransition();
            }
        }
        else
        {
            if (playerInTrigger && !isTransitioning)
            {
                StartTransition();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;

            if (pressEText != null)
                pressEText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;

            if (pressEText != null)
                pressEText.SetActive(false);
        }
    }

    public void StartTransition()
    {
        isTransitioning = true;
        if (pressEText != null)
            pressEText.SetActive(false);
        CreateExplosionFade();
    }

    private void CreateExplosionFade()
    {
        fadeCanvas = new GameObject("FadeCanvas");
        Canvas canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasGroup canvasGroup = fadeCanvas.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = true;

        for (int i = 0; i < explosionCircles; i++)
        {
            CreateExplosionCircle(fadeCanvas.transform, i);
        }

        canvasGroup.DOFade(1f, fadeDuration * 0.3f);

        DOVirtual.DelayedCall(fadeDuration, () =>
        {
            SceneManager.LoadScene(nextSceneName);
        });
    }

    private void CreateExplosionCircle(Transform parent, int index)
    {
        GameObject circle = new GameObject($"ExplosionCircle_{index}");
        circle.transform.SetParent(parent);

        RectTransform rectTransform = circle.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        float startSize = 0f;
        float endSize = Screen.width * 2f;
        rectTransform.sizeDelta = new Vector2(startSize, startSize);

        UnityEngine.UI.Image image = circle.AddComponent<UnityEngine.UI.Image>();
        image.sprite = CreateCircleSprite();
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0.8f / explosionCircles);

        float delay = (fadeDuration / explosionCircles) * index * 0.8f;
        float duration = fadeDuration * 0.7f;

        rectTransform.DOSizeDelta(new Vector2(endSize, endSize), duration)
            .SetDelay(delay)
            .SetEase(Ease.OutQuad);

        image.DOFade(0f, duration * 0.5f)
            .SetDelay(delay + duration * 0.5f);
    }

    private Sprite CreateCircleSprite()
    {

        int resolution = 256;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];

        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                float alpha = 1f - Mathf.Clamp01((distance - radius + 10f) / 10f);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}