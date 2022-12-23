using AlessioBorriello;
using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

namespace AlessioBorriello
{
    [Serializable]
    public struct AnimationEventStruct
    {
        public EventTypes eventType;
        public float eventTime;
    }

    public enum EventTypes
    {
        openCollider,
        closeCollider,
        enableParriable,
        disableParriable,
        openParry,
        setPlayerStuckInAnimation,
        setPlayerNotStuckInAnimation,
        checkForCriticalDamageDeath
    }
    public class AnimationEventsManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private PlayerNetworkManager networkManager;
        private PlayerLocomotionManager locomotionManager;
        private PlayerInventoryManager inventoryManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerCombatManager combatManager;
        private PlayerWeaponManager weaponManager;
        private PlayerShieldManager shieldManager;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
            locomotionManager = playerManager.GetLocomotionManager();
            inventoryManager = playerManager.GetInventoryManager();
            ragdollManager = playerManager.GetRagdollManager();
            combatManager = playerManager.GetCombatManager();
            weaponManager = playerManager.GetWeaponManager();
            shieldManager = playerManager.GetShieldManager();
        }

        public void AddJumpForceOnRoll(float force)
        {
            ragdollManager.AddForceToPlayer(locomotionManager.GetGroundNormal() * force, ForceMode.Impulse);
        }

        public void SetPlayerStuckInAnimation(bool toggle)
        {
            playerManager.isStuckInAnimation = toggle;
        }

        public void SetCanRotate(bool toggle)
        {
            playerManager.canRotate = toggle;
        }

        public void ToggleIFrames(bool toggle)
        {
            playerManager.areIFramesActive = toggle;
        }

        public void CheckForCriticalDamageDeath()
        {
            if(combatManager.diedFromCriticalDamage)
            {
                combatManager.diedFromCriticalDamage = false;
                playerManager.Die();
                networkManager.DieServerRpc();
            }
        }

        #region Collider stuff
        public void ToggleDamageCollider(bool enable)
        {
            DamageColliderControl colliderControl = inventoryManager.GetCurrentItemDamageColliderControl(weaponManager.IsAttackingWithLeft());
            if (colliderControl != null) colliderControl.ToggleCollider(enable);
        }

        public void ToggleWeaponParriable(bool enable)
        {
            DamageColliderControl colliderControl = inventoryManager.GetCurrentItemDamageColliderControl(weaponManager.IsAttackingWithLeft());
            if (colliderControl != null) colliderControl.ToggleParriable(enable);
        }

        public void OpenParry()
        {
            //If it's not a shield
            if (inventoryManager.GetCurrentItemType(shieldManager.IsParryingWithLeft()) != PlayerInventoryManager.ItemType.shield) return;

            ParryColliderControl parryColliderControl = inventoryManager.GetParryColliderControl();
            parryColliderControl.OpenParryCollider(((ShieldItem)inventoryManager.GetCurrentItem(shieldManager.IsParryingWithLeft())).parryDuration);
        }
        #endregion

        public AnimancerEvent.Sequence GetEventSequence(AnimationEventStruct[] events, float endTime)
        {
            AnimancerEvent.Sequence sequence = new AnimancerEvent.Sequence();
            //Set animation events
            foreach (AnimationEventStruct animationEvent in events)
            {
                Action _event = GetActionFromType(animationEvent.eventType);
                sequence.Add(animationEvent.eventTime, _event);
            }

            sequence.NormalizedEndTime = endTime;

            return sequence;
        }

        private Action GetActionFromType(EventTypes type)
        {
            switch (type)
            {
                case EventTypes.openCollider: return () => { ToggleDamageCollider(true); };
                case EventTypes.closeCollider: return () => { ToggleDamageCollider(false); };
                case EventTypes.enableParriable: return () => { ToggleWeaponParriable(true); };
                case EventTypes.disableParriable: return () => { ToggleWeaponParriable(false); };
                case EventTypes.openParry: return () => { OpenParry(); };
                case EventTypes.setPlayerStuckInAnimation: return () => { SetPlayerStuckInAnimation(true); };
                case EventTypes.setPlayerNotStuckInAnimation: return () => { SetPlayerStuckInAnimation(false); };
                case EventTypes.checkForCriticalDamageDeath: return () => { CheckForCriticalDamageDeath(); };
                default: return null;
            }
        }

    }
}