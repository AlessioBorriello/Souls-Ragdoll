using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerShieldManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private InputManager inputManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerStatsManager statsManager;
        private AnimationManager animationManager;
        private PlayerInventoryManager inventoryManager;
        private PlayerNetworkManager networkManager;

        private Rigidbody physicalHips;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            inputManager = playerManager.GetInputManager();
            ragdollManager = playerManager.GetRagdollManager();
            statsManager = playerManager.GetStatsManager();
            animationManager = playerManager.GetAnimationManager();
            inventoryManager = playerManager.GetInventoryManager();
            networkManager = playerManager.GetNetworkManager();

            physicalHips = playerManager.GetPhysicalHips();
        }

        public void HandleBlocks()
        {
            if (playerManager.isStuckInAnimation)
            {
                if (!playerManager.isBlocking) return; //Already not blocking

                StopBlocking();
                networkManager.StopBlockingServerRpc();
                return;
            }

            //Check if pressed
            bool lb = inputManager.lbInput;

            if (lb)
            {
                //If it's not a shield
                if (inventoryManager.GetCurrentItemType(true) != PlayerInventoryManager.ItemType.shield) return;

                if (playerManager.isBlocking) return; //Blocking already

                //Start blocking
                Block();
                networkManager.BlockServerRpc();
            }
            else
            {
                if (!playerManager.isBlocking) return; //Already not blocking

                //Stop blocking
                StopBlocking();
                networkManager.StopBlockingServerRpc();
            }
        }

        public void Block()
        {

            Action onBlockEnter = () => {
                playerManager.isBlocking = true;
            };

            animationManager.PlayOverrideAnimation("BlockLoopLeft", onBlockEnter, null, 3);
        }

        public void HandleParries()
        {
            if (playerManager.isStuckInAnimation) return;

            //Check if pressed
            bool lt = inputManager.ltInput;

            if (lt)
            {
                //If it's not a shield
                if (inventoryManager.GetCurrentItemType(true) != PlayerInventoryManager.ItemType.shield) return;

                //If the shield cannot parry
                if (!((ShieldItem)inventoryManager.GetCurrentItem(true)).canParry) return;

                //If no stamina
                if (statsManager.CurrentStamina < 1) return;

                //Parry
                Parry();
                networkManager.ParryServerRpc();

                //Consume stamina
                float staminaCost = ((ShieldItem)inventoryManager.GetCurrentItem(true)).parryStaminaCost;
                statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);
            }
        }

        public void Parry()
        {
            //Create enter and exit events
            Action onParryEnterAction = () =>
            {
                //Debug.Log("Parry enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.canRotate = false;
                playerManager.isParrying = true;
                playerManager.shouldSlide = false;
            };

            Action onParryExitAction = () =>
            {
                //Debug.Log("Parry exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                playerManager.isParrying = false;
                animationManager.FadeOutOverrideAnimation(.1f);

                inventoryManager.GetParryColliderControl().CloseParryCollider();
            };

            animationManager.PlayOverrideAnimation("Parry", onParryEnterAction, onParryExitAction);
        }

        public void StopBlocking()
        {
            animationManager.FadeOutOverrideAnimation(.1f, 3);
            playerManager.isBlocking = false;
        }

        public void ShieldBroken()
        {
            //Create enter and exit events
            Action onShieldBrokenEnterAction = () =>
            {
                //Debug.Log("Shield broken enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.shouldSlide = false;
                playerManager.canRotate = false;
                playerManager.canBeRiposted = true;

                //Disable attack collider
                inventoryManager.GetCurrentItemDamageColliderControl(false).ToggleCollider(false);

                //Set forward when shield broken
                playerManager.GetCombatManager().forwardWhenParried = physicalHips.transform.forward;
            };

            Action onShieldBrokenExitAction = () =>
            {
                //Debug.Log("Shield broken exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                playerManager.canBeRiposted = false;
                animationManager.FadeOutOverrideAnimation(.15f);
            };

            animationManager.PlayOverrideAnimation("ShieldBrokenLeft", onShieldBrokenEnterAction, onShieldBrokenExitAction);
        }

    }
}