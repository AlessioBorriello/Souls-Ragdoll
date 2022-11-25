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
        [SerializeField] private Image lockOnImage;

        private PlayerManager playerManager;
        private PlayerNetworkManager networkManager;

        private Transform cameraTransform;
        private Quaternion startRotation;

        private bool handleHealthBar = false;
        private bool lockedOn = false;

        private void Awake()
        {
            playerManager = GetComponentInParent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();

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

            if(handleHealthBar) HandleHealthBar();

            //Stop handling if dead
            if (playerManager.isDead) handleHealthBar = false;
        }

        private void HandleHealthBar()
        {
            if(lockedOn) healthBarSlider.value = Mathf.Lerp(healthBarSlider.value, (float)networkManager.netCurrentHealth.Value / networkManager.netMaxHealth.Value, 4 * Time.deltaTime);
            else healthBarSlider.value = (float)networkManager.netCurrentHealth.Value / networkManager.netMaxHealth.Value;
        }

        public void ToggleTargetUI(bool enable)
        {
            healthBarFill.enabled = enable;
            lockOnImage.enabled = enable;
            lockedOn = enable;

            //Enable handling only after first lock on (avoids NaN bug in the slider)
            if (!handleHealthBar && enable)
            {
                healthBarSlider.value = (float)networkManager.netCurrentHealth.Value / networkManager.netMaxHealth.Value;
                handleHealthBar = true;
            }
        }
    }
}