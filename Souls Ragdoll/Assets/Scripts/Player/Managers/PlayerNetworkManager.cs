using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Unity.Netcode;
using UnityEngine;
using static AlessioBorriello.PlayerWeaponManager;

namespace AlessioBorriello
{
    public class PlayerNetworkManager : NetworkBehaviour
    {
        [SerializeField] private bool showClientNetworkRpcs = false;

        //Components
        private PlayerManager playerManager;
        private AnimationManager animationManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerLocomotionManager locomotionManager;
        private PlayerStatsManager statsManager;
        private PlayerWeaponManager weaponManager;
        private PlayerShieldManager shieldManager;
        private PlayerCollisionManager collisionManager;
        private PlayerInventoryManager inventoryManager;
        private PlayerCombatManager combatManager;
        private Transform cameraTransform;

        private Rigidbody physicalHips;
        private GameObject animatedPlayer;

        private NetworkVariable<Vector3> netPosition = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> netRotation = new(writePerm: NetworkVariableWritePermission.Owner);

        //Camera
        public NetworkVariable<Vector3> netCameraForward = new(writePerm: NetworkVariableWritePermission.Owner);

        public NetworkVariable<float> netNormalMovementAmount = new(0, writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<float> netStrafeMovementAmount = new(0, writePerm: NetworkVariableWritePermission.Owner);

        //Stats
        public NetworkVariable<int> netCurrentHealth = new(0, writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<int> netMaxHealth = new(0, writePerm: NetworkVariableWritePermission.Owner);

        //Inventory
        public NetworkVariable<int> netCurrentRightItemSlotIndex = new(0, writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<int> netCurrentLeftItemSlotIndex = new(0, writePerm: NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            animationManager = playerManager.GetAnimationManager();
            ragdollManager = playerManager.GetRagdollManager();
            locomotionManager = playerManager.GetLocomotionManager();
            statsManager = playerManager.GetStatsManager();
            weaponManager = playerManager.GetWeaponManager();
            shieldManager = playerManager.GetShieldManager();
            collisionManager = playerManager.GetCollisionManager();
            inventoryManager = playerManager.GetInventoryManager();
            combatManager = playerManager.GetCombatManager();

            cameraTransform = playerManager.GetCameraTransform();

            physicalHips = playerManager.GetPhysicalHips();
            animatedPlayer = playerManager.GetAnimatedPlayer();
        }

        public override void OnNetworkSpawn()
        {
            netCurrentHealth.OnValueChanged += (int oldCurrentHealth, int newCurrentHealth) => statsManager.SetCurrentHealth(newCurrentHealth);
            netMaxHealth.OnValueChanged += (int oldMaxHealth, int newMaxHealth) => statsManager.SetMaxHealth(newMaxHealth);

            netCurrentLeftItemSlotIndex.OnValueChanged += (int oldIndex, int newIndex) => inventoryManager.SetCurrentItemIndex(true, newIndex);
            netCurrentRightItemSlotIndex.OnValueChanged += (int oldIndex, int newIndex) => inventoryManager.SetCurrentItemIndex(false, newIndex);
        }

        public override void OnNetworkDespawn()
        {
            netCurrentHealth.OnValueChanged -= (int oldCurrentHealth, int newCurrentHealth) => statsManager.SetCurrentHealth(newCurrentHealth);
            netMaxHealth.OnValueChanged -= (int oldMaxHealth, int newMaxHealth) => statsManager.SetMaxHealth(newMaxHealth);
        }

        private Vector3 posVel;
        private float rotVel = 15;
        private void Update()
        {
            if (playerManager.isDead) return;

            if (IsOwner)
            {
                netPosition.Value = physicalHips.transform.position;
                netRotation.Value = animatedPlayer.transform.rotation;
                netCameraForward.Value = cameraTransform.forward;
            }
            else
            {
                if(Vector3.Distance(physicalHips.transform.position, netPosition.Value) > 1.8f) physicalHips.transform.position = netPosition.Value;
                else physicalHips.transform.position = Vector3.SmoothDamp(physicalHips.transform.position, netPosition.Value, ref posVel, .1f);

                animatedPlayer.transform.rotation = Quaternion.Slerp(animatedPlayer.transform.rotation, netRotation.Value, rotVel);
                animationManager.UpdateMovementAnimatorValues(netNormalMovementAmount.Value, netStrafeMovementAmount.Value, 0);
            }

        }


        #region RPCs

        #region Game logic

        //Player death
        [ServerRpc(RequireOwnership = false)]
        public void DieServerRpc()
        {
            DieClientRpc();
        }

        [ClientRpc]
        private void DieClientRpc()
        {
            if (IsOwner) return;
            if(showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} died");
            playerManager.Die();
        }

        //Player respawn
        [ServerRpc(RequireOwnership = false)]
        public void RespawnPlayerServerRpc(float respawnTime = 1.5f)
        {
            RespawnPlayerClientRpc(respawnTime);
        }

        [ClientRpc]
        private void RespawnPlayerClientRpc(float respawnTime = 1.5f)
        {
            if (IsOwner) return; //Already respawned
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} respawned");
            StartCoroutine(playerManager.RespawnPlayer(respawnTime));
        }

        #endregion

        #region Locomotion

        //Roll
        [ServerRpc(RequireOwnership = false)]
        public void RollServerRpc()
        {
            RollClientRpc();
        }

        [ClientRpc]
        private void RollClientRpc()
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} rolled");
            locomotionManager.Roll();
        }

        //Backdash
        [ServerRpc(RequireOwnership = false)]
        public void BackdashServerRpc()
        {
            BackdashClientRpc();
        }

        [ClientRpc]
        private void BackdashClientRpc()
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} backdashed");
            locomotionManager.Backdash();
        }

        //Fall
        [ServerRpc(RequireOwnership = false)]
        public void StartFallingServerRpc()
        {
            StartFallingClientRpc();
        }

        [ClientRpc]
        private void StartFallingClientRpc()
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} started falling");
            locomotionManager.StartFalling();
        }

        //Land
        [ServerRpc(RequireOwnership = false)]
        public void LandServerRpc()
        {
            LandClientRpc();
        }

        [ClientRpc]
        private void LandClientRpc()
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} landed");
            locomotionManager.Land();
        }

        //Land
        [ServerRpc(RequireOwnership = false)]
        public void TurnInPlaceServerRpc(float angle)
        {
            TurnInPlaceClientRpc(angle);
        }

        [ClientRpc]
        private void TurnInPlaceClientRpc(float angle)
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} turned");
            locomotionManager.TurnInPlace(angle);
        }

        #endregion

        #region Combat

        //Two hand
        [ServerRpc(RequireOwnership = false)]
        public void TwoHandServerRpc(bool isLeft)
        {
            TwoHandClientRpc(isLeft);
        }

        [ClientRpc]
        private void TwoHandClientRpc(bool isLeft)
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} two handed");
            combatManager.TwoHand(isLeft);
        }

        //Stop Two handing
        [ServerRpc(RequireOwnership = false)]
        public void StopTwoHandingServerRpc()
        {
            StopTwoHandingClientRpc();
        }

        [ClientRpc]
        private void StopTwoHandingClientRpc()
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} stopped two handing");
            combatManager.StopTwoHanding();
        }

        #region Weapon

        //Attack
        [ServerRpc(RequireOwnership = false)]
        public void AttackServerRpc(AttackType newAttackType, bool isLeft)
        {
            AttackClientRpc(newAttackType, isLeft);
        }

        [ClientRpc]
        private void AttackClientRpc(AttackType newAttackType, bool isLeft)
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} attacked");
            weaponManager.Attack(newAttackType, isLeft);
        }

        //Backstab
        [ServerRpc(RequireOwnership = false)]
        public void RiposteServerRpc(AttackType newAttackType, bool attackingWithLeft)
        {
            RiposteClientRpc(newAttackType, attackingWithLeft);
        }

        [ClientRpc]
        private void RiposteClientRpc(AttackType newAttackType, bool attackingWithLeft)
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} backstabbed");
            weaponManager.Riposte(newAttackType, attackingWithLeft);
        }

        //Backstabbed
        [ServerRpc(RequireOwnership = false)]
        public void RipostedServerRpc(Vector3 riposteVictimPosition, Quaternion riposteVictimRotation, string riposteVictimAnimation, float damage, ulong id)
        {
            RipostedClientRpc(riposteVictimPosition, riposteVictimRotation, riposteVictimAnimation, damage, id);
        }

        [ClientRpc]
        private void RipostedClientRpc(Vector3 riposteVictimPosition, Quaternion riposteVictimRotation, string riposteVictimAnimation, float damage, ulong id)
        {
            //Sent to another player, it should not be the Owner of the object
            if (!playerManager.IsOwner || playerManager.OwnerClientId != id) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} got backstabbed");

            //Check if backstab actually occurred, else return
            weaponManager.Riposted(riposteVictimPosition, riposteVictimRotation, riposteVictimAnimation, damage);
        }

        //Parried
        [ServerRpc(RequireOwnership = false)]
        public void ParriedServerRpc()
        {
            ParriedClientRpc();
        }

        [ClientRpc]
        private void ParriedClientRpc()
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} got parried");
            weaponManager.Parried();
        }

        //Attack deflected
        [ServerRpc(RequireOwnership = false)]
        public void AttackDeflectedServerRpc(ulong id)
        {
            AttackDeflectedClientRpc(id);
        }

        [ClientRpc]
        private void AttackDeflectedClientRpc(ulong id)
        {
            //Sent to another player, it should not be the Owner of the object
            Debug.Log(playerManager.OwnerClientId);
            if (!playerManager.IsOwner || playerManager.OwnerClientId != id) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId}'s attack has been deflected");
            weaponManager.AttackDeflected();
        }

        //Wall bounce
        [ServerRpc(RequireOwnership = false)]
        public void WallBounceServerRpc()
        {
            WallBounceClientRpc();
        }

        [ClientRpc]
        private void WallBounceClientRpc()
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} bounced off a wall");
            weaponManager.WallBounce();
        }

        #endregion

        #region Shield
        //Block
        [ServerRpc(RequireOwnership = false)]
        public void BlockServerRpc(bool isLeft)
        {
            BlockClientRpc(isLeft);
        }

        [ClientRpc]
        private void BlockClientRpc(bool isLeft)
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} started blocking");
            shieldManager.Block(isLeft);
        }

        //Stop blocking
        [ServerRpc(RequireOwnership = false)]
        public void StopBlockingServerRpc()
        {
            StopBlockingClientRpc();
        }

        [ClientRpc]
        private void StopBlockingClientRpc()
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} stopped blocking");
            shieldManager.StopBlocking();
        }

        //Parry
        [ServerRpc(RequireOwnership = false)]
        public void ParryServerRpc(bool parryingWithLeft)
        {
            ParryClientRpc(parryingWithLeft);
        }

        [ClientRpc]
        private void ParryClientRpc(bool parryingWithLeft)
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} parried");
            shieldManager.Parry(parryingWithLeft);
        }

        //Shield broken
        [ServerRpc(RequireOwnership = false)]
        public void ShieldBrokenServerRpc()
        {
            ShieldBrokenClientRpc();
        }

        [ClientRpc]
        private void ShieldBrokenClientRpc()
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} shield guard has been broken");
            shieldManager.ShieldBroken();
        }
        #endregion

        #region Other
        //Player got hurt
        [ServerRpc(RequireOwnership = false)]
        public void PlayerHurtServerRpc(string hurtAnimation)
        {
            PlayerHurtClientRpc(hurtAnimation);
        }

        [ClientRpc]
        private void PlayerHurtClientRpc(string hurtAnimation)
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} got hurt");
            collisionManager.PlayerHurt(hurtAnimation);
        }
        #endregion

        #endregion

        #region Ragdoll

        //Knock out
        [ServerRpc(RequireOwnership = false)]
        public void KnockOutServerRpc(float time = 0)
        {
            KnockOutClientRpc(time);
        }

        [ClientRpc]
        private void KnockOutClientRpc(float time = 0)
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} got knocked out");
            ragdollManager.KnockOut(time);
        }

        //Wake up
        [ServerRpc(RequireOwnership = false)]
        public void WakeUpServerRpc(float time)
        {
            WakeUpClientRpc(time);
        }

        [ClientRpc]
        private void WakeUpClientRpc(float time)
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} woke up");
            ragdollManager.WakeUp(time);
        }

        #endregion

        #region Inventory
        //Change weapon
        [ServerRpc(RequireOwnership = false)]
        public void ChangeHandItemSlotServerRpc(bool leftHand)
        {
            ChangeHandItemSlotClientRpc(leftHand);
        }

        [ClientRpc]
        private void ChangeHandItemSlotClientRpc(bool leftHand)
        {
            if (IsOwner) return;
            if (showClientNetworkRpcs) Debug.Log($"Client {playerManager.OwnerClientId} changed weapon");
            inventoryManager.ChangeHandItemSlot(leftHand);
        }
        #endregion

        #endregion

    }
}