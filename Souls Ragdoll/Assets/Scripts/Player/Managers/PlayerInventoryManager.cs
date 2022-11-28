using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{

    public class PlayerInventoryManager : NetworkBehaviour
    {

        private PlayerManager playerManager;
        private InputManager inputManager;
        private AnimationManager animationManager;

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
        private DamageColliderControl currentRightItemDamageColliderControl;
        private NetworkVariable<int> currentRightItemSlotIndex = new(0, writePerm: NetworkVariableWritePermission.Owner);
        private ItemType currentLeftItemType;

        private HandEquippableItem currentLeftHandItem;
        private DamageColliderControl currentLeftItemDamageColliderControl;
        private NetworkVariable<int> currentLeftItemSlotIndex = new(0, writePerm: NetworkVariableWritePermission.Owner);
        private ItemType currentRightItemType;

        private NetworkVariable<bool> netIsChangingWithLeft = new(false, writePerm: NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            inputManager = playerManager.GetInputManager();
            animationManager = playerManager.GetAnimationManager();

            animator = animationManager.GetAnimator();
            uiManager = playerManager.GetUiManager();

            foreach (HandItemHolder itemHolder in GetComponentsInChildren<HandItemHolder>())
            {
                if(itemHolder.name == "ltemHolder.l") leftHolder = itemHolder;
                else rightHolder = itemHolder;
            }

            //Initialize items
            currentRightHandItem = itemsInRightSlots[currentRightItemSlotIndex.Value];
            currentLeftHandItem = itemsInLeftSlots[currentLeftItemSlotIndex.Value];

            uiManager.UpdateQuickSlotsUI(this);

        }

        public override void OnNetworkSpawn()
        {
            currentRightItemSlotIndex.OnValueChanged += (int previousValue, int newValue) =>
            {
                if (IsOwner) return;
                LoadItemInHand(false);
            };

            currentLeftItemSlotIndex.OnValueChanged += (int previousValue, int newValue) => 
            {
                if (IsOwner) return;
                LoadItemInHand(true);
            };

            netIsChangingWithLeft.OnValueChanged += (bool previousValue, bool newValue) => animationManager.UpdateChangingLeftItemValue(newValue);
            
            LoadItemInHand(false);
            LoadItemInHand(true);
        }

        private void LoadItemInHand(bool loadOnLeft)
        {
            HandEquippableItem item;
            if (loadOnLeft)
            {
                item = itemsInLeftSlots[currentLeftItemSlotIndex.Value];
                currentLeftHandItem = item;

                //Set item on left
                leftHolder.LoadItemModel(item);
                currentLeftHandItem = item;
                currentLeftItemDamageColliderControl = SetItemColliderControl(leftHolder);
            }
            else
            {
                item = itemsInRightSlots[currentRightItemSlotIndex.Value];
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

        }

        private void LoadIdleAnimation(HandEquippableItem item, bool loadOnLeft)
        {
            int layer = animator.GetLayerIndex((loadOnLeft) ? "Left Arm" : "Right Arm");
            if (item != null && item.oneHandedIdle != "") animationManager.PlayTargetAnimation(item.oneHandedIdle, .1f, playerManager.playerIsStuckInAnimation, layer);
            else animationManager.PlayTargetAnimation("Empty Hand Idle", .1f, playerManager.playerIsStuckInAnimation, layer);
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
            //Left and right hands
            int horizontalInput = (int)inputManager.dPadInput.x;

            //Right slot
            if (horizontalInput > 0) ChangeHandItemSlot(false);
            //Left slot
            else if (horizontalInput < 0) ChangeHandItemSlot(true);

            //Spells


            //Items


            //Update icons
            if (inputManager.dPadInput.magnitude > 0) uiManager.UpdateQuickSlotsUI(this);
        }

        private void ChangeHandItemSlot(bool leftHand)
        {
            if (playerManager.disableActions) return;

            if(leftHand)
            {
                currentLeftItemSlotIndex.Value = (currentLeftItemSlotIndex.Value + 1) % itemsInLeftSlots.Length;
                LoadItemInHand(true);
            }
            else
            {
                currentRightItemSlotIndex.Value = (currentRightItemSlotIndex.Value + 1) % itemsInRightSlots.Length;
                LoadItemInHand(false);
            }

            //Play animation
            netIsChangingWithLeft.Value = leftHand;
            animationManager.UpdateChangingLeftItemValue(leftHand);
            animationManager.PlayTargetAnimation("ChangeItem", .15f, playerManager.playerIsStuckInAnimation);

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
        public DamageColliderControl GetCurrentItemDamageColliderControl(bool leftHand)
        {
            return (leftHand) ? currentLeftItemDamageColliderControl : currentRightItemDamageColliderControl;
        }

        public void SetColliderValues(int damage, float knockbackStrength, float flinchStrenght, bool leftHand)
        {
            if(leftHand)
            {
                currentLeftItemDamageColliderControl.SetColliderValues(damage, knockbackStrength, flinchStrenght);
            }else
            {
                currentRightItemDamageColliderControl.SetColliderValues(damage, knockbackStrength, flinchStrenght);
            }
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