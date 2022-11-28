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
        private AnimationManager animationManager;
        private PlayerInventoryManager inventoryManager;
        private PlayerNetworkManager networkManager;

        private bool blockingWithLeft = false;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            inputManager = playerManager.GetInputManager();
            animationManager = playerManager.GetAnimationManager();
            inventoryManager = playerManager.GetInventoryManager();
            networkManager = playerManager.GetNetworkManager();
        }

        public void HandleBlocks()
        {
            if (playerManager.playerIsStuckInAnimation)
            {
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

                Block(isLeft);
                networkManager.BlockServerRpc(isLeft);
            }
            else
            {
                StopBlocking();
                networkManager.StopBlockingServerRpc();
            }
        }

        public void Block(bool blockingWithLeft)
        {
            //Start blocking
            if(!playerManager.isBlocking)
            {
                this.blockingWithLeft = blockingWithLeft;
                animationManager.UpdateBlockingWithLeftValue(blockingWithLeft);

                animationManager.PlayTargetAnimation("OneHandShieldBlockLoop", .1f, false);
                playerManager.isBlocking = true;
            }
        }

        public void StopBlocking()
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