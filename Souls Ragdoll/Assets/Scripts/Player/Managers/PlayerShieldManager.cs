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

        private bool blockingWithLeft = false;
        private bool parryingWithLeft = false;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            inputManager = playerManager.GetInputManager();
            ragdollManager = playerManager.GetRagdollManager();
            statsManager = playerManager.GetStatsManager();
            animationManager = playerManager.GetAnimationManager();
            inventoryManager = playerManager.GetInventoryManager();
            networkManager = playerManager.GetNetworkManager();
        }

        public void HandleBlocks()
        {
            if (playerManager.playerIsStuckInAnimation)
            {
                if (!playerManager.isBlocking) return; //Already not blocking

                StopBlocking();
                networkManager.StopBlockingServerRpc();
                return;
            }

            //Check if pressed
            bool rb = inputManager.rbInput;
            bool lb = inputManager.lbInput;

            bool isLeft = (lb);

            if (rb || lb)
            {
                //If it's not a shield
                if (inventoryManager.GetCurrentItemType(isLeft) != PlayerInventoryManager.ItemType.shield) return;

                if (playerManager.isBlocking) return; //Blocking already

                //Start blocking
                Block(isLeft);
                networkManager.BlockServerRpc(isLeft);
            }
            else
            {
                if (!playerManager.isBlocking) return; //Already not blocking

                //Stop blocking
                StopBlocking();
                networkManager.StopBlockingServerRpc();
            }
        }

        public void Block(bool blockingWithLeft)
        {
            this.blockingWithLeft = blockingWithLeft;
            animationManager.UpdateBlockingWithLeftValue(blockingWithLeft);

            animationManager.PlayTargetAnimation("OneHandShieldBlockLoop", .1f, false);
            playerManager.isBlocking = true;
        }

        public void HandleParries()
        {
            if (playerManager.playerIsStuckInAnimation) return;

            //Check if pressed
            bool rt = inputManager.rtInput;
            bool lt = inputManager.ltInput;

            bool isLeft = (lt);

            if (rt || lt)
            {
                //If it's not a shield
                if (inventoryManager.GetCurrentItemType(isLeft) != PlayerInventoryManager.ItemType.shield) return;

                //If no stamina
                if (statsManager.CurrentStamina < 1) return;

                //Parry
                Parry(isLeft);
                networkManager.ParryServerRpc(isLeft);

                //Consume stamina
                float staminaCost = ((ShieldItem)inventoryManager.GetCurrentItem(isLeft)).parryStaminaCost;
                statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);
            }
        }

        public void Parry(bool parryingWithLeft)
        {
            this.parryingWithLeft = parryingWithLeft;
            animationManager.UpdateParryingWithLeftValue(parryingWithLeft);

            animationManager.PlayTargetAnimation("Parry", .2f, true);
        }

        public void StopBlocking()
        {
            //Stop blocking without changing the playerIsStuckInAnimation bool
            animationManager.PlayTargetAnimation("Upper Body Empty", .1f, playerManager.playerIsStuckInAnimation);
            playerManager.isBlocking = false;
        }

        public bool IsBlockingWithLeft()
        {
            return blockingWithLeft;
        }

    }
}