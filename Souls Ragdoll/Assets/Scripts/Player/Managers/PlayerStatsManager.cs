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
        public int VigorLevel
        {
            get { 
                return vigorLevel; 
            }
            set {
                vigorLevel = value;
                MaxHealth = CalculateStatValue(vigorLevel, playerStats.vigorMaxLevel, playerStats.vigorDiminishingReturnCurve, playerStats.baseHealth, playerStats.baseHealthAdded);
                CurrentHealth = MaxHealth;
            }
        }

        private int maxHealth = 1;
        public int MaxHealth {
            get {
                return maxHealth;
            }
            private set
            {
                maxHealth = value;
                networkManager.netMaxHealth.Value = value;
            }
        }

        private int currentHealth = 1;
        public int CurrentHealth {
            get { 
                return currentHealth; 
            } 
            private set {
                currentHealth = value;
                networkManager.netCurrentHealth.Value = value;
            } 
        }
        #endregion

        #region Endurance
        private int enduranceLevel;
        public int EnduranceLevel
        {
            get {
                return vigorLevel;
            }
            set {
                enduranceLevel = value;
                MaxStamina = CalculateStatValue(enduranceLevel, playerStats.enduranceMaxLevel, playerStats.enduranceDiminishingReturnCurve, (int)playerStats.baseStamina, (int)playerStats.baseStaminaAdded);
                CurrentStamina = MaxStamina;
            }
        }

        private float maxStamina = 1;
        public float MaxStamina {
            get {
                return maxStamina;
            }
            private set {
                maxStamina = value;
            }
        }

        private float currentStamina = 1;
        public float CurrentStamina {
            get {
                return currentStamina;
            }
            private set {
                currentStamina = value;
            }
        }

        private float staminaRecoveryTimer = 0;
        #endregion

        #region Strength
        private int strengthLevel;
        public int StrengthLevel {
            get { 
                return strengthLevel; 
            }
            set {
                strengthLevel = value;
                Power = CalculateStatValue(strengthLevel, playerStats.strengthMaxLevel, playerStats.strengthDiminishingReturnCurve, playerStats.basePower, playerStats.basePowerAdded);
            }
        }

        private int power = 1;
        public int Power {
            get {
                return power;
            }
            private set {
                power = value;
            }
        }
        #endregion

        #region Poise
        private float maxPoise = 1;
        public float MaxPoise
        {
            get
            {
                return maxPoise;
            }
            private set
            {
                maxPoise = value;
            }
        }

        private float currentPoise = 1;
        public float CurrentPoise
        {
            get
            {
                return currentPoise;
            }
            private set
            {
                currentPoise = value;
            }
        }
        private float poiseRecoveryTimer = 0;
        #endregion

        void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
        }

        private void Start()
        {
            if (playerManager.IsOwner)
            {
                //VigorLevel = 25;
                VigorLevel = 1;
                StrengthLevel = 1;
                EnduranceLevel = 1;

                MaxPoise = 50;
                CurrentPoise = MaxPoise;
            }
            else
            {
                CurrentHealth = networkManager.netCurrentHealth.Value;
                MaxHealth = networkManager.netMaxHealth.Value;
            }
        }

        private void Update()
        {
            if (!playerManager.IsOwner) return;

            HandleStaminaRecovery();
            HandlePoiseRecovery();
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
            if(CurrentHealth <= 0)
            {
                playerManager.Die();
                networkManager.DieServerRpc();

            }
        }

        public void TakePoiseDamage(float damage)
        {
            CurrentPoise = Mathf.Max(CurrentPoise - damage, 0);

            poiseRecoveryTimer = playerStats.poiseResetTimer;
        }

        public bool IsPoiseBroken()
        {
            if (CurrentPoise <= 0)
            {
                Debug.Log("Broken poise");
                CurrentPoise = MaxPoise;
                return true;
            }else
            {
                return false;
            }
        }

        public IEnumerator TakeCriticalDamage(int damage, float delay)
        {
            yield return new WaitForSeconds(delay);
            CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
            if (CurrentHealth <= 0)
            {
                playerManager.GetCombatManager().diedFromCriticalDamage = true;

            }
        }

        public void ConsumeStamina(float staminaCost, float staminaRecoveryTime)
        {
            CurrentStamina = Mathf.Max(CurrentStamina - staminaCost, 0);
            staminaRecoveryTimer = staminaRecoveryTime;

            //Stamina penalty if the stamina is at 0
            if (CurrentStamina <= 0)
            {
                staminaRecoveryTimer = playerStats.staminaDefaultRecoveryTime * playerStats.staminaRecoveryTimerMultiplierOnStaminaDepleted;
                //Disable sprinting until stamina recovered a bit
                playerManager.disableSprint = true;
            }
        }

        public void ResetStats()
        {
            CurrentHealth = MaxHealth;
            CurrentStamina = MaxStamina;

            //Net
            networkManager.netCurrentHealth.Value = CurrentHealth;
        }

        private void HandleStaminaRecovery()
        {
            if (playerManager.isDead) return;

            if(!playerManager.isStuckInAnimation) staminaRecoveryTimer = Mathf.Max(staminaRecoveryTimer - Time.deltaTime, 0);

            float staminaRecovered = playerStats.staminaRecoveryRate * ((!playerManager.isBlocking)? 1f : playerStats.staminaRecoveryRateMultiplierWhenBlocking);
            if (staminaRecoveryTimer <= 0) CurrentStamina = Mathf.Min(CurrentStamina + staminaRecovered, MaxStamina);
        }

        private void HandlePoiseRecovery()
        {
            poiseRecoveryTimer = Mathf.Max(poiseRecoveryTimer - Time.deltaTime, 0);

            //Reset poise
            if (poiseRecoveryTimer <= 0) CurrentPoise = MaxPoise;
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
    
        public void SetCurrentHealth(int newCurrentHealth)
        {
            CurrentHealth = newCurrentHealth;
        }

        public void SetMaxHealth(int newMaxHealth)
        {
            MaxHealth = newMaxHealth;
        }

    }
}
