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
        private PlayerAnimationsDatabase animationDatabase;
        private PlayerCombatManager combatManager;

        [SerializeField] private AnimationData shieldBrokenAnimationData;
        [SerializeField] private AnimationData oneHandedBlockAnimationData;
        [SerializeField] private AnimationData twoHandedBlockAnimationData;

        private Rigidbody physicalHips;

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
            animationDatabase = playerManager.GetAnimationDatabase();
            combatManager = playerManager.GetCombatManager();

            physicalHips = playerManager.GetPhysicalHips();
        }

        public void HandleBlocks()
        {
            if (playerManager.isStuckInAnimation || playerManager.disableActions)
            {
                if (!playerManager.isBlocking) return; //Already not blocking

                StopBlocking();
                networkManager.StopBlockingServerRpc();
                return;
            }

            //Check if pressed
            bool lb = inputManager.lbInput;
            bool rb = inputManager.rbInput;
            bool lt = inputManager.ltInput;
            bool rt = inputManager.rtInput;

            bool isLeft = lb || lt;

            if (lb || rb || lt || rt)
            {
                //If not 2 handing, allow block only with lb and rb
                if (!combatManager.twoHanding && (!lb && !rb)) return;

                //If not 2 handing, allow blocking only with shields
                if (!combatManager.twoHanding && inventoryManager.GetCurrentItem(isLeft) is not ShieldItem) return;

                //If 2 handing block only with the left buttons
                if (combatManager.twoHanding && isLeft != true) return;

                //Blocking already or pressing north button
                if (playerManager.isBlocking || inputManager.northInput) return;

                //Start blocking
                Block((!combatManager.twoHanding) ? isLeft : combatManager.twoHandingLeft);
                networkManager.BlockServerRpc((!combatManager.twoHanding) ? isLeft : combatManager.twoHandingLeft);
            }
            else
            {
                if (!playerManager.isBlocking) return; //Already not blocking

                //Stop blocking
                StopBlocking();
                networkManager.StopBlockingServerRpc();
            }
        }

        public void Block(bool isLeft)
        {
            //Get right or left item (if 2 handing get the 2 handed item)
            HandEquippableItem blockingItem = inventoryManager.GetCurrentItem(isLeft);
            if (blockingItem == null) return;

            Debug.Log("Blocking with " + blockingItem.name + " from " + ((isLeft) ? "left" : "right") + " side");

            //Update proprieties
            blockingWithLeft = isLeft;

            Action onBlockEnter = () => {
                playerManager.isBlocking = true;
            };

            AnimationData blockingAnimationData = (combatManager.twoHanding)? blockingItem.twoHandedBlockAnimationData : blockingItem.oneHandedBlockAnimationData;

            OverrideLayers layer = (combatManager.twoHanding)? OverrideLayers.upperBodyLayer : ((isLeft) ? OverrideLayers.upperBodyLeftArmLayer : OverrideLayers.upperBodyRightArmLayer);
            animationManager.PlayOverrideAnimation(blockingAnimationData, onBlockEnter, null, layer, isLeft);
        }

        public void HandleParries()
        {
            if (playerManager.isStuckInAnimation) return;

            //Check if pressed
            bool lt = inputManager.ltInput;
            bool rt = inputManager.rtInput;

            bool isLeft = lt;

            if (lt || rt)
            {
                //If 2 handing, allow parrying only with the left buttons
                if (combatManager.twoHanding && isLeft != true) return;

                //If no stamina
                if (statsManager.CurrentStamina < 1) return;

                //Parry
                Parry((!combatManager.twoHanding) ? isLeft : combatManager.twoHandingLeft);
                networkManager.ParryServerRpc((!combatManager.twoHanding) ? isLeft : combatManager.twoHandingLeft);
            }
        }

        public void Parry(bool isLeft)
        {
            //If it's not a shield
            if (inventoryManager.GetCurrentItem(isLeft) is not ShieldItem) return;

            //If the shield cannot parry
            if (!((ShieldItem)inventoryManager.GetCurrentItem(isLeft)).canParry) return;

            parryingWithLeft = isLeft;

            Debug.Log("Parrying with " + inventoryManager.GetCurrentItem(isLeft).name + " from " + ((isLeft) ? "left" : "right") + " side");

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

            AnimationData parryAnimationData = ((ShieldItem)inventoryManager.GetCurrentItem(isLeft)).parryAnimationData;
            animationManager.PlayOverrideAnimation(parryAnimationData, onParryEnterAction, onParryExitAction, mirrored: isLeft);

            //Consume stamina
            float staminaCost = ((ShieldItem)inventoryManager.GetCurrentItem(isLeft)).parryStaminaCost;
            statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);
        }

        public void StopBlocking()
        {
            OverrideLayers layer = (combatManager.twoHanding) ? OverrideLayers.upperBodyLayer : ((blockingWithLeft) ? OverrideLayers.upperBodyLeftArmLayer : OverrideLayers.upperBodyRightArmLayer);
            animationManager.FadeOutOverrideAnimation(.1f, layer);
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

            animationManager.PlayOverrideAnimation(shieldBrokenAnimationData, onShieldBrokenEnterAction, onShieldBrokenExitAction, mirrored: blockingWithLeft);
        }

        public bool IsBlockingWithLeft()
        {
            return blockingWithLeft;
        }

        public bool IsParryingWithLeft()
        {
            return parryingWithLeft;
        }

    }
}