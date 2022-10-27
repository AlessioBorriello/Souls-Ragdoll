using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerInventoryManager : MonoBehaviour
    {

        private PlayerManager playerManager;

        public int numberOfSlots = 2;
        private HandEquippableItem[] rightHandItems;
        private HandEquippableItem[] leftHandItems;

        public HandEquippableItem currentEquippedRightItem;
        public HandEquippableItem currentEquippedLeftItem;

        private int currentRightHandItemIndex = 0;
        private int currentLeftHandItemIndex = 0;

        private HandItemHolder leftHolder;
        private HandItemHolder rightHolder;

        public HandEquippableItem testWeapon;
        public HandEquippableItem testShield;

        private void Awake()
        {
            //Initialize hand slots
            rightHandItems = new HandEquippableItem[numberOfSlots];
            leftHandItems = new HandEquippableItem[numberOfSlots];
        }

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();

            foreach(HandItemHolder itemHolder in GetComponentsInChildren<HandItemHolder>())
            {
                if(itemHolder.name == "ltemHolder.l") leftHolder = itemHolder;
                else rightHolder = itemHolder;
            }

            rightHandItems[0] = testWeapon;
            leftHandItems[0] = testShield;

            //Load Items
            LoadItemInSlot(false);
            LoadItemInSlot(true);

            //Set the equipped items as the current ones
            currentEquippedRightItem = rightHandItems[0];
            currentEquippedLeftItem = leftHandItems[0];
        }

        public void LoadItemInSlot(bool loadOnLeft)
        {

            HandEquippableItem item = (loadOnLeft)? leftHandItems[currentLeftHandItemIndex] : rightHandItems[currentRightHandItemIndex];

            if(item == null) return;

            if (loadOnLeft) leftHolder.LoadItemModel(item);
            else rightHolder.LoadItemModel(item);
        }

    }
}