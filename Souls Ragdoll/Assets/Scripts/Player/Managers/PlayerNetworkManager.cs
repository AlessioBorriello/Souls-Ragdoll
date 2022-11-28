using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerNetworkManager : NetworkBehaviour
    {
        //Components
        private PlayerManager playerManager;
        private AnimationManager animationManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerLocomotionManager locomotionManager;
        private PlayerWeaponManager weaponManager;
        private PlayerShieldManager shieldManager;

        private Rigidbody physicalHips;
        private GameObject animatedPlayer;

        private NetworkVariable<Vector3> netPosition = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> netRotation = new(writePerm: NetworkVariableWritePermission.Owner);

        public NetworkVariable<float> netNormalMovementAmount = new(0, writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<float> netStrafeMovementAmount = new(0, writePerm: NetworkVariableWritePermission.Owner);

        //Stats
        public NetworkVariable<int> netCurrentHealth = new(0, writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<int> netMaxHealth = new(0, writePerm: NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            animationManager = playerManager.GetAnimationManager();
            ragdollManager = playerManager.GetRagdollManager();
            locomotionManager = playerManager.GetLocomotionManager();
            weaponManager = playerManager.GetWeaponManager();
            shieldManager = playerManager.GetShieldManager();

            physicalHips = playerManager.GetPhysicalHips();
            animatedPlayer = playerManager.GetAnimatedPlayer();
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
            }
            else
            {
                if(Vector3.Distance(physicalHips.transform.position, netPosition.Value) > 1.8f) physicalHips.transform.position = netPosition.Value;
                else physicalHips.transform.position = Vector3.SmoothDamp(physicalHips.transform.position, netPosition.Value, ref posVel, .1f);

                animatedPlayer.transform.rotation = Quaternion.Slerp(animatedPlayer.transform.rotation, netRotation.Value, rotVel);
            }

            animationManager.UpdateMovementAnimatorValues(netNormalMovementAmount.Value, netStrafeMovementAmount.Value, 0);
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
            locomotionManager.Land();
        }

        #endregion

        #region Combat

        #region Weapon

        //Attack
        [ServerRpc(RequireOwnership = false)]
        public void AttackServerRpc(string attackAnimation, int damage, float knockbackStrength, float flinchStrength, bool attackingWithLeft)
        {
            AttackClientRpc(attackAnimation, damage, knockbackStrength, flinchStrength, attackingWithLeft);
        }

        [ClientRpc]
        private void AttackClientRpc(string attackAnimation, int damage, float knockbackStrength, float flinchStrength, bool attackingWithLeft)
        {
            if (IsOwner) return;
            weaponManager.Attack(attackAnimation, damage, knockbackStrength, flinchStrength, attackingWithLeft);
        }

        //Backstab
        [ServerRpc(RequireOwnership = false)]
        public void BackstabServerRpc(string backstabAnimation, bool attackingWithLeft)
        {
            BackstabClientRpc(backstabAnimation, attackingWithLeft);
        }

        [ClientRpc]
        private void BackstabClientRpc(string backstabAnimation, bool attackingWithLeft)
        {
            if (IsOwner) return;
            weaponManager.Backstab(backstabAnimation, attackingWithLeft);
        }

        //Backstabbed
        [ServerRpc(RequireOwnership = false)]
        public void BackstabbedServerRpc(Vector3 backstabbedPosition, Quaternion backstabbedRotation, string backstabbedAnimation, float damage, ulong id)
        {
            BackstabbedClientRpc(backstabbedPosition, backstabbedRotation, backstabbedAnimation, damage, id);
        }

        [ClientRpc]
        private void BackstabbedClientRpc(Vector3 backstabbedPosition, Quaternion backstabbedRotation, string backstabbedAnimation, float damage, ulong id)
        {
            //Sent to another player, is it should not be the Owner of the object
            if (!playerManager.IsOwner || playerManager.OwnerClientId != id) return;

            //Check if backstab actually occurred, else return
            weaponManager.Backstabbed(backstabbedPosition, backstabbedRotation, backstabbedAnimation, damage);
        }

        #endregion

        #region Shield
        //Block
        [ServerRpc(RequireOwnership = false)]
        public void BlockServerRpc(bool blockingWithLeft)
        {
            BlockClientRpc(blockingWithLeft);
        }

        [ClientRpc]
        private void BlockClientRpc(bool blockingWithLeft)
        {
            if (IsOwner) return;
            shieldManager.Block(blockingWithLeft);
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
            shieldManager.StopBlocking();
        }
        #endregion

        #endregion

        #region Animation
        [ServerRpc(RequireOwnership = false)]
        public void PlayTargetAnimationServerRpc(string targetAnimation, float fadeDuration, bool isStuckInAnimation)
        {
            PlayTargetAnimationClientRpc(targetAnimation, fadeDuration, isStuckInAnimation);
        }

        [ClientRpc]
        private void PlayTargetAnimationClientRpc(string targetAnimation, float fadeDuration, bool isStuckInAnimation)
        {
            if (IsOwner) return;
            animationManager.PlayTargetAnimation(targetAnimation, fadeDuration, isStuckInAnimation);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayTargetAnimationServerRpc(string targetAnimation, float fadeDuration, bool isStuckInAnimation, int layer)
        {
            PlayTargetAnimationClientRpc(targetAnimation, fadeDuration, isStuckInAnimation, layer);
        }

        [ClientRpc]
        private void PlayTargetAnimationClientRpc(string targetAnimation, float fadeDuration, bool isStuckInAnimation, int layer)
        {
            if (IsOwner) return;
            animationManager.PlayTargetAnimation(targetAnimation, fadeDuration, isStuckInAnimation, layer);
        }
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
            ragdollManager.WakeUp(time);
        }

        #endregion

        #endregion

    }
}