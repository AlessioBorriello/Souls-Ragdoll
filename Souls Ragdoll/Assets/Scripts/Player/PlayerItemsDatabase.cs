using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerItemsDatabase : MonoBehaviour
    {
        public static PlayerItemsDatabase Instance { get; private set; }

        [Serializable]
        private struct PlayerItem
        {
            //Id : XYZZ
            //X : item category, 1 weapons, 2 shields...
            //Y : item type, example 1 short swords, 2 axes, 3 spears...
            //ZZ : item number
            public int id;
            public Item item;
        }

        [SerializeField] private PlayerItem[] weapons;
        [SerializeField] private PlayerItem[] shields;

        //Dictionary of items
        private Dictionary<int, HandEquippableItem> playerHandEquippableItems = new Dictionary<int, HandEquippableItem>();

        private void Awake()
        {
            //Create singleton
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;

            //Add weapons to equippable dictionary
            foreach (PlayerItem item in weapons)
            {
                if (item.item is not HandEquippableItem) continue;
                int id = item.id;
                playerHandEquippableItems.Add(id, (HandEquippableItem)item.item);
            }

            //Add shield to equippable dictionary
            foreach (PlayerItem item in shields)
            {
                int id = item.id;
                playerHandEquippableItems.Add(id, (HandEquippableItem)item.item);
            }
        }

        public HandEquippableItem GetHandEquippableItem(int id)
        {
            if (!playerHandEquippableItems.TryGetValue(id, out HandEquippableItem item)) return null;
            else return item;
        }
    }
}