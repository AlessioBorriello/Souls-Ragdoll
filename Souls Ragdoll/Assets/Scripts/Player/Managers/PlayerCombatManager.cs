using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerCombatManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private PlayerWeaponManager weaponManager;
        private PlayerShieldManager shieldManager;

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
