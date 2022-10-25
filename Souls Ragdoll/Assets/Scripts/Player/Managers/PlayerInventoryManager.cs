using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerInventoryManager : MonoBehaviour
    {

        private PlayerManager playerManager;

        public HandEquippableItem currentRightItem;
        public HandEquippableItem currentLeftItem;

        private HandItemHolder leftHolder;
        private HandItemHolder rightHolder;

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();

            foreach(HandItemHolder itemHolder in GetComponentsInChildren<HandItemHolder>())
            {
                if(itemHolder.name == "ltemHolder.l") leftHolder = itemHolder;
                else rightHolder = itemHolder;
            }

            //Load Items
            LoadItemInSlot(currentLeftItem, true);
            LoadItemInSlot(currentRightItem, false);
        }

        public void LoadItemInSlot(HandEquippableItem item, bool loadOnLeft)
        {
            if (loadOnLeft) leftHolder.LoadItemModel(item);
            else rightHolder.LoadItemModel(item);
        }

    }
}