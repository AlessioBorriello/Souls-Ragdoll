using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Unity.Netcode;

namespace AlessioBorriello
{
    public class LockOnUIManager : MonoBehaviour
    {
        [SerializeField] private Slider healthBarSlider;
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Slider animationHealthBarSlider;
        [SerializeField] private Image animationHealthBarFill;

        [SerializeField] private Image lockOnImage;

        private PlayerManager playerManager;
        private PlayerStatsManager statsManager;

        private Transform cameraTransform;
        private Quaternion startRotation;

        private bool lockedOn = false;

        private void Awake()
        {
            playerManager = GetComponentInParent<PlayerManager>();
            statsManager = playerManager.GetStatsManager();

            cameraTransform = Camera.main.transform;
        }

        private void Start()
        {
            //Cache start rotation to face camera
            startRotation = transform.rotation;

            //Disable UI
            ToggleTargetUI(false);
        }

        private void Update()
        {
            //Keep UI facing the camera
            transform.rotation = cameraTransform.rotation * startRotation;

            HandleHealthBar();
        }

        private void HandleHealthBar()
        {
            healthBarSlider.value = (float)statsManager.CurrentHealth / statsManager.MaxHealth;

            if (lockedOn) animationHealthBarSlider.value = Mathf.Lerp(animationHealthBarSlider.value, (float)statsManager.CurrentHealth / statsManager.MaxHealth, 4 * Time.deltaTime);
            else animationHealthBarSlider.value = (float)statsManager.CurrentHealth / statsManager.MaxHealth;
        }

        public void ToggleTargetUI(bool enable)
        {
            healthBarFill.enabled = enable;
            animationHealthBarFill.enabled = enable;

            lockOnImage.enabled = enable;
            lockedOn = enable;
        }
    }
}