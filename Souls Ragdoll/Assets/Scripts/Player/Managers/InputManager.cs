using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlessioBorriello
{

    public class InputManager : MonoBehaviour
    {
        private class Action
        {
            public string name;
            public float creationTime;

            public Action(string name)
            {
                this.name = name;
                this.creationTime = Time.time;
            }
        }

        private PlayerManager playerManager;
        private PlayerControls inputAction;

        private Stack<Action> actions = new Stack<Action>();
        public float queueTime = .2f;

        //Analogs
        [Header("Analogs")]
        public Vector2 movementInput;
        public Vector2 cameraInput;

        [Header("D-Pad")]
        public Vector2 dPadInput;

        #region Sticks
        [Header("Right stick")]
        public bool rightStickInput;
        public bool rightStickInputReleased;
        public bool rightStickInputPressed;
        #endregion

        #region Buttons
        [Header("Buttons - east")]
        public bool eastInput;
        public bool eastInputReleased;
        public bool eastInputPressed;

        [Header("Buttons - south")]
        public bool southInput;
        public bool southInputReleased;
        public bool southInputPressed;
        #endregion

        #region Triggers - Bumpers
        [Header("Bumpers - right")]
        public bool rbInput;
        public bool rbInputReleased;
        public bool rbInputPressed;

        [Header("Triggers - right")]
        public bool rtInput;
        public bool rtInputReleased;
        public bool rtInputPressed;

        [Header("Bumpers - left")]
        public bool lbInput;
        public bool lbInputReleased;
        public bool lbInputPressed;

        [Header("Triggers - left")]
        public bool ltInput;
        public bool ltInputReleased;
        public bool ltInputPressed;
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
                inputAction.PlayerGameplay.Movement.performed += inputAction => movementInput = inputAction.ReadValue<Vector2>();

                //Camera movement
                inputAction.PlayerGameplay.CameraMovement.performed += inputAction => cameraInput = inputAction.ReadValue<Vector2>().normalized;

                #region Sticks
                //Right stick
                inputAction.PlayerGameplay.LockOn.started += inputAction =>
                {
                    rightStickInput = true;
                    actions.Push(new Action("RightStickPressed"));
                };
                inputAction.PlayerGameplay.LockOn.canceled += inputAction =>
                {
                    rightStickInput = false;
                    actions.Push(new Action("RightStickReleased"));
                };
                #endregion

                #region Buttons
                //East button
                inputAction.PlayerGameplay.EastButton.started += inputAction =>
                {
                    eastInput = true;
                    actions.Push(new Action("EastButtonPressed"));
                };
                inputAction.PlayerGameplay.EastButton.canceled += inputAction =>
                {
                    eastInput = false;
                    actions.Push(new Action("EastButtonReleased"));
                };

                //South button
                inputAction.PlayerGameplay.SouthButton.started += inputAction =>
                {
                    southInput = true;
                    actions.Push(new Action("SouthButtonPressed"));
                };
                inputAction.PlayerGameplay.SouthButton.canceled += inputAction =>
                {
                    southInput = false;
                    actions.Push(new Action("SouthButtonReleased"));
                };
                #endregion

                #region Bumpers and Triggers
                //Right bumper
                inputAction.PlayerGameplay.RightLightButton.started += inputAction =>
                {
                    rbInput = true;
                    actions.Push(new Action("RightLightButtonPressed"));
                };
                inputAction.PlayerGameplay.RightLightButton.canceled += inputAction =>
                {
                    rbInput = false;
                    actions.Push(new Action("RightLightButtonReleased"));
                };

                //Left bumper
                inputAction.PlayerGameplay.LeftLightButton.started += inputAction =>
                {
                    lbInput = true;
                    actions.Push(new Action("LeftLightButtonPressed"));
                };
                inputAction.PlayerGameplay.LeftLightButton.canceled += inputAction =>
                {
                    lbInput = false;
                    actions.Push(new Action("LeftLightButtonReleased"));
                };

                //Right trigger
                inputAction.PlayerGameplay.RightHeavyButton.started += inputAction =>
                {
                    rtInput = true;
                    actions.Push(new Action("RightHeavyButtonPressed"));
                };
                inputAction.PlayerGameplay.RightHeavyButton.canceled += inputAction =>
                {
                    rtInput = false;
                    actions.Push(new Action("RightHeavyButtonReleased"));
                };

                //Left trigger
                inputAction.PlayerGameplay.LeftHeavyButton.started += inputAction =>
                {
                    ltInput = true;
                    actions.Push(new Action("LeftHeavyButtonPressed"));
                };
                inputAction.PlayerGameplay.LeftHeavyButton.canceled += inputAction =>
                {
                    ltInput = false;
                    actions.Push(new Action("LeftHeavyButtonReleased"));
                };
                #endregion

                #region Dpad
                inputAction.PlayerGameplay.DPadUp.started += inputAction => actions.Push(new Action("DPadUpPressed"));
                inputAction.PlayerGameplay.DPadDown.started += inputAction => actions.Push(new Action("DPadDownPressed"));
                inputAction.PlayerGameplay.DPadRight.started += inputAction => actions.Push(new Action("DPadLeftPressed"));
                inputAction.PlayerGameplay.DPadLeft.started += inputAction => actions.Push(new Action("DPadRightPressed"));
                #endregion

            }

            inputAction.Enable();

        }

        private void Update()
        {
            if (!playerManager.isClient) return;

            if (actions.Count > 0 && !playerManager.playerIsStuckInAnimation && playerManager.consumeInputs) ConsumeInputs(actions);
        }

        private void LateUpdate()
        {
            //Reset flags
            ResetInputBooleans();
        }

        private void OnDisable()
        {
            inputAction.Disable();
        }

        private void ConsumeInputs(Stack<Action> actions)
        {
            //While there are still actions to process
            while(actions.Any())
            {
                //Get action
                Action action = actions.Pop();
                //If the action is not too old
                if(action.creationTime > Time.time - queueTime)
                {
                    ProcessInput(action.name);
                }
            }
        }

        private void ProcessInput(string inputName)
        {
            switch (inputName)
            {
                #region Sticks
                //Right stick
                case "RightStickPressed": rightStickInputPressed = true; break;
                case "RightStickReleased": rightStickInputReleased = true; break;
                #endregion

                #region Buttons
                //East button
                case "EastButtonPressed": eastInputPressed = true; break;
                case "EastButtonReleased": eastInputReleased = true; break;

                //South button
                case "SouthButtonPressed": southInputPressed = true; break;
                case "SouthButtonReleased": southInputReleased = true; break;
                #endregion

                #region Bumpers and Triggers
                //Right bumper
                case "RightLightButtonPressed": rbInputPressed = true; break;
                case "RightLightButtonReleased": rbInputReleased = true; break;

                //Left bumper
                case "LeftLightButtonPressed": lbInputPressed = true; break;
                case "LeftLightButtonReleased": lbInputReleased = true; break;

                //Right trigger
                case "RightHeavyButtonPressed": rtInputPressed = true; break;
                case "RightHeavyButtonReleased": rtInputReleased = true; break;

                //Left trigger
                case "LeftHeavyButtonPressed": ltInputPressed = true; break;
                case "LeftHeavyButtonReleased": ltInputReleased = true; break;
                #endregion

                #region DPad
                case "DPadUpPressed": dPadInput = new Vector2(0, -1); break;
                case "DPadDownPressed": dPadInput = new Vector2(0, 1); break;
                case "DPadRightPressed": dPadInput = new Vector2(-1, 0); break;
                case "DPadLeftPressed": dPadInput = new Vector2(1, 0); break;
                #endregion

                default: break;
            }
        }

        private void ResetInputBooleans()
        {
            #region Sticks
            rightStickInputPressed = false;
            rightStickInputReleased = false;
            #endregion
            
            #region Buttons
            //East button
            eastInputPressed = false;
            eastInputReleased = false;

            //South button
            southInputPressed = false;
            southInputReleased = false;
            #endregion

            #region Bumpers and Triggers
            //Right bumper
            rbInputPressed = false;
            rbInputReleased = false;

            //Left bumper
            lbInputPressed = false;
            lbInputReleased = false;

            //Right trigger
            rtInputPressed = false;
            rtInputReleased = false;

            //Left trigger
            ltInputPressed = false;
            ltInputReleased = false;
            #endregion

            #region DPad
            dPadInput = Vector2.zero;
            #endregion
        }

    }

}
