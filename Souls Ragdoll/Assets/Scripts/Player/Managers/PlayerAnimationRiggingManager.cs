using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace AlessioBorriello
{
    public class PlayerAnimationRiggingManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private PlayerNetworkManager networkManager;
        private Rigidbody physicalHips;

        [Header("Components")]
        [SerializeField] private Transform headTorsoTargetPivotTransform;
        [SerializeField] private Transform headTorsoTargetTransform;
        [SerializeField] private MultiAimConstraint headMultiAim;
        [SerializeField] private MultiAimConstraint torsoMultiAim;

        [Header("Options")]
        [SerializeField] private float headTorsoTargetDistance = 10f;
        [SerializeField] private float weightSlerpSpeed = 5f;
        [SerializeField] private float horizontalAngleLimit = 70f;
        [SerializeField] private float verticalAngleLimit = 4f;

        private Transform cameraTransform;
        private Vector3 cameraForward;

        private void Awake()
        {
            playerManager = GetComponentInParent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
            physicalHips = playerManager.GetPhysicalHips();

            headTorsoTargetTransform.localPosition = new Vector3(0, 0, headTorsoTargetDistance);

            cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            //Lerp rigging weight to 0 if the target is behind the player
            HandleRiggingWeights();

            //Rotate target in the camera direction
            HandleRotation();
        }

        private void HandleRotation()
        {
            if (playerManager.IsOwner) cameraForward = cameraTransform.forward;
            else cameraForward = networkManager.netCameraForward.Value;

            if (cameraForward == Vector3.zero) cameraForward = Vector3.forward;

            Vector3 cameraRotationEuler = Quaternion.LookRotation(cameraForward).eulerAngles;
            cameraRotationEuler.x = (cameraRotationEuler.x > 180)? cameraRotationEuler.x - 360 : cameraRotationEuler.x;
            //Debug.Log(cameraRotationEuler);
            cameraRotationEuler.x = Mathf.Clamp(cameraRotationEuler.x, -verticalAngleLimit, verticalAngleLimit);

            //headTorsoTargetPivotTransform.rotation = Quaternion.Slerp(headTorsoTargetTransform.rotation, cameraTransform.rotation, headTorsoTargetSpeed * Time.deltaTime);
            headTorsoTargetPivotTransform.rotation = Quaternion.Euler(cameraRotationEuler);
        }

        private void HandleRiggingWeights()
        {
            Vector3 targetDirection = (headTorsoTargetTransform.position - physicalHips.transform.position).normalized;
            float targetLookingAngle = Vector3.Angle(Vector3.ProjectOnPlane(physicalHips.transform.forward, Vector3.up), Vector3.ProjectOnPlane(targetDirection, Vector3.up));
            if (targetLookingAngle > horizontalAngleLimit) LerpWeight(0);
            else LerpWeight(1);
        }

        private void LerpWeight(float targetValue)
        {
            if (headMultiAim.weight != targetValue) headMultiAim.weight = Mathf.Lerp(headMultiAim.weight, targetValue, weightSlerpSpeed * Time.deltaTime);
            if (torsoMultiAim.weight != targetValue) torsoMultiAim.weight = Mathf.Lerp(torsoMultiAim.weight, targetValue, weightSlerpSpeed * Time.deltaTime);
        }
    }
}