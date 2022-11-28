using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlessioBorriello
{
    public class PlayerCombatManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private PlayerWeaponManager weaponManager;
        private PlayerShieldManager shieldManager;

        public bool diedFromCriticalDamage = false;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            weaponManager = playerManager.GetWeaponManager();
            shieldManager = playerManager.GetShieldManager();
        }

        public void HandleCombat()
        {
            if (playerManager.disableActions) return;

            //Attacks
            weaponManager.HandleAttacks();
            //Shields
            shieldManager.HandleBlocks();
        }

        public PlayerWeaponManager GetWeaponManager()
        {
            return weaponManager;
        }

        public PlayerShieldManager GetShieldManager()
        {
            return shieldManager;
        }

    }
}
