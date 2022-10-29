using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerInventoryManager : MonoBehaviour
    {

        private PlayerManager playerManager;

        //Item holders (slots)
        private HandItemHolder leftHolder;
        private HandItemHolder rightHolder;

        //The currently equipped items and the relative colliders
        public HandEquippableItem currentRightSlotItem;
        private DamageCollider currentRightSlotItemCollider;

        public HandEquippableItem currentLeftSlotItem;
        private DamageCollider currentLeftSlotItemCollider;

        //For testing
        public HandEquippableItem testWeapon;
        public HandEquippableItem testShield;

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();

            foreach(HandItemHolder itemHolder in GetComponentsInChildren<HandItemHolder>())
            {
                if(itemHolder.name == "ltemHolder.l") leftHolder = itemHolder;
                else rightHolder = itemHolder;
            }

            //Load Items
            LoadItemInSlot(testWeapon, false);
            LoadItemInSlot(testShield, true);

        }

        private void LoadItemInSlot(HandEquippableItem item, bool loadOnLeft)
        {
            if (loadOnLeft)
            {
                leftHolder.LoadItemModel(item);
                currentLeftSlotItem = item;
                currentLeftSlotItemCollider = GetItemCollider(leftHolder);
            }
            else
            {
                rightHolder.LoadItemModel(item);
                currentRightSlotItem = item;
                currentRightSlotItemCollider = GetItemCollider(rightHolder);
            }
        }

        private DamageCollider GetItemCollider(HandItemHolder holder)
        {
            return holder.currentItemModel.GetComponentInChildren<DamageCollider>();
        }

        #region Collider stuff
        public void EnableRightDamageCollider()
        {
            currentRightSlotItemCollider.EnableDamageCollider();
        }

        public void EnableLeftDamageCollider()
        {
            currentLeftSlotItemCollider.EnableDamageCollider();
        }

        public void DisableRightDamageCollider()
        {
            currentRightSlotItemCollider.DisableDamageCollider();
        }

        public void DisableLeftDamageCollider()
        {
            currentLeftSlotItemCollider.DisableDamageCollider();
        }
        #endregion
    }
}