using UnityEngine;

public class HeartUI : MonoBehaviour
{
    public EnemyHealth player;
    public GameObject[] hearts;

    public void UpdateHearts()
    {
        int health = player.GetCurrentHealth();
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].SetActive(i < health);
        }
    }
}
