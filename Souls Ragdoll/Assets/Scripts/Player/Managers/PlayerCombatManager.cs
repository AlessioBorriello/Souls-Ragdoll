using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlessioBorriello
{
    public class PlayerCombatManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private InputManager inputManager;
        private AnimationManager animationManager;
        private PlayerInventoryManager inventoryManager;
        private PlayerWeaponManager weaponManager;
        private PlayerShieldManager shieldManager;
        private PlayerNetworkManager networkManager;

        public bool diedFromCriticalDamage = false;
        public Vector3 forwardWhenParried; //Forward direction when just parried

        public bool twoHanding = false; //If the player is two handing
        public bool twoHandingLeft = false; //If he is two handing, then if it's two handing right or left

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            inputManager = playerManager.GetInputManager();
            animationManager = playerManager.GetAnimationManager();
            inventoryManager = playerManager.GetInventoryManager();
            weaponManager = playerManager.GetWeaponManager();
            shieldManager = playerManager.GetShieldManager();
            networkManager = playerManager.GetNetworkManager();

            forwardWhenParried = playerManager.GetPhysicalHips().transform.forward;
        }

        public void HandleCombat()
        {
            if (playerManager.disableActions) return;

            //Two handing
            HandleTwoHanding();

            //Attacks
            weaponManager.HandleAttacks();

            //Parries
            shieldManager.HandleParries();

            //Blocks
            shieldManager.HandleBlocks();
        }

        public void HandleTwoHanding()
        {
            bool twoHandRightInput = inputManager.northInput && inputManager.rbInputPressed;
            bool twoHandLeftInput = inputManager.northInput && inputManager.lbInputPressed;

            bool isLeft = twoHandLeftInput;

            if(twoHandRightInput || twoHandLeftInput)
            {
                //Stop attack input
                inputManager.rbInputPressed = false;
                inputManager.lbInputPressed = false;

                if (twoHanding)
                {
                    StopTwoHanding();
                    networkManager.StopTwoHandingServerRpc();
                }
                else
                {
                    TwoHand(isLeft);
                    networkManager.TwoHandServerRpc(isLeft);
                }
            }
        }

        public void StopTwoHanding()
        {
            //Load other item model
            inventoryManager.LoadItemInHand(!twoHandingLeft);

            //Unload 2 hands idle animation
            animationManager.FadeOutOverrideAnimation(.1f, OverrideLayers.bothArmsLayer);

            //Load 1 hand idle animations
            inventoryManager.LoadIdleAnimation(false, false); //Right hand
            inventoryManager.LoadIdleAnimation(true, false); //Left hand

            twoHanding = false;
            twoHandingLeft = false;
        }

        public void TwoHand(bool isLeft)
        {
            twoHanding = true;
            twoHandingLeft = isLeft;

            //Unload other item model
            inventoryManager.UnloadItemInHand(!isLeft);

            //Unload 1 hand idle animations
            animationManager.FadeOutOverrideAnimation(.1f, OverrideLayers.leftArmLayer);
            animationManager.FadeOutOverrideAnimation(.1f, OverrideLayers.rightArmLayer);

            //Load 2 hand idle animation
            inventoryManager.LoadIdleAnimation(isLeft, true);
        }

    }
}
