using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Windows;

namespace AlessioBorriello
{
    public class PlayerManager : CharacterManager
    {
        [Header("Set up")]
        public PlayerData playerData; //Player data reference
        public Transform groundCheckTransform; //Player's ground check's transform
        public GameObject ragdoll; //Player ragdoll

        private GameObject animatedPlayer; //Animated player reference
        private GameObject physicalPlayer; //Physical player reference
        private Rigidbody physicalHips; //Player's physical hips
        private Transform animatedHips; //Player's animated hips

        //Components
        private Transform cameraTransform;
        private InputManager inputManager;
        private PlayerNetworkManager networkManager;
        private AnimationManager animationManager;
        private AnimationEventsManager animationEventsManager;
        private PlayerLocomotionManager locomotionManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerInventoryManager inventoryManager;
        private PlayerCombatManager combatManager;
        private PlayerStatsManager statsManager;
        private PlayerCollisionManager collisionManager;
        private PlayerWeaponManager weaponManager;
        private PlayerShieldManager shieldManager;
        private PlayerAnimationsDatabase animationsDatabase;

        private UIManager uiManager;

        private CameraControl cameraControl;

        [HideInInspector] public Transform lockedTarget;

        #region Flags
        [Header("General flags")]
        public bool isClient = true;
        public bool isStuckInAnimation = false; //Disables actions like dodging, attacking... and movement
        public bool disableActions = false; //If the player can perform actions like dodging, attacking, blocking... (Indipendent from movement)
        public bool consumeInputs = true;
        public bool isOnGround = true;
        public bool isKnockedOut = false;
        public bool isDead = false;
        public bool isInOverrideAnimation = false;

        [Header("Locomotion flags")]
        public bool canRotate = true;
        public bool shouldSlide = false; //If the friction should be enabled or not

        //Is rolling
        [SerializeField] private bool isRolling = false;
        public bool IsRolling { get { return isRolling; } set { isRolling = value; } }
        public bool isBackdashing = false;
        public bool isSprinting = false;
        public bool isFalling = false;
        public bool disableSprint = false; //Disables sprint when going to 0 stamina

        [Header("Combat flag")]
        public bool areIFramesActive = false;
        public bool isAttacking = false;
        public bool isBlocking = false;
        public bool isParrying = false;
        public bool canBlock = true;
        public bool canBeBackstabbed = true;
        public bool canBeRiposted = false;
        public bool isLockingOn = false;
        public bool canLockOn = true;
        #endregion

        private void Awake()
        {
            inputManager = GetComponent<InputManager>();
            networkManager = GetComponent<PlayerNetworkManager>();
            animationManager = GetComponent<AnimationManager>();
            animationEventsManager = GetComponent<AnimationEventsManager>();
            animationsDatabase = GetComponent<PlayerAnimationsDatabase>();
            ragdollManager = GetComponent<ActiveRagdollManager>();
            collisionManager = GetComponent<PlayerCollisionManager>();
            locomotionManager = GetComponent<PlayerLocomotionManager>();
            inventoryManager = GetComponent<PlayerInventoryManager>();
            statsManager = GetComponent<PlayerStatsManager>();
            combatManager = GetComponent<PlayerCombatManager>();
            weaponManager = GetComponent<PlayerWeaponManager>();
            shieldManager = GetComponent<PlayerShieldManager>();

            uiManager = FindObjectOfType<UIManager>();

            animatedPlayer = transform.Find("AnimatedPlayer").gameObject;
            physicalPlayer = transform.Find("PhysicalPlayer").gameObject;
            physicalHips = transform.Find("PhysicalPlayer/Armature/Hip").GetComponent<Rigidbody>();
            animatedHips = transform.Find("AnimatedPlayer/Armature/Hip");

            cameraTransform = Camera.main.transform;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                inputManager.enabled = false;
                animationManager.GetAnimator().applyRootMotion = false;
                isClient = false;

                foreach(KnockOutResistance r in GetComponentsInChildren<KnockOutResistance>())
                {
                    Destroy(r);
                }
            }
            else
            {
                SpawnSetup();
            }
        }

        private void FixedUpdate()
        {
            if (isDead || !IsOwner) return;

            //Move player with animation (Order is important for some reason)
            locomotionManager.ApplyGravity();
            locomotionManager.MovePlayerWithAnimation();

        }

        private void Update()
        {
            if (isDead || !IsOwner) return;

            //Ragdoll
            ragdollManager.HandleWakeUp();

            //Locomotion
            locomotionManager.HandleLocomotion();

            //Combat
            combatManager.HandleCombat();

            //QuickSlots
            inventoryManager.HandleQuickSlots();

            //Reset inputs
            inputManager.ResetAllInputValues();

        }

        private void SpawnSetup()
        {
            //Set as client
            isClient = true;
            //Set up camera
            SetUpCamera();
            //Set up UI
            uiManager.SetPlayerStatsManager(statsManager);

            //Position
            Transform spawnPoint = FindObjectOfType<GameControl>().GetSpawnPoint();
            physicalHips.transform.position = spawnPoint.position;
            animatedPlayer.transform.rotation = spawnPoint.rotation;
        }

        private void SetUpCamera()
        {
            if (!IsOwner) return;

            cameraControl = cameraTransform.GetComponentInParent<CameraControl>();

            cameraControl.SetCameraPlayerManager(this);
            cameraControl.SetCameraInputManager(inputManager);

            cameraControl.SetCameraPhysicalHips(physicalHips);
            cameraControl.SetCameraFollowTransform(physicalHips.transform);
        }

        public void Die()
        {
            isDead = true;
            ragdollManager.SetJointsDriveForces(0, 0);

            //Fade animations
            animationManager.FadeOutOverrideAnimation(.1f);
            animationManager.FadeOutOverrideAnimation(.1f, OverrideLayers.leftArmLayer);
            animationManager.FadeOutOverrideAnimation(.1f, OverrideLayers.rightArmLayer);
            animationManager.FadeOutOverrideAnimation(.1f, OverrideLayers.bothArmsLayer);

            //Changes friction of the feet so that they don't slide around (set it to idle friction)
            shouldSlide = false;

            //Remove target if it was the target of the player
            if (!IsOwner)
            {
                CameraControl cameraControl = Camera.main.transform.GetComponentInParent<CameraControl>();
                Transform lockedTarget = cameraControl.lockedTarget;
                if (lockedTarget != null && lockedTarget.root.GetInstanceID() == transform.root.GetInstanceID()) cameraControl.TargetDied();
            }

            //Respawn
            if (IsOwner) StartCoroutine(RespawnPlayer());
            else networkManager.RespawnPlayerServerRpc();
        }

        public IEnumerator RespawnPlayer(float respawnTime = 1.5f)
        {
            yield return new WaitForSeconds(respawnTime);

            isDead = false;
            SpawnCorpse();

            if (IsOwner)
            {
                GameControl gameControl = FindObjectOfType<GameControl>();
                Transform spawnPoint = gameControl.GetSpawnPoint();

                physicalHips.transform.position = spawnPoint.position;

                statsManager.ResetStats();
                ragdollManager.WakeUp(0);
                networkManager.WakeUpServerRpc(0);

                cameraControl.SetCameraToTarget();
            }
        }

        private void SpawnCorpse()
        {
            //Spawn corpse
            GameObject corpse = Instantiate(ragdoll, physicalHips.transform.position, animatedPlayer.transform.rotation);
            corpse.GetComponent<Corpse>().SetUp(GetComponentInChildren<PlayerColorManager>().GetPlayerColor(), ragdollManager.GetRigidbodies());
        }

        #region Getters
        public InputManager GetInputManager()
        {
            return inputManager;
        }

        public PlayerNetworkManager GetNetworkManager()
        {
            return networkManager;
        }

        public AnimationManager GetAnimationManager()
        {
            return animationManager;
        }

        public AnimationEventsManager GetAnimationEventsManager()
        {
            return animationEventsManager;
        }

        public PlayerAnimationsDatabase GetAnimationDatabase()
        {
            return animationsDatabase;
        }

        public PlayerLocomotionManager GetLocomotionManager()
        {
            return locomotionManager;
        }

        public PlayerInventoryManager GetInventoryManager()
        {
            return inventoryManager;
        }

        public ActiveRagdollManager GetRagdollManager()
        {
            return ragdollManager;
        }

        public PlayerCombatManager GetCombatManager()
        {
            return combatManager;
        }

        public PlayerWeaponManager GetWeaponManager()
        {
            return weaponManager;
        }

        public PlayerShieldManager GetShieldManager()
        {
            return shieldManager;
        }

        public PlayerStatsManager GetStatsManager()
        {
            return statsManager;
        }

        public PlayerCollisionManager GetCollisionManager()
        {
            return collisionManager;
        }

        public UIManager GetUiManager()
        {
            return uiManager;
        }

        public Transform GetCameraTransform()
        {
            return cameraTransform;
        }

        public GameObject GetAnimatedPlayer()
        {
            return animatedPlayer;
        }

        public GameObject GetPhysicalPlayer()
        {
            return physicalPlayer;
        }

        public Rigidbody GetPhysicalHips()
        {
            return physicalHips;
        }

        public Transform GetAnimatedHips()
        {
            return animatedHips;
        }
        #endregion

    }
}
