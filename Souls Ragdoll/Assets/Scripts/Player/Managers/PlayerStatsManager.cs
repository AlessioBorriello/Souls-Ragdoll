using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerStatsManager : MonoBehaviour
    {

        private PlayerManager playerManager;
        private PlayerNetworkManager networkManager;

        public PlayerStats playerStats;

        #region Vigor
        private int vigorLevel;
        public int maxHealth { get; private set; } = 1;
        public int currentHealth { get; private set; } = 1;
        public int VigorLevel
        {
            get { return vigorLevel; }
            set 
            {
                vigorLevel = value;
                maxHealth = CalculateStatValue(vigorLevel, playerStats.vigorMaxLevel, playerStats.vigorDiminishingReturnCurve, playerStats.baseHealth, playerStats.baseHealthAdded);
                currentHealth = maxHealth;

                //Update net vars
                networkManager.netCurrentHealth.Value = currentHealth;
                networkManager.netMaxHealth.Value = maxHealth;
            }
        }
        #endregion

        #region Endurance
        private int enduranceLevel;
        public float maxStamina { get; private set; } = 1;
        public float currentStamina { get; private set; } = 1;
        public int EnduranceLevel
        {
            get { return vigorLevel; }
            set
            {
                enduranceLevel = value;
                maxStamina = CalculateStatValue(enduranceLevel, playerStats.enduranceMaxLevel, playerStats.enduranceDiminishingReturnCurve, (int)playerStats.baseStamina, (int)playerStats.baseStaminaAdded);
                currentStamina = maxStamina;
            }
        }
        private float staminaRecoveryTimer = 0;
        #endregion

        #region Strength
        private int strengthLevel;
        public int power { get; private set; }
        public int StrengthLevel
        {
            get { return strengthLevel; }
            set
            {
                strengthLevel = value;
                power = CalculateStatValue(strengthLevel, playerStats.strengthMaxLevel, playerStats.strengthDiminishingReturnCurve, playerStats.basePower, playerStats.basePowerAdded);
            }
        }
        #endregion

        void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
        }

        private void Start()
        {
            if (!playerManager.IsOwner) return;

            VigorLevel = 10;
            StrengthLevel = 1;
            EnduranceLevel = 10;
        }

        private void Update()
        {
            if (!playerManager.IsOwner) return;

            HandleStaminaRecovery();
        }

        public void TakeDamage(int damage)
        {
            currentHealth = Mathf.Max(currentHealth - damage, 0);
            if(currentHealth <= 0)
            {
                playerManager.Die();
                playerManager.DieServerRpc();

            }

            networkManager.netCurrentHealth.Value = currentHealth;
        }

        public IEnumerator TakeCriticalDamage(int damage, float delay)
        {
            yield return new WaitForSeconds(delay);
            currentHealth = Mathf.Max(currentHealth - damage, 0);
            if (currentHealth <= 0)
            {
                playerManager.GetCombatManager().diedFromCriticalDamage = true;

            }

            networkManager.netCurrentHealth.Value = currentHealth;
        }

        public void ConsumeStamina(float staminaCost, float staminaRecoveryTime)
        {
            currentStamina = Mathf.Max(currentStamina - staminaCost, 0);
            staminaRecoveryTimer = staminaRecoveryTime;

            //Stamina penalty if the stamina is at 0
            if (currentStamina <= 0)
            {
                staminaRecoveryTimer = playerStats.staminaDefaultRecoveryTime * playerStats.staminaRecoveryTimerMultiplierOnStaminaDepleted;
                //Disable sprinting until stamina recovered a bit
                playerManager.disableSprint = true;
            }
        }

        public void ResetStats()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;

            //Net
            networkManager.netCurrentHealth.Value = currentHealth;
        }

        private void HandleStaminaRecovery()
        {
            if (playerManager.isDead) return;

            if(!playerManager.playerIsStuckInAnimation) staminaRecoveryTimer = Mathf.Max(staminaRecoveryTimer - Time.deltaTime, 0);

            float staminaRecovered = playerStats.staminaRecoveryRate * ((!playerManager.isBlocking)? 1f : playerStats.staminaRecoveryRateMultiplierWhenBlocking);
            if (staminaRecoveryTimer <= 0) currentStamina = Mathf.Min(currentStamina + staminaRecovered, maxStamina);
        }

        private int CalculateStatValue(int statLevel, int maxStatLevel, AnimationCurve diminishingCurve, int baseStatValue, int baseStatValueAddition)
        {

            int statValue = baseStatValue;

            for(int level = 1; level < statLevel; level++)
            {
                float diminishingValue = (1.0f - diminishingCurve.Evaluate((float)level / (float)maxStatLevel));
                int additionalValue = (Mathf.CeilToInt(baseStatValueAddition * diminishingValue));
                statValue += additionalValue;
            }
            return statValue;
        }
    
    }
}
