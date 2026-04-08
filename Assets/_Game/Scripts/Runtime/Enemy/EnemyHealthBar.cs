using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{

    public Slider mainSlider;


    public void UpdateHealthBar(float health, float maxHealth)
    {
        mainSlider.value = health / maxHealth;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
