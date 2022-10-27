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
        public int baseStamina = 200; //Base stamina when at level 1
        public int baseStaminaAdded = 6; //Stamina added per endurance level
        public AnimationCurve enduranceDiminishingReturnCurve; //Diminishing return

        [Header("Strength")]
        public int strengthMinLevel = 1; //Minimum of strength stat
        public int strengthMaxLevel = 99; //Maximum of strength stat
        public int basePower = 100; //Base power when at level 1
        public int basePowerAdded = 8; //Power added per strength level
        public AnimationCurve strengthDiminishingReturnCurve; //Diminishing return

    }
}
