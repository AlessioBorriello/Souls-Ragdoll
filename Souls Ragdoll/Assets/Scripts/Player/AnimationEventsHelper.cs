using AlessioBorriello;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class AnimationEventsHelper : MonoBehaviour
    {
        private PlayerManager playerManager;

        private void Start()
        {
            playerManager = GetComponentInParent<PlayerManager>();
        }

        public void EnableRightDamageCollider()
        {
            playerManager.inventoryManager.EnableRightDamageCollider();
        }

        public void EnableLeftDamageCollider()
        {
            playerManager.inventoryManager.EnableLeftDamageCollider();
        }

        public void DisableRightDamageCollider()
        {
            playerManager.inventoryManager.DisableRightDamageCollider();
        }

        public void DisableLeftDamageCollider()
        {
            playerManager.inventoryManager.DisableLeftDamageCollider();
        }
    }
}