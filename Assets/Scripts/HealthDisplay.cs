using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    public Sprite emptyHeart;
    public Sprite fullHeart;
    public Image[] heart;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerController.Instance == null) return;

        int health = PlayerController.Instance.health;
        int maxHealth = PlayerController.Instance.maxHealth;

        for (int i = 0; i < heart.Length; i++)
        {
            if(i < health)
            {
                heart[i].sprite = fullHeart; // shows full heart image
            }
            else
            {
                heart[i].sprite = emptyHeart; // shows empty heart image

            }

            if (i < maxHealth)
            {
                heart[i].enabled = true; // Turn the heart ON if we have this much max health
            }
            else
            {
                heart[i].enabled = false; // Turn the heart OFF if this slot exceeds our max health
            }

        }
    }
}
