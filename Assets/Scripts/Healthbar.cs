using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image fillImage;

    void Start()
    {
        fillImage.fillAmount = 1;
    }

    public void UpdateHealthBar(float fillAmt)
    {
        fillImage.fillAmount = Mathf.Clamp(fillAmt, 0, 1);
    }
}