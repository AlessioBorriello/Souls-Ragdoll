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

        [SerializeField] private AnimationData changeItemAnimationData;

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

        //Parry collider
        private ParryColliderControl parryColliderControl;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
            inputManager = playerManager.GetInputManager();
            animationManager = playerManager.GetAnimationManager();

            parryColliderControl = GetComponentInChildren<ParryColliderControl>();

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

            LoadItemInHand(true);
            LoadItemInHand(false);

            if(playerManager.IsOwner) uiManager.UpdateQuickSlotsUI(this);
        }

        public void LoadItemInHand(bool loadOnLeft)
        {
            //Get item id
            int itemId = (loadOnLeft) ? itemsIdInLeftSlots[currentLeftItemSlotIndex] : itemsIdInRightSlots[currentRightItemSlotIndex];

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
            LoadIdleAnimation(loadOnLeft, false);

            //Set item type
            SetCurrentItemType(item, loadOnLeft);

            //Update UI
            if (playerManager.IsOwner) uiManager.UpdateQuickSlotsUI(this);

        }

        public void UnloadItemInHand(bool onLeft)
        {
            HandItemHolder holder = (onLeft) ? leftHolder : rightHolder;
            holder.UnloadItemModel();
        }

        public void LoadIdleAnimation(bool loadOnLeft, bool twoHanded)
        {
            HandEquippableItem item = (loadOnLeft)? currentLeftHandItem : currentRightHandItem;

            AnimationData idleAnimation = (twoHanded) ? item.twoHandedIdleAnimationData : item.oneHandedIdleAnimationData;
            OverrideLayers layer = (twoHanded)? OverrideLayers.bothArmsLayer : ((loadOnLeft) ? OverrideLayers.leftArmLayer : OverrideLayers.rightArmLayer);

            if (item != null && idleAnimation != null) animationManager.PlayOverrideAnimation(idleAnimation, null, null, layer, loadOnLeft);
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

                //Increase the corrisponding index
                if (!leftHand) IncreaseItemIndex(ref currentRightItemSlotIndex, ref networkManager.netCurrentRightItemSlotIndex, itemsIdInRightSlots.Length);
                else  IncreaseItemIndex(ref currentLeftItemSlotIndex, ref networkManager.netCurrentLeftItemSlotIndex, itemsIdInLeftSlots.Length);

                ChangeHandItemSlot(leftHand);
                networkManager.ChangeHandItemSlotServerRpc(leftHand);
            }

            //Spells


            //Items

        }

        public void ChangeHandItemSlot(bool leftHand)
        {

            if (playerManager.disableActions) return;

            //Stop two handing
            if (playerManager.GetCombatManager().twoHanding)
            {
                playerManager.GetCombatManager().StopTwoHanding();
                networkManager.StopTwoHandingServerRpc();
            }

            Action onChangeItemEnter = () =>
            {
                //Debug.Log("Change item enter");
                //Stop blocking
                if(playerManager.isBlocking)
                {
                    playerManager.GetShieldManager().StopBlocking();
                    networkManager.StopBlockingServerRpc();
                }

                playerManager.disableActions = true;
            };

            Action onChangeItemExit = () =>
            {
                //Debug.Log("Change item exit");
                StartCoroutine(EnableActionsAfterTime(.1f));
                animationManager.FadeOutOverrideAnimation(.1f, (leftHand) ? OverrideLayers.leftArmLayer : OverrideLayers.rightArmLayer);

                LoadItemInHand((leftHand)? true : false);
            };

            //Play animation
            OverrideLayers layer = (leftHand) ? OverrideLayers.leftArmLayer : OverrideLayers.rightArmLayer;
            animationManager.PlayOverrideAnimation(changeItemAnimationData, onChangeItemEnter, onChangeItemExit, layer, leftHand);

        }

        private void IncreaseItemIndex(ref int currentItemSlotIndex, ref NetworkVariable<int> netCurrentItemSlotIndex, int itemsAmount)
        {
            int newIndex;
            if (playerManager.IsOwner)
            {
                //Calculate new index
                newIndex = (currentItemSlotIndex + 1) % itemsAmount;
                netCurrentItemSlotIndex.Value = newIndex;
            }
            else newIndex = netCurrentItemSlotIndex.Value;

            currentItemSlotIndex = newIndex;
        }

        private IEnumerator EnableActionsAfterTime(float time)
        {
            yield return new WaitForSeconds(time);
            playerManager.disableActions = false;
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

        public ParryColliderControl GetParryColliderControl()
        {
            return parryColliderControl;
        }

        public void SetDamageColliderValues(DamageColliderInfo colliderInfo, bool isLeft)
        {
            if(!isLeft) currentRightItemDamageColliderControl.SetColliderValues(colliderInfo);
            else currentLeftItemDamageColliderControl.SetColliderValues(colliderInfo);
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