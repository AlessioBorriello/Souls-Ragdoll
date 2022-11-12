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

        private bool blockingWithLeft = false;

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();
            inputManager = playerManager.GetInputManager();
            inventoryManager = playerManager.GetInventoryManager();
            animationManager = playerManager.GetAnimationManager();
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
            if (item is not ShieldItem) return;

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