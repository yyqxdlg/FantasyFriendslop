using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PersonalStatusbar : MonoBehaviour
{
    public Healthbar healthbar;

    public TMP_Text coinText;

    public Image[] characterImages;

    public Image keyImage;

    public void Awake()
    {
        foreach(Image image in characterImages)
        {
            image.enabled = false;   
        }

        keyImage.enabled = false;
    }

    public void UpdateHealth(float health, float healthMax)
    {
        healthbar.UpdateHealthBar(health, healthMax);
    }

    public void UpdateCoin(int coin)
    {
        coinText.text = coin.ToString();
    }

    public void SetType(int type)
    {
        foreach (Image image in characterImages)
        {
            image.enabled = false;
        }

        characterImages[type].enabled = true;
    }

    public void SetHasKey(bool hasKey)
    {
        keyImage.enabled = hasKey;
    }
}
