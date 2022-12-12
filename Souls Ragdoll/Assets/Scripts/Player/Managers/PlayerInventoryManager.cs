using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{

    public class PlayerInventoryManager : MonoBehaviour
    {

        private PlayerManager playerManager;
        private PlayerNetworkManager networkManager;
        private InputManager inputManager;
        private AnimationManager animationManager;

        private Animator animator;
        private UIManager uiManager;

        //Item holders
        private HandItemHolder leftHolder;
        private HandItemHolder rightHolder;

        //Item slots
        [SerializeField] private int[] itemsIdInRightSlots = new int[3];
        private int currentRightItemSlotIndex = 0;

        [SerializeField] private int[] itemsIdInLeftSlots = new int[3];
        private int currentLeftItemSlotIndex = 0;

        //The currently equipped items and the relative colliders
        private HandEquippableItem currentRightHandItem;
        private DamageColliderControl currentRightItemDamageColliderControl;
        private ItemType currentLeftItemType;

        private HandEquippableItem currentLeftHandItem;
        private DamageColliderControl currentLeftItemDamageColliderControl;
        private ItemType currentRightItemType;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
            inputManager = playerManager.GetInputManager();
            animationManager = playerManager.GetAnimationManager();

            animator = animationManager.GetAnimator();
            uiManager = playerManager.GetUiManager();

            foreach (HandItemHolder itemHolder in GetComponentsInChildren<HandItemHolder>())
            {
                if(itemHolder.name == "ltemHolder.l") leftHolder = itemHolder;
                else rightHolder = itemHolder;
            }

        }

        private void Start()
        {
            if(!playerManager.IsOwner)
            {
                currentLeftItemSlotIndex = networkManager.netCurrentLeftItemSlotIndex.Value;
                currentRightItemSlotIndex = networkManager.netCurrentRightItemSlotIndex.Value;
            }

            LoadItemInHand(true, itemsIdInLeftSlots[currentLeftItemSlotIndex]);
            LoadItemInHand(false, itemsIdInRightSlots[currentRightItemSlotIndex]);

            if(playerManager.IsOwner) uiManager.UpdateQuickSlotsUI(this);
        }

        private void LoadItemInHand(bool loadOnLeft, int itemId)
        {
            HandEquippableItem item = PlayerItemsDatabase.Instance.GetHandEquippableItem(itemId);
            if (loadOnLeft)
            {
                currentLeftHandItem = item;

                //Set item on left
                leftHolder.LoadItemModel(item);
                currentLeftHandItem = item;
                currentLeftItemDamageColliderControl = SetItemColliderControl(leftHolder);
            }
            else
            {
                currentRightHandItem = item;

                //Set item on right
                rightHolder.LoadItemModel(item);
                currentRightHandItem = item;
                currentRightItemDamageColliderControl = SetItemColliderControl(rightHolder);
            }

            //Set idle animation
            LoadIdleAnimation(item, loadOnLeft);

            //Set item type
            SetCurrentItemType(item, loadOnLeft);

            //Update UI
            if (playerManager.IsOwner) uiManager.UpdateQuickSlotsUI(this);

        }

        private void LoadIdleAnimation(HandEquippableItem item, bool loadOnLeft)
        {
            int layer = (loadOnLeft) ? 1 : 2;
            if (item != null && item.oneHandedIdle != "") animationManager.PlayOverrideAnimation(item.oneHandedIdle + ((loadOnLeft) ? "Left" : "Right"), null, null, layer);
            else animationManager.FadeOutOverrideAnimation(.1f, layer);
        }

        private DamageColliderControl SetItemColliderControl(HandItemHolder holder)
        {
            DamageColliderControl collider;
            if (holder.currentItemModel == null) return null;
            collider = holder.currentItemModel.GetComponentInChildren<DamageColliderControl>();
            return collider;
        }

        public void HandleQuickSlots()
        {
            if (playerManager.disableActions) return;

            Vector2 slotInput = inputManager.dPadInput;

            //Equipment slots
            if((int)slotInput.x != 0)
            {
                bool leftHand = (slotInput.x < 0);

                int id;
                if(leftHand)
                {
                    int newIndex;
                    if(playerManager.IsOwner)
                    {
                        //Calculate new index
                        newIndex = (currentLeftItemSlotIndex + 1) % itemsIdInLeftSlots.Length;
                        networkManager.netCurrentLeftItemSlotIndex.Value = newIndex;
                    }
                    else
                    {
                        newIndex = networkManager.netCurrentLeftItemSlotIndex.Value;
                    }

                    currentLeftItemSlotIndex = newIndex;
                    id = itemsIdInLeftSlots[currentLeftItemSlotIndex];
                }
                else
                {
                    int newIndex;
                    if (playerManager.IsOwner)
                    {
                        //Calculate new index
                        newIndex = (currentRightItemSlotIndex + 1) % itemsIdInRightSlots.Length;
                        networkManager.netCurrentRightItemSlotIndex.Value = newIndex;
                    }
                    else
                    {
                        newIndex = networkManager.netCurrentRightItemSlotIndex.Value;
                    }

                    currentRightItemSlotIndex = newIndex;
                    id = itemsIdInRightSlots[currentRightItemSlotIndex];
                }

                ChangeHandItemSlot(leftHand, id);
                networkManager.ChangeHandItemSlotServerRpc(leftHand, id);
            }

            //Spells


            //Items

        }

        public void ChangeHandItemSlot(bool leftHand, int itemId)
        {

            Action onChangeItemEnter = () =>
            {
                //Debug.Log("Change item enter");
                playerManager.isBlocking = false;
                playerManager.disableActions = true;
            };

            Action onChangeItemExit = () =>
            {
                //Debug.Log("Change item exit");
                playerManager.disableActions = false;
                animationManager.FadeOutOverrideAnimation(.1f, (leftHand)? 1 : 2);

                LoadItemInHand((leftHand)? true : false, itemId);
            };

            //Play animation
            string animationName = "ChangeItem" + ((leftHand) ? "Left" : "Right");
            int layer = (leftHand) ? 1 : 2;
            animationManager.PlayOverrideAnimation(animationName, onChangeItemEnter, onChangeItemExit, layer);

        }

        /// <summary>
        /// Gets the currently equipped item in either hands
        /// </summary>
        /// <param name="leftHand">Current item equipped in the left hand if true, in the right hand if false</param>
        /// <returns>The currently equipped item</returns>
        public HandEquippableItem GetCurrentItem(bool leftHand)
        {
            return (leftHand) ? currentLeftHandItem : currentRightHandItem;
        }

        /// <summary>
        /// Gets the collider control of the currently equipped item in either hands
        /// </summary>
        /// <param name="leftHand">Collider control of the currently equipped item in the left hand if true, in the right hand if false</param>
        /// <returns>The collider control of the currently equipped item</returns>
        public DamageColliderControl GetCurrentItemDamageColliderControl(bool leftHand)
        {
            return (leftHand) ? currentLeftItemDamageColliderControl : currentRightItemDamageColliderControl;
        }

        public void SetDamageColliderValues(int damage, int poiseDamage, int staminaDamage, float knockbackStrength, string staggerAnimation)
        {
            currentRightItemDamageColliderControl.SetColliderValues(damage, poiseDamage, staminaDamage, knockbackStrength, staggerAnimation);
        }

        public void SetCurrentItemType(HandEquippableItem item, bool leftHand)
        {
            ItemType type = ItemType.unarmed;

            if (item is WeaponItem) type = ItemType.weapon;
            else if (item is ShieldItem) type = ItemType.shield;

            if (leftHand) currentLeftItemType = type;
            else currentRightItemType = type;
        }

        public ItemType GetCurrentItemType(bool leftHand)
        {
            return (leftHand) ? currentLeftItemType : currentRightItemType;
        }

        public int GetCurrentItemIndex(bool leftHand)
        {
            return (leftHand) ? currentLeftItemSlotIndex : currentRightItemSlotIndex;
        }

        public void SetCurrentItemIndex(bool leftHand, int index)
        {
            if (leftHand) currentLeftItemSlotIndex = index;
            else currentRightItemSlotIndex = index;
        }

        public enum ItemType
        {
            unarmed,
            weapon,
            shield
        }

    }
}