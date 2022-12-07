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
            animationManager.PlayUpperBodyArmsOverrideAnimation("BlockLoop", true);
            playerManager.isBlocking = true;
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
            };

            Action onParryExitAction = () =>
            {
                //Debug.Log("Parry exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                animationManager.FadeOutOverrideAnimation(.1f);
            };

            animationManager.PlayOverrideAnimation("Parry", onParryEnterAction, onParryExitAction);
        }

        public void StopBlocking()
        {
            //Stop blocking without changing the playerIsStuckInAnimation bool
            animationManager.FadeOutUpperBodyArmsOverrideAnimation(.1f, true);
            playerManager.isBlocking = false;
        }

        public void ShieldBroken()
        {
            animationManager.PlayTargetAnimation("ShieldBroken", .15f, true);

            //Allow enemy to riposte
            playerManager.canBeRiposted = true;

            //Set forward when broken
            playerManager.GetCombatManager().forwardWhenParried = physicalHips.transform.forward;
        }

    }
}