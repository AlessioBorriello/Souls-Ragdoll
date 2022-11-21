using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newPlayerStats", menuName = "Data/Player Data/Base Stats")]
    public class PlayerStats : ScriptableObject
    {

        [Header("Vigor")]
        public int vigorMinLevel = 1; //Minimum of vigor stat
        public int vigorMaxLevel = 99; //Maximum of vigor stat
        public int baseHealth = 400; //Base HP when at level 1
        public int baseHealthAdded = 25; //HP added per vigor level
        public AnimationCurve vigorDiminishingReturnCurve; //Diminishing return

        [Header("Endurance")]
        public int enduranceMinLevel = 1; //Minimum of endurance stat
        public int enduranceMaxLevel = 99; //Maximum of endurance stat
        public float baseStamina = 200; //Base stamina when at level 1
        public float baseStaminaAdded = 6; //Stamina added per endurance level
        public float staminaRecoveryRate = .6f; //Stamina recovered per frame
        public float staminaRecoveryRateMultiplierWhenBlocking = .3f; //Stamina recovery multiplier when blocking with a shield
        public float staminaDefaultRecoveryTime = 1.1f; //Stamina recovery timer after stamina use by default
        public float staminaRecoveryTimerMultiplierOnStaminaDepleted = 1.4f; //Timer multiplier when all the stamina is consumed
        public AnimationCurve enduranceDiminishingReturnCurve; //Diminishing return

        [Header("Strength")]
        public int strengthMinLevel = 1; //Minimum of strength stat
        public int strengthMaxLevel = 99; //Maximum of strength stat
        public int basePower = 100; //Base power when at level 1
        public int basePowerAdded = 8; //Power added per strength level
        public AnimationCurve strengthDiminishingReturnCurve; //Diminishing return

    }
}
