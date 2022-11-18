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

        [Header("Quickslots")]
        [SerializeField] private Image rightItemIcon;
        [SerializeField] private Image leftItemIcon;

        private void Update()
        {
            if (statsManager == null) return;

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

        public void UpdateQuickSlotsUI(PlayerInventoryManager inventoryManager)
        {
            //Right slot
            HandEquippableItem rightItem = inventoryManager.GetCurrentItem(false);
            Sprite rightSprite = (rightItem != null)? rightItem.iconSprite : null;
            SetIcon(rightSprite, ref rightItemIcon);

            //Left slot
            HandEquippableItem leftItem = inventoryManager.GetCurrentItem(true);
            Sprite leftSprite = (leftItem != null) ? leftItem.iconSprite : null;
            SetIcon(leftSprite, ref leftItemIcon);
        }

        private void SetIcon(Sprite sprite, ref Image slotImage)
        {
            slotImage.enabled = (sprite != null) ? true : false;
            slotImage.sprite = sprite;
            slotImage.preserveAspect = true;
        }

        public void SetPlayerStatsManager(PlayerStatsManager statsManager)
        {
            this.statsManager = statsManager;
        }
    }

}