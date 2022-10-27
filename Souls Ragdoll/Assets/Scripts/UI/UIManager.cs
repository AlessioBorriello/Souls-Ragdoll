using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AlessioBorriello
{
    public class UIManager : MonoBehaviour
    {

        [SerializeField] private PlayerStatsManager statsManager;

        [Header("Healthbar")]
        [SerializeField] private Slider healthBarSlider;
        [SerializeField] private float healthBarLerpSpeed = 2.0f;

        [Header("Staminabar")]
        [SerializeField] private Slider staminaBarSlider;
        [SerializeField] private float staminaBarLerpSpeed = 4.0f;

        private void Update()
        {
            HandleHealthBar();
            HandleStaminaBar();
        }

        private void HandleHealthBar()
        {
            healthBarSlider.value = Mathf.Lerp(healthBarSlider.value, (float)statsManager.currentHealth / statsManager.maxHealth, healthBarLerpSpeed * Time.deltaTime);
        }

        private void HandleStaminaBar()
        {
            staminaBarSlider.value = Mathf.Lerp(staminaBarSlider.value, (float)statsManager.currentStamina / statsManager.maxStamina, staminaBarLerpSpeed * Time.deltaTime);
        }

    }

}