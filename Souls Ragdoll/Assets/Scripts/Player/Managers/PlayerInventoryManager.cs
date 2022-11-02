using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

namespace AlessioBorriello
{
    public class PlayerInventoryManager : MonoBehaviour
    {

        private PlayerManager playerManager;

        //Item holders
        private HandItemHolder leftHolder;
        private HandItemHolder rightHolder;

        //Item slots
        public HandEquippableItem[] itemsInRightSlots = new HandEquippableItem[3];
        public HandEquippableItem[] itemsInLeftSlots = new HandEquippableItem[3];

        //The currently equipped items and the relative colliders
        [HideInInspector] public HandEquippableItem currentRightHandItem;
        [HideInInspector] public Collider currentRightSlotItemCollider;
        private int currentRightItemSlotIndex = 0;

        [HideInInspector] public HandEquippableItem currentLeftHandItem;
        [HideInInspector] public Collider currentLeftSlotItemCollider;
        private int currentLeftItemSlotIndex = 0;

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();

            foreach(HandItemHolder itemHolder in GetComponentsInChildren<HandItemHolder>())
            {
                if(itemHolder.name == "ltemHolder.l") leftHolder = itemHolder;
                else rightHolder = itemHolder;
            }

            //Initialize items
            currentRightHandItem = itemsInRightSlots[currentRightItemSlotIndex];
            currentLeftHandItem = itemsInLeftSlots[currentLeftItemSlotIndex];

            //Load Items
            LoadItemInHand(currentRightHandItem, false);
            LoadItemInHand(currentLeftHandItem, true);

        }

        private void LoadItemInHand(HandEquippableItem item, bool loadOnLeft)
        {

            if (loadOnLeft)
            {
                //Set item on left
                leftHolder.LoadItemModel(item);
                currentLeftHandItem = item;
                currentLeftSlotItemCollider = GetItemCollider(leftHolder);
            }
            else
            {
                //Set item on right
                rightHolder.LoadItemModel(item);
                currentRightHandItem = item;
                currentRightSlotItemCollider = GetItemCollider(rightHolder);
            }

            //Set idle animation
            LoadIdleAnimation(item, loadOnLeft);

        }

        private void LoadIdleAnimation(HandEquippableItem item, bool loadOnLeft)
        {
            int layer = playerManager.animationManager.animator.GetLayerIndex((loadOnLeft) ? "Left Arm" : "Right Arm");
            if (item != null && item.OneHandedIdle != "") playerManager.animationManager.animator.CrossFade(item.OneHandedIdle, .1f, layer);
            else playerManager.animationManager.animator.CrossFade("Empty", .1f, layer);
        }

        private Collider GetItemCollider(HandItemHolder holder)
        {
            Collider collider;
            if (holder.currentItemModel == null) return null;
            collider = holder.currentItemModel.GetComponentInChildren<Collider>();
            return (collider != null) ? collider : null;
        }

        public void HandleQuickSlots()
        {
            //Left and right hands
            int horizontalInput = (int)playerManager.inputManager.dPadInput.x;

            if(horizontalInput > 0) //Right slot
            {
                currentRightItemSlotIndex = (currentRightItemSlotIndex + 1) % itemsInRightSlots.Length;
                currentRightHandItem = itemsInRightSlots[currentRightItemSlotIndex];
                LoadItemInHand(currentRightHandItem, false);
            }
            else if(horizontalInput < 0) //Left slot
            {
                currentLeftItemSlotIndex = (currentLeftItemSlotIndex + 1) % itemsInLeftSlots.Length;
                currentLeftHandItem = itemsInLeftSlots[currentLeftItemSlotIndex];
                LoadItemInHand(currentLeftHandItem, true);
            }

            //Spells


            //Items
        }
    }
}