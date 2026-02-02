using UnityEngine;

public class HeartUI1 : MonoBehaviour
{
    public PlayerHealthController player;
    public GameObject[] hearts;

    public void UpdateHearts()
    {
        int health = player.GetHealth();
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].SetActive(i < health);
        }
    }
}
