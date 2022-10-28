using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerStatsManager : MonoBehaviour
    {

        private PlayerManager playerManager;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] public UIManager uiManager;

        #region Vigor
        private int vigorLevel;
        public int maxHealth;
        public int currentHealth;
        public int VigorLevel
        {
            get { return vigorLevel; }
            set 
            {
                vigorLevel = value;
                maxHealth = CalculateStatValue(vigorLevel, playerStats.vigorMaxLevel, playerStats.vigorDiminishingReturnCurve, playerStats.baseHealth, playerStats.baseHealthAdded);
                currentHealth = maxHealth;
            }
        }
        #endregion

        #region Endurance
        private int enduranceLevel;
        public int maxStamina;
        public int currentStamina;
        public int EnduranceLevel
        {
            get { return vigorLevel; }
            set
            {
                enduranceLevel = value;
                maxStamina = CalculateStatValue(enduranceLevel, playerStats.enduranceMaxLevel, playerStats.enduranceDiminishingReturnCurve, playerStats.baseStamina, playerStats.baseStaminaAdded);
                currentStamina = maxStamina;
            }
        }
        #endregion

        #region Strength
        private int strengthLevel;
        [SerializeField] private int power;
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

        void Start()
        {

            playerManager = GetComponent<PlayerManager>();

            VigorLevel = 1;
            StrengthLevel = 1;
            EnduranceLevel = 1;

        }

        public void ReduceHealth(int damage, string hurtAnimationName, float transitionTime)
        {
            currentHealth -= damage;
            playerManager.animationManager.PlayTargetAnimation(hurtAnimationName, transitionTime);
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
