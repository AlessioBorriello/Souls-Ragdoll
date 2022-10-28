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
        private Vector2 movementIn;
        private Vector2 cameraIn;
        [Header("Analogs")]
        public Vector2 movementInput;
        public Vector2 cameraInput;

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
        #endregion

        public void OnEnable()
        {

            playerManager = GetComponent<PlayerManager>();

            if (inputAction == null)
            {

                inputAction = new PlayerControls();
                //Movement
                inputAction.PlayerGameplay.Movement.performed += inputAction => movementIn = inputAction.ReadValue<Vector2>();

                //Camera movement
                inputAction.PlayerGameplay.CameraMovement.performed += inputAction => cameraIn = inputAction.ReadValue<Vector2>();

                #region Buttons
                #region East button
                inputAction.PlayerGameplay.EastButton.started += inputAction => eastIn = true;
                inputAction.PlayerGameplay.EastButton.canceled += inputAction =>
                {
                    eastIn = false;
                    eastInReleased = true;
                };
                #endregion

                #region South button
                inputAction.PlayerGameplay.SouthButton.started += inputAction => southIn = true;
                inputAction.PlayerGameplay.SouthButton.canceled += inputAction =>
                {
                    southIn = false;
                    southInReleased = true;
                };
                #endregion
                #endregion

                #region Triggers - Bumpers
                #region RB
                inputAction.PlayerGameplay.EastButton.started += inputAction => rbIn = true;
                inputAction.PlayerGameplay.EastButton.canceled += inputAction =>
                {
                    rbIn = false;
                    rbInReleased = true;
                };
                #endregion
                
                #region RT
                inputAction.PlayerGameplay.EastButton.started += inputAction => rtIn = true;
                inputAction.PlayerGameplay.EastButton.canceled += inputAction =>
                {
                    rtIn = false;
                    rtInReleased = true;
                };
                #endregion
                #endregion

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

        public void TickActionsInput()
        {
            if (inputAction == null) return;

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
            #endregion
        }

    }

}
