using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlessioBorriello
{

    public class InputManager : MonoBehaviour
    {

        //Note, inputs variables ending in "In" are local and private
        //Thos ending in "Input" are instead those readable from the outside
        private PlayerManager playerManager;
        private PlayerControls inputAction;

        //Analogs
        [Header("Analogs")]
        public Vector2 movementInput;
        public Vector2 cameraInput;
        private Vector2 movementIn;
        private Vector2 cameraIn;

        [Header("D-Pad")]
        public Vector2 dPadInput;
        private Vector2 dPadIn;

        #region Sticks
        private bool rightStickIn;
        private bool rightStickInReleased;
        private bool rightStickInPressed;

        [Header("Right stick")]
        public bool rightStickInput;
        public bool rightStickInputReleased;
        public bool rightStickInputPressed;
        #endregion

        #region Buttons
        #region East button
        private bool eastIn;
        private bool eastInReleased;
        private bool eastInPressed;

        [Header("Buttons - east")]
        public bool eastInput;
        public bool eastInputReleased;
        public bool eastInputPressed;
        #endregion

        #region South button
        private bool southIn;
        private bool southInReleased;
        private bool southInPressed;

        [Header("Buttons - south")]
        public bool southInput;
        public bool southInputReleased;
        public bool southInputPressed;
        #endregion
        #endregion

        #region Triggers - Bumpers
        #region RB
        private bool rbIn;
        private bool rbInReleased;
        private bool rbInPressed;

        [Header("Bumpers - right")]
        public bool rbInput;
        public bool rbInputReleased;
        public bool rbInputPressed;
        #endregion
        
        #region RT
        private bool rtIn;
        private bool rtInReleased;
        private bool rtInPressed;

        [Header("Triggers - right")]
        public bool rtInput;
        public bool rtInputReleased;
        public bool rtInputPressed;
        #endregion

        #region LB
        private bool lbIn;
        private bool lbInReleased;
        private bool lbInPressed;

        [Header("Bumpers - left")]
        public bool lbInput;
        public bool lbInputReleased;
        public bool lbInputPressed;
        #endregion

        #region LT
        private bool ltIn;
        private bool ltInReleased;
        private bool ltInPressed;

        [Header("Triggers - left")]
        public bool ltInput;
        public bool ltInputReleased;
        public bool ltInputPressed;
        #endregion
        #endregion

        private void Start()
        {
            if (!playerManager.isClient) this.enabled = false;
        }

        public void OnEnable()
        {

            playerManager = GetComponent<PlayerManager>();

            if (inputAction == null)
            {

                inputAction = new PlayerControls();

                //Movement
                inputAction.PlayerGameplay.Movement.performed += inputAction => movementIn = inputAction.ReadValue<Vector2>();

                //Camera movement
                inputAction.PlayerGameplay.CameraMovement.performed += inputAction => cameraIn = inputAction.ReadValue<Vector2>().normalized;

            }

            inputAction.Enable();

        }

        private void OnDisable()
        {
            inputAction.Disable();
        }

        public void TickMovementInput()
        {
            if (inputAction == null) return;

            movementInput = movementIn;
        }

        public void TickCameraMovementInput()
        {
            if (inputAction == null) return;

            cameraInput = cameraIn;
        }

        public void TickDPadInput()
        {
            if (inputAction == null) return;

            int up = (inputAction.PlayerGameplay.DPadUp.WasPerformedThisFrame())? 1 : 0;
            int down = (inputAction.PlayerGameplay.DPadDown.WasPerformedThisFrame())? 1 : 0;
            int left = (inputAction.PlayerGameplay.DPadLeft.WasPerformedThisFrame())? 1 : 0;
            int right = (inputAction.PlayerGameplay.DPadRight.WasPerformedThisFrame())? 1 : 0;

            dPadIn = new Vector2(right - left, up - down);

            dPadInput = dPadIn;
        }

        public void TickActionsInput()
        {
            if (inputAction == null) return;

            #region Sticks
            rightStickIn = (inputAction.PlayerGameplay.LockOn.phase == InputActionPhase.Performed);
            rightStickInPressed = inputAction.PlayerGameplay.LockOn.WasPerformedThisFrame();
            rightStickInReleased = inputAction.PlayerGameplay.LockOn.WasReleasedThisFrame();

            rightStickInput = rightStickIn;
            rightStickInputReleased = rightStickInReleased;
            rightStickInputPressed = rightStickInPressed;
            #endregion

            #region Buttons
            #region East button
            eastIn = (inputAction.PlayerGameplay.EastButton.phase == InputActionPhase.Performed);
            eastInPressed = inputAction.PlayerGameplay.EastButton.WasPerformedThisFrame();
            eastInReleased = inputAction.PlayerGameplay.EastButton.WasReleasedThisFrame();

            eastInput = eastIn;
            eastInputReleased = eastInReleased;
            eastInputPressed = eastInPressed;
            #endregion

            #region South button
            southIn = (inputAction.PlayerGameplay.SouthButton.phase == InputActionPhase.Performed);
            southInPressed = inputAction.PlayerGameplay.SouthButton.WasPerformedThisFrame();
            southInReleased = inputAction.PlayerGameplay.SouthButton.WasReleasedThisFrame();

            southInput = southIn;
            southInputReleased = southInReleased;
            southInputPressed = southInPressed;
            #endregion
            #endregion

            #region Triggers - Bumpers
            #region RB
            rbIn = (inputAction.PlayerGameplay.RightLightButton.phase == InputActionPhase.Performed);
            rbInPressed = inputAction.PlayerGameplay.RightLightButton.WasPerformedThisFrame();
            rbInReleased = inputAction.PlayerGameplay.RightLightButton.WasReleasedThisFrame();

            rbInput = rbIn;
            rbInputReleased = rbInReleased;
            rbInputPressed = rbInPressed;
            #endregion
            
            #region RT
            rtIn = (inputAction.PlayerGameplay.RightHeavyButton.phase == InputActionPhase.Performed);
            rtInPressed = inputAction.PlayerGameplay.RightHeavyButton.WasPerformedThisFrame();
            rtInReleased = inputAction.PlayerGameplay.RightHeavyButton.WasReleasedThisFrame();

            rtInput = rtIn;
            rtInputReleased = rtInReleased;
            rtInputPressed = rtInPressed;
            #endregion

            #region LB
            lbIn = (inputAction.PlayerGameplay.LeftLightButton.phase == InputActionPhase.Performed);
            lbInPressed = inputAction.PlayerGameplay.LeftLightButton.WasPerformedThisFrame();
            lbInReleased = inputAction.PlayerGameplay.LeftLightButton.WasReleasedThisFrame();

            lbInput = lbIn;
            lbInputReleased = lbInReleased;
            lbInputPressed = lbInPressed;
            #endregion

            #region LT
            ltIn = (inputAction.PlayerGameplay.LeftHeavyButton.phase == InputActionPhase.Performed);
            ltInPressed = inputAction.PlayerGameplay.LeftHeavyButton.WasPerformedThisFrame();
            ltInReleased = inputAction.PlayerGameplay.LeftHeavyButton.WasReleasedThisFrame();

            ltInput = ltIn;
            ltInputReleased = ltInReleased;
            ltInputPressed = ltInPressed;
            #endregion
            #endregion
        }

    }

}
