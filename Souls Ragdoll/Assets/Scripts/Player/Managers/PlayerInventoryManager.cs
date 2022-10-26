using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerInventoryManager : MonoBehaviour
    {

        private PlayerManager playerManager;

        public int numberOfSlots = 2;
        public HandEquippableItem[] rightHandItems;
        public HandEquippableItem[] leftHandItems;

        private int currentRightHandItem = 0;
        private int currentLeftHandItem = 0;

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

            rightHandItems[currentRightHandItem] = testWeapon;
            leftHandItems[currentLeftHandItem] = testShield;

            //Load Items
            LoadItemInSlot(false);
            LoadItemInSlot(true);
        }

        public void LoadItemInSlot(bool loadOnLeft)
        {

            HandEquippableItem item = (loadOnLeft)? leftHandItems[currentLeftHandItem] : rightHandItems[currentRightHandItem];

            if(item == null) return;

            if (loadOnLeft) leftHolder.LoadItemModel(item);
            else rightHolder.LoadItemModel(item);
        }

    }
}