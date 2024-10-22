using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;
using UnityEngine.Assertions.Must;
using Cinemachine;
using UnityEditor;
using UnityEngine.UIElements;


namespace CharacterSystem
{
    public class PlayerController : MonoBehaviour, ICharacterController
    {

        [Header("Assets/objects/components")]
        public KinematicCharacterMotor Motor;
        public ConfigAssetType config;
        public GameObject CinemachineCameraTarget;

        [Header("Upgrades")]
        public Upgrades MyUpgrades = new Upgrades(false, 0);

        [Header("Player State")]
        public Quaternion yawRotation;
        public float pitchRotation = 0;
        public bool Sprinting = false;
        public byte WaterState = 0;
        public byte HotState = 0;
        public byte ColdState = 0;
        public byte ClimbState = 0;
        public float BaseSpeed;
        public float MoveSharpness;
        public float BaseAirSpeed;
        public float AirAccel;
        public float AirDrag;
        public float JumpUpSpeed;
        public float JumpUpMul;
        public Vector3 Gravity;
        public float Buoyancy;
        public float TerminalVelocity;
        public float AscendVel = 0;
        public float AscendAccel = 0; //upwards acceleration while holding jump button- used for swimming
        public bool Crouching = false;
        public Vector3 RespawnPos;
        public float RespawnDeltaTime = 0f;


        public enum MovementStates
        {
            Normal,
            Dash,
            Climb
        }
        public MovementStates MovementState;

        private uint _waterTriggerCount = 0;
        private uint _hotTriggerCount = 0;
        private uint _coldTriggerCount = 0;
        private uint _climbTriggerCount = 0;
        private float _finalSpeed;
        private float _finalAirSpeed;
        private bool _jumpConsumed = false;

        private PlayerInput _playerInput;
        private InputHandler _input;

        private readonly Collider[] _probedColliders = new Collider[8];
        private Vector3 _moveInputVector;
        private Vector3 _dashInputVec;
        private bool _jumpRequested = false;

        private bool _jumpedThisFrame = false;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        private Vector3 _internalVelocityAdd = Vector3.zero;
        private bool _shouldBeCrouching = false;

        private float _dashTimer = 0f;
        private bool _dashReady = true;
        private float _rotationVelocity;
        private bool _jumpReleased = false;
        private bool IsCurrentDeviceMouse
        {
            get
            {
                return _playerInput.currentControlScheme == "KeyboardMouse";

            }
        }
        private float _crouchLerpTime = 0;

        public UnityEngine.UI.Image TempBlackoutUI;

        private void Awake()
        {
            yawRotation = this.transform.rotation;
            // Handle initial state
            SetMovementState(MovementStates.Normal);

            // Assign the characterController to the motor
            Motor.CharacterController = this;

            // get a reference to our main camera
            BaseSpeed = config.BaseSpeed;
            _finalSpeed = BaseSpeed;
            MoveSharpness = config.MovementSharpness;
            BaseAirSpeed = config.BaseAirSpeed;
            AirAccel = config.AirAccel;
            AirDrag = config.AirDrag;
            JumpUpSpeed = config.JumpUpSpeed;
            Gravity = Vector3.down * config.DefaultGravityStrength;
            TerminalVelocity = config.BaseTerminalVelocity;

        }
        private void Start()
        {
            _input = GetComponent<InputHandler>();
            _playerInput = GetComponent<PlayerInput>();
        }

        /// <summary>
        /// Handles movement state transitions and enter/exit callbacks
        /// </summary>
        public void SetMovementState(MovementStates newState)
        {

            //When exiting a movement state
            switch (MovementState)
            {
                case MovementStates.Normal:
                    {
                        break;
                    }
                case MovementStates.Dash:
                    {
                        _dashTimer = 0;
                        Motor.AllowSteppingWithoutStableGrounding = false;
                        break;
                    }
            }

            
            //When entering a movement state
            switch (newState)
            {
                case MovementStates.Normal:
                    {
                        break;
                    }
                case MovementStates.Dash:
                    {
                        _dashTimer = 0;

                        _dashInputVec = (_moveInputVector.magnitude > 0.05f) ? Vector3.ProjectOnPlane(_moveInputVector.normalized, Motor.CharacterUp) : Motor.CharacterForward;
                        _dashReady = false;
                        Motor.AllowSteppingWithoutStableGrounding = true;
                        break;
                    }
            }
            MovementState = newState;

        }


        /// <summary>
        /// This is called every frame, determines input and player state
        /// </summary>
        private void Update()
        {


            Color col = TempBlackoutUI.color;
            col.a = Mathf.InverseLerp(config.RespawnTimerLength, 0, RespawnDeltaTime);
            TempBlackoutUI.color = col;

            UpdateZoneState();

            // Respawn handling 

            if (Motor.GroundingStatus.IsStableOnGround && !Motor.GroundingStatus.GroundCollider.CompareTag("NoRespawn"))
            {
                if ((WaterState == 0 || MyUpgrades.Swim) && (HotState == 0 || MyUpgrades.Heat) && (ColdState == 0 || MyUpgrades.Cold))
                {
                    RespawnPos = transform.position;
                }
            }
            if ((WaterState == 2 && !MyUpgrades.Swim) || (HotState == 2 && !MyUpgrades.Heat) || (ColdState == 2 && !MyUpgrades.Cold))
            {
                if (RespawnDeltaTime > 0)
                {
                    RespawnDeltaTime -= Time.deltaTime;
                } else
                {
                    RespawnDeltaTime = config.RespawnTimerLength;
                    Teleport(RespawnPos);
                }
            } else
            {
                RespawnDeltaTime = config.RespawnTimerLength;
            }

            // Clamp input
            Vector3 inputVector = Vector3.ClampMagnitude(new Vector3(_input.move.x, 0, _input.move.y), 1f);

            // Calculate camera direction and rotation on the character plane

            // Move and look inputs
            _moveInputVector = yawRotation * inputVector;
            //Character state updates
            if (MyUpgrades.Glide && !_jumpReleased && !_input.jump) { _jumpReleased = true; }
            switch (MovementState)
            {
                case MovementStates.Normal:
                    {
                        // Request jump if jump input
                        if (_input.jump)
                        {
                            _timeSinceJumpRequested = 0f;
                            _jumpRequested = true;
                        }

                        //dash
                        if (MyUpgrades.Dash && _input.dash && _dashReady)
                        {
                            SetMovementState(MovementStates.Dash);
                        }
                        if (!_dashReady)
                        {
                            _dashTimer += Time.deltaTime;
                            if (_dashTimer > config.DashCooldownLength)
                            {
                                _dashReady = true;
                            }
                        }
                        if (WaterState == 0 && ClimbState == 0 && MyUpgrades.Glide)
                        {
                            if (_input.jump && _jumpReleased)
                            {
                                TerminalVelocity = config.GlideTerminalVelocity;
                            } else
                            {
                                TerminalVelocity = config.BaseTerminalVelocity;
                            }
                        }

                        _shouldBeCrouching = _input.crouch;

                        if (!Crouching && _shouldBeCrouching)
                        {
                            Crouching = true;
                            if (Motor.GroundingStatus.IsStableOnGround)
                            {
                                Sprinting = false;
                            }
                            Motor.SetCapsuleDimensions(0.5f, config.CrouchedCapsuleHeight, config.CrouchedCapsuleHeight * 0.5f);
                        }
                        if (_input.sprint)
                        {
                            if (Motor.GroundingStatus.IsStableOnGround)
                            {
                                Sprinting = !Crouching;
                            }
                        }
                        else
                        {
                            Sprinting = false;
                        }

                        //Determine final speed
                        if (Sprinting)
                        {
                            _finalSpeed = BaseSpeed * config.SprintMultiplier;
                        }
                        else if (Crouching)
                        {
                            _finalSpeed = BaseSpeed * config.CrouchMultiplier;
                        }
                        else
                        {
                            _finalSpeed = BaseSpeed;
                        }
                        _finalAirSpeed = _finalSpeed;
                        JumpUpMul = (MyUpgrades.Jump && Crouching) ? config.SuperJumpMultiplier : 1f;
                        break;
                    }
                case MovementStates.Dash:
                    {
                        _dashTimer += Time.deltaTime;
                        if (_dashTimer > config.DashLength || Motor.BaseVelocity.sqrMagnitude < 0.1)
                        {
                            SetMovementState(MovementStates.Normal);
                        }
                        break;
                    }
            }
            if (MyUpgrades.Smash || MyUpgrades.Cut)
            {
                Ray InteractRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

                if (Physics.Raycast(InteractRay, out RaycastHit HitInfo, config.InteractRange, ~(1 << 2)) && HitInfo.collider.gameObject.TryGetComponent(out Breakable breakable))
                {
                    if (_input.interact && 
                        (((breakable.breakableType == Breakable.BreakableType.Cut) && MyUpgrades.Cut) ||
                         ((breakable.breakableType == Breakable.BreakableType.Smash) && MyUpgrades.Smash)))
                    {
                        breakable.Break();
                    }
                }
            }
            if (_input.interact) { _input.interact = false; }
            //if ()
        }



        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its rotation should be right now. 
        /// This is the ONLY place where you should set the character's rotation
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            switch (MovementState)
            {
                case MovementStates.Normal:
                    {
                        currentRotation = yawRotation;
                        break;
                    }
                case MovementStates.Dash:
                    {
                        currentRotation = yawRotation;
                        break;
                    }
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its velocity should be right now. 
        /// This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (MovementState)
            {
                case MovementStates.Normal:
                    {
                        // Ground movement
                        if (Motor.GroundingStatus.IsStableOnGround)
                        {
                            float currentVelocityMagnitude = currentVelocity.magnitude;

                            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

                            // Reorient velocity on slope
                            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                            // Calculate target velocity
                            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                            Vector3 targetMovementVelocity = reorientedInput * _finalSpeed;

                            // Smooth movement Velocity
                            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-MoveSharpness * deltaTime));
                        }
                        // Air movement
                        else
                        {
                            // Add move input
                            if (_moveInputVector.sqrMagnitude > 0f)
                            {
                                Vector3 addedVelocity = AirAccel * _finalAirSpeed * deltaTime * _moveInputVector;

                                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                                // Limit air velocity from inputs
                                if (currentVelocityOnInputsPlane.magnitude < _finalAirSpeed)
                                {
                                    // clamp addedVel to make total vel not exceed max vel on inputs plane
                                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, _finalAirSpeed);
                                    addedVelocity = newTotal - currentVelocityOnInputsPlane;
                                }
                                else
                                {
                                    // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                                    if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                                    {
                                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                                    }
                                }

                                // Apply added velocity
                                currentVelocity += addedVelocity;

                                // Prevent air-climbing sloped walls
                                if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                                {
                                    Vector3 wallNormal = Motor.GroundingStatus.GroundNormal;
                                    if (Vector3.Dot(currentVelocity, wallNormal) > 0f)
                                    {
                                        currentVelocity = Vector3.ProjectOnPlane(currentVelocity, wallNormal);
                                    }
                                }

                            }

                            if (AscendAccel > 0 && _input.jump && currentVelocity.y < AscendVel)
                            {
                                currentVelocity.y += AscendAccel * deltaTime;
                            }

                            // Gravity ONLY POINTS DOWN IN CURRENT IMPLEMENTATION, but if you want tell me and I'll change this

                            if (currentVelocity.y > -TerminalVelocity)
                            {
                                currentVelocity += (Gravity + new Vector3(0, Buoyancy, 0)) * deltaTime;
                            }
                            else
                            {
                                currentVelocity.y = Mathf.Lerp(currentVelocity.y, -TerminalVelocity, 0.5f);
                            }

                            //currentVelocity.y = (currentVelocity.y < -_terminalVelocity) ? -_terminalVelocity : currentVelocity.y;
                            // Drag
                            currentVelocity.x *= (1f / (1f + (AirDrag * deltaTime)));
                            currentVelocity.y *= (1f / (1f + (0.1f * deltaTime)));
                            currentVelocity.z *= (1f / (1f + (AirDrag * deltaTime)));
                        }

                        // Handle jumping
                        _jumpedThisFrame = false;
                        _timeSinceJumpRequested += deltaTime;
                        if (_jumpRequested)
                        {
                            // See if we actually are allowed to jump
                            if (!_jumpConsumed && ((config.AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= config.JumpPostGroundingGraceTime))
                            {
                                // Calculate jump direction before ungrounding
                                Vector3 jumpDirection = Motor.CharacterUp;
                                if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                                {
                                    jumpDirection = Motor.GroundingStatus.GroundNormal;
                                }

                                // Makes the character skip ground probing/snapping on its next update. 
                                // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                                Motor.ForceUnground();

                                // Add to the return velocity and reset jump state
                                currentVelocity += (JumpUpMul * JumpUpSpeed * jumpDirection) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                _jumpRequested = false;
                                _jumpConsumed = true;
                                _jumpedThisFrame = true;
                                _jumpReleased = false;
                            }
                        }
                        // Take into account additive velocity
                        if (_internalVelocityAdd.sqrMagnitude > 0f)
                        {
                            currentVelocity += _internalVelocityAdd;
                            _internalVelocityAdd = Vector3.zero;
                        }
                        break;
                    }
                case MovementStates.Dash:
                    {
                        currentVelocity = _dashInputVec * config.DashSpeed;
                        break;

                    }
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called after the character has finished its movement update
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            switch (MovementState)
            {
                case MovementStates.Normal:
                    {
                        // Handle jump-related values
                        {
                            // Handle jumping pre-ground grace period
                            if (_jumpRequested && _timeSinceJumpRequested > config.JumpPreGroundingGraceTime)
                            {
                                _jumpRequested = false;
                            }

                            if (config.AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                            {
                                // If we're on a ground surface, reset jumping values
                                if (!_jumpedThisFrame)
                                {
                                    _jumpConsumed = false;
                                }
                                _timeSinceLastAbleToJump = 0f;
                            }
                            else
                            {
                                // Keep track of time since we were last able to jump (for grace period)
                                _timeSinceLastAbleToJump += deltaTime;
                            }
                        }

                        // Handle uncrouching
                        if (Crouching && !_shouldBeCrouching)
                        {
                            // Do an overlap test with the character's standing height to see if there are any obstructions
                            Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                            if (Motor.CharacterOverlap(
                                Motor.TransientPosition,
                                Motor.TransientRotation,
                                _probedColliders,
                                Motor.CollidableLayers,
                                QueryTriggerInteraction.Ignore) > 0)
                            {
                                // If obstructions, just stick to crouching dimensions
                                Motor.SetCapsuleDimensions(0.5f, config.CrouchedCapsuleHeight, config.CrouchedCapsuleHeight * 0.5f);
                            }
                            else
                            {
                                // If no obstructions, uncrouch
                                Crouching = false;
                            }
                        }

                        break;
                    }
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Handle landing and leaving ground
            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
            else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLeaveStableGround();
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (config.IgnoredColliders.Count == 0)
            {
                return true;
            }

            if (config.IgnoredColliders.Contains(coll))
            {
                return false;
            }

            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        protected void OnLanded() { }
        protected void OnLeaveStableGround() { }
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }
        public void LateUpdate()
        {
            if (_input.look.sqrMagnitude >= 0.01f)
            {
                //Don't multiply mouse input by Time.deltaTime
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                pitchRotation += _input.look.y * config.RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * config.RotationSpeed * deltaTimeMultiplier;

                // clamp our pitch rotation
                pitchRotation = ClampAngle(pitchRotation, -90f, 90f);

                // Update Cinemachine camera target pitch
                CinemachineCameraTarget.transform.localEulerAngles = new Vector3(pitchRotation, 0f, 0f);

                // rotate the player left and right
                yawRotation *= Quaternion.Euler(0f, _rotationVelocity, 0f);
                //CinemachineCameraTarget.transform.Rotate(new Vector3(0, _rotationVelocity,0));
            }
            if (Crouching && _crouchLerpTime < 1)
            {
                _crouchLerpTime = Mathf.Min(_crouchLerpTime + Time.deltaTime * config.CrouchLerpSpeed, 1);
                CinemachineCameraTarget.transform.localPosition = new Vector3(0, Mathf.SmoothStep(config.CinemachineBaseHeight, config.CinemachineBaseHeight / 2, _crouchLerpTime), 0);
            } else if (!Crouching && _crouchLerpTime > 0)
            {
                _crouchLerpTime = Mathf.Max(_crouchLerpTime - Time.deltaTime * config.CrouchLerpSpeed, 0);
                CinemachineCameraTarget.transform.localPosition = new Vector3(0, Mathf.SmoothStep(config.CinemachineBaseHeight, config.CinemachineBaseHeight / 2, _crouchLerpTime), 0);
            }

        }
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;

            return Mathf.Clamp(lfAngle, lfMin, lfMax);

        }

        private void OnTriggerEnter(Collider other)
        {
            switch (other.tag)
            {
                case "Water":
                    _waterTriggerCount++;
                    if (_waterTriggerCount == 1) // On entering water
                    {
                        if (MyUpgrades.Swim)
                        {
                            AscendAccel = config.WaterAscendAccel;
                            AscendVel = config.WaterAscendVel;
                        }
                        BaseSpeed = config.WaterBaseSpeed;
                        BaseAirSpeed = config.WaterBaseAirSpeed;
                        AirDrag = config.WaterAirDrag;
                        MoveSharpness = config.WaterMovementSharpness;
                        JumpUpSpeed = config.WaterJumpUpSpeed;
                        Buoyancy = config.Buoyancy;
                        TerminalVelocity = config.WaterTerminalVelocity;
                    }
                    break;
                case "HotZone":
                    _hotTriggerCount++;
                    break;
                case "ColdZone":
                    _coldTriggerCount++;
                    break;
                case "ClimbZone":
                    _climbTriggerCount++;
                    if (_climbTriggerCount == 1) //On entering climb zone
                    {
                        if (_waterTriggerCount == 0) // If not in water
                        {
                            AscendAccel = config.ClimbAccel;
                            AscendVel = config.ClimbUpVel;
                            Buoyancy = config.ClimbSlowFall;
                            TerminalVelocity = config.ClimbDownVel;
                            AirDrag = config.ClimbAirDrag;
                        } else if (!MyUpgrades.Swim) // If in water but doesn't have swim upgrade
                        {
                            AscendAccel = config.ClimbAccel;
                            AscendVel = config.ClimbUpVel;
                            Buoyancy = Math.Max(config.ClimbSlowFall, config.Buoyancy);
                            TerminalVelocity = Math.Min(config.ClimbDownVel, config.WaterTerminalVelocity);
                            AirDrag = Math.Max(config.ClimbAirDrag, config.WaterAirDrag);
                        }

                    }
                    break;
                default:
                    break;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            switch (other.tag)
            {
                case "Water":
                    _waterTriggerCount--;
                    if (_waterTriggerCount == 0) // On exiting water
                    {
                        if (_climbTriggerCount == 0) // If not in a climb zone
                        {
                            AscendAccel = 0;
                            AscendVel = 0;
                            Buoyancy = 0;
                            TerminalVelocity = config.BaseTerminalVelocity;
                            AirDrag = config.AirDrag;

                            BaseSpeed = config.BaseSpeed;
                            BaseAirSpeed = config.BaseAirSpeed;
                            MoveSharpness = config.MovementSharpness;
                            JumpUpSpeed = config.JumpUpSpeed;
                        }
                        else // If in a climb zone
                        {
                            AscendAccel = config.ClimbAccel;
                            AscendVel = config.ClimbUpVel;
                            Buoyancy = config.ClimbSlowFall;
                            TerminalVelocity = config.ClimbDownVel;
                            AirDrag = config.ClimbAirDrag;

                            BaseSpeed = config.BaseSpeed;
                            BaseAirSpeed = config.BaseAirSpeed;
                            MoveSharpness = config.MovementSharpness;
                            JumpUpSpeed = config.JumpUpSpeed;
                        }
                    }
                    break;
                case "HotZone":
                    _hotTriggerCount--;
                    break;
                case "ColdZone":
                    _coldTriggerCount--;
                    break;
                case "ClimbZone":
                    _climbTriggerCount--;
                    if (_climbTriggerCount == 0) //On exiting climb zone
                    {
                        if (_waterTriggerCount == 0) // If not in water
                        {
                            AscendAccel = 0;
                            AscendVel = 0;
                            Buoyancy = 0;
                            TerminalVelocity = config.BaseTerminalVelocity;
                            AirDrag = config.AirDrag;
                        } else if (!MyUpgrades.Swim) //If in water but doesn't have swim ugprade
                        {
                            AscendAccel = 0;
                            AscendVel = 0;
                            Buoyancy = config.Buoyancy;
                            TerminalVelocity = config.WaterTerminalVelocity;
                            AirDrag = config.WaterAirDrag;
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        private void UpdateZoneState()
        {
            WaterState = (_waterTriggerCount > 0) ? (byte)1 : (byte)0;
            ColdState = (_coldTriggerCount > 0) ? (byte)1 : (byte)0;
            HotState = (_hotTriggerCount > 0) ? (byte)1 : (byte)0;
            ClimbState = (_climbTriggerCount > 0) ? (byte)1 : (byte)0;
            int InsideZoneCheckMax=10;
            Collider[] colArray = new Collider[InsideZoneCheckMax];
            Physics.OverlapSphereNonAlloc(CinemachineCameraTarget.transform.position, 0.1f, colArray );
            for (int i = 0; i < colArray.Length; i++)
            {
                if (colArray[i]==null) { break; }
                Collider col = colArray[i];
                switch (col.tag)
                {
                    case "Water":
                        WaterState = 2;
                        break;
                    case "HotZone":
                        HotState = 2;
                        break;
                    case "ColdZone":
                        ColdState = 2;
                        break;
                    case "ClimbZone":
                        ClimbState = 2;
                        break;
                    default:
                        break;
                }
            }
        }
        public void Teleport(Vector3 pos)
        {
            Motor.BaseVelocity = Vector3.zero;
            Motor.SetPosition(pos);
            SetMovementState(MovementStates.Normal);
        }
#if UNITY_EDITOR
        [Header("Debug")]
        public bool EnableDebugVelocityArrow = false;
        public Mesh DebugArrowShaft;
        public Mesh DebugArrowHead;
        public bool EnableDebugRespawnPosition = false;
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 0, 0.25f);

            if (Motor.BaseVelocity.magnitude>0 && EnableDebugVelocityArrow)
            {
                Quaternion ArrowRot = Quaternion.LookRotation(Motor.BaseVelocity);
                
                Gizmos.DrawMesh(DebugArrowShaft, -1, transform.position + Vector3.up, ArrowRot, new Vector3(1, 1, Motor.BaseVelocity.magnitude / 5));
                Gizmos.DrawMesh(DebugArrowHead, -1, (transform.position + Vector3.up) + (Motor.BaseVelocity / 5), ArrowRot);
            }
            if (EnableDebugRespawnPosition)
            {
                Gizmos.DrawSphere(RespawnPos, 0.1f);
            }
        }

#endif
    }
}