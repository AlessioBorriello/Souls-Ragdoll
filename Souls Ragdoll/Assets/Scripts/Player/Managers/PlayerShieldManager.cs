using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerShieldManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private InputManager inputManager;
        private AnimationManager animationManager;
        private PlayerInventoryManager inventoryManager;
        private PlayerCombatManager combatManager;

        private bool blockingWithLeft = false;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            inputManager = playerManager.GetInputManager();
            animationManager = playerManager.GetAnimationManager();
            inventoryManager = playerManager.GetInventoryManager();
            combatManager = playerManager.GetCombatManager();
        }

        public void HandleBlocks()
        {
            if (playerManager.playerIsStuckInAnimation)
            {
                StopBlock();
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
                TryBlock(isLeft);
            }
            else
            {
                StopBlock();
            }
        }

        private void TryBlock(bool isLeft)
        {

            //Get right or left item
            HandEquippableItem item = (isLeft) ? inventoryManager.GetCurrentItem(true) : inventoryManager.GetCurrentItem(false);

            blockingWithLeft = isLeft;
            animationManager.UpdateBlockingWithLeftValue(blockingWithLeft);
            Block();
        }

        private void Block()
        {
            if(!playerManager.isBlocking)
            {
                animationManager.PlayTargetAnimation("OneHandShieldBlockLoop", .1f, false);
                playerManager.isBlocking = true;
            }
        }

        private void StopBlock()
        {
            if (playerManager.isBlocking)
            {
                //Stop blocking without changing the playerIsStuckInAnimation bool
                animationManager.PlayTargetAnimation("Upper Body Empty", .1f, playerManager.playerIsStuckInAnimation);
                playerManager.isBlocking = false;
            }
        }

        public bool IsBlockingWithLeft()
        {
            return blockingWithLeft;
        }

    }
}