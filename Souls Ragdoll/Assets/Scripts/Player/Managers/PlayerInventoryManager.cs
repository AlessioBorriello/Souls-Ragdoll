using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static AlessioBorriello.PlayerCombatManager;

namespace AlessioBorriello
{

    public class PlayerInventoryManager : MonoBehaviour
    {

        private PlayerManager playerManager;
        private InputManager inputManager;
        private AnimationManager animationManager;
        private PlayerCombatManager combatManager;

        private Animator animator;
        private UIManager uiManager;

        //Item holders
        private HandItemHolder leftHolder;
        private HandItemHolder rightHolder;

        //Item slots
        [SerializeField] private HandEquippableItem[] itemsInRightSlots = new HandEquippableItem[3];
        [SerializeField] private HandEquippableItem[] itemsInLeftSlots = new HandEquippableItem[3];

        //The currently equipped items and the relative colliders
        private HandEquippableItem currentRightHandItem;
        private ColliderControl currentRightSlotItemColliderControl;
        private int currentRightItemSlotIndex = 0;
        private ItemType currentLeftItemType;

        private HandEquippableItem currentLeftHandItem;
        private ColliderControl currentLeftSlotItemColliderControl;
        private int currentLeftItemSlotIndex = 0;
        private ItemType currentRightItemType;

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();
            inputManager = playerManager.GetInputManager();
            animationManager = playerManager.GetAnimationManager();
            combatManager = playerManager.GetCombatManager();

            animator = animationManager.GetAnimator();
            uiManager = playerManager.GetUiManager();

            foreach (HandItemHolder itemHolder in GetComponentsInChildren<HandItemHolder>())
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

            uiManager.UpdateQuickSlotsUI(this);

        }

        private void LoadItemInHand(HandEquippableItem item, bool loadOnLeft)
        {

            if (loadOnLeft)
            {
                //Set item on left
                leftHolder.LoadItemModel(item);
                currentLeftHandItem = item;
                currentLeftSlotItemColliderControl = SetItemColliderControl(leftHolder);
            }
            else
            {
                //Set item on right
                rightHolder.LoadItemModel(item);
                currentRightHandItem = item;
                currentRightSlotItemColliderControl = SetItemColliderControl(rightHolder);
            }

            //Set idle animation
            LoadIdleAnimation(item, loadOnLeft);

            //Set item type
            SetCurrentItemType(item, loadOnLeft);

        }

        private void LoadIdleAnimation(HandEquippableItem item, bool loadOnLeft)
        {
            int layer = animator.GetLayerIndex((loadOnLeft) ? "Left Arm" : "Right Arm");
            if (item != null && item.oneHandedIdle != "") animator.CrossFade(item.oneHandedIdle, .1f, layer);
            else animator.CrossFade("Empty Hand Idle", .1f, layer);
        }

        private ColliderControl SetItemColliderControl(HandItemHolder holder)
        {
            ColliderControl collider;
            if (holder.currentItemModel == null) return null;
            collider = holder.currentItemModel.GetComponentInChildren<ColliderControl>();
            return (collider != null) ? collider : null;
        }

        public void HandleQuickSlots()
        {
            //Left and right hands
            int horizontalInput = (int)inputManager.dPadInput.x;

            if(horizontalInput > 0) ChangeHandItemSlot(ref currentRightItemSlotIndex, itemsInRightSlots, ref currentRightHandItem, false); //Right slot
            else if(horizontalInput < 0) ChangeHandItemSlot(ref currentLeftItemSlotIndex, itemsInLeftSlots, ref currentLeftHandItem, true); //Left slot

            //Spells


            //Items


            //Update icons
            if(inputManager.dPadInput.magnitude > 0) uiManager.UpdateQuickSlotsUI(this);
        }

        private void ChangeHandItemSlot(ref int index, HandEquippableItem[] slots, ref HandEquippableItem currentHandItem, bool onLeft)
        {
            if (playerManager.disableActions) return;

            index = (index + 1) % slots.Length;
            currentHandItem = slots[index];
            LoadItemInHand(currentHandItem, onLeft);

            //Play animation
            animationManager.UpdateChangingLeftItemValue(onLeft);
            animationManager.PlayTargetAnimation("ChangeItem", .15f, false);

            //Stop blocking
            playerManager.isBlocking = false;
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
        public ColliderControl GetCurrentItemColliderControl(bool leftHand)
        {
            return (leftHand) ? currentLeftSlotItemColliderControl : currentRightSlotItemColliderControl;
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

        public enum ItemType
        {
            unarmed,
            weapon,
            shield
        }

    }
}