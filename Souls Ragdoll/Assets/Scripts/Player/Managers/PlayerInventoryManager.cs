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
        [HideInInspector] public HandEquippableItem currentRightSlotItem;
        [HideInInspector] public Collider currentRightSlotItemCollider;

        [HideInInspector] public HandEquippableItem currentLeftSlotItem;
        [HideInInspector] public Collider currentLeftSlotItemCollider;

        //For testing
        public HandEquippableItem shortSword;
        public HandEquippableItem spear;
        public HandEquippableItem battleAxe;
        public HandEquippableItem shield;

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();

            foreach(HandItemHolder itemHolder in GetComponentsInChildren<HandItemHolder>())
            {
                if(itemHolder.name == "ltemHolder.l") leftHolder = itemHolder;
                else rightHolder = itemHolder;
            }

            //Load Items
            LoadItemInSlot(shortSword, false);
            LoadItemInSlot(battleAxe, true);

        }

        private void LoadItemInSlot(HandEquippableItem item, bool loadOnLeft)
        {

            if (loadOnLeft)
            {
                //Set item on left
                leftHolder.LoadItemModel(item);
                currentLeftSlotItem = item;
                currentLeftSlotItemCollider = GetItemCollider(leftHolder);
            }
            else
            {
                //Set item on right
                rightHolder.LoadItemModel(item);
                currentRightSlotItem = item;
                currentRightSlotItemCollider = GetItemCollider(rightHolder);
            }

            //Set idle animation
            int layer = playerManager.animationManager.animator.GetLayerIndex((loadOnLeft)? "Left Arm" : "Right Arm");
            if (item != null) playerManager.animationManager.animator.CrossFade(item.OneHandedIdle, .1f, layer);
            else playerManager.animationManager.animator.CrossFade("Empty", .1f, layer);

        }

        private Collider GetItemCollider(HandItemHolder holder)
        {
            return holder.currentItemModel.GetComponentInChildren<Collider>();
        }

    }
}