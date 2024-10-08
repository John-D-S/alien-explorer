using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using UnityEngine.InputSystem;
using System;



public class TestController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;
    //[Space(10)]
    //[Header("Character Default Settings")]
    //[Header("Walking Grounded Movement")]
    [Serializable]
    public struct PlayerConfig
    {
        [Header("Walking Grounded Movement")]
        public float BaseSpeed;
        public float MovementSharpness;
        [Header("Walking Air Movement")]
        public float BaseAirSpeed;
        public float AirAccel;
        public float AirDrag;
        [Header("Sprinting Movement")]
        public float SprintMultiplier;
        [Header("Crouching Movement")]
        public float CrouchMultiplier;

        [Header("Jumping")]
        public bool AllowJumpingWhenSliding;
        public float JumpUpSpeed;
        public float JumpPreGroundingGraceTime;
        public float JumpPostGroundingGraceTime;
        public float SuperJumpMultiplier;
        [Header("Underwater")]
        public float WaterSwimUpAccel;
        public float WaterBaseSpeed;
        public float WaterMovementSharpness;
        public float WaterBaseAirSpeed;
        public float WaterAirAccel;
        public float WaterAirDrag;
        public float WaterJumpUpSpeed;
        public float WaterTerminalVelocity;
        public float Buoyancy;
        [Header("Misc")]
        public List<Collider> IgnoredColliders;
        public float DefaultGravityStrength;
        public float CrouchedCapsuleHeight;
        public float BaseTerminalVelocity;

        [Header("Dash")]
        public float DashLength;
        public float DashCooldownLength;
        public float DashSpeed;
        [Header("Glide")]
        public float GlideUpAccel;
        [Header("Hazard")]
        public float HazardRespawnTime;
        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        public float CinemachineBaseHeight;
        public float RotationSpeed;
        public float CrouchLerpSpeed;
    }
    [SerializeField] //Defaults, values can be changed in editor
    private PlayerConfig config = new()
    {
        BaseSpeed = 4f,
        MovementSharpness = 15f,
        BaseAirSpeed = 4f,
        AirAccel = 15f,
        AirDrag = 0.1f,
        SprintMultiplier = 2f,
        CrouchMultiplier = 0.5f,
        AllowJumpingWhenSliding = false,
        JumpUpSpeed = 10f,
        JumpPreGroundingGraceTime = 0f,
        JumpPostGroundingGraceTime = 0f,
        SuperJumpMultiplier = 3f,
        WaterSwimUpAccel = 10f,
        WaterBaseSpeed = 3f,
        WaterMovementSharpness = 4f,
        WaterBaseAirSpeed = 3f,
        WaterAirAccel = 7.5f,
        WaterAirDrag = 0.8f,
        WaterJumpUpSpeed = 3f,
        WaterTerminalVelocity = 3f,
        Buoyancy = 25f,
        IgnoredColliders = new List<Collider>(),
        DefaultGravityStrength = 30,
        CrouchedCapsuleHeight = 1f,
        BaseTerminalVelocity = 50f,
        DashLength = 0.1f,
        DashCooldownLength = 2f,
        DashSpeed = 20f,
        GlideUpAccel = 0.5f,
        HazardRespawnTime = 3f,
        CinemachineBaseHeight = 1.375f,
        RotationSpeed = 1.0f,
        CrouchLerpSpeed = 4f
    };
    [Serializable]
    public struct Upgrades
    {
        public bool Jump;
        public bool Dash;
        public bool Swim;
        public bool Glide;
        public bool Heat;
        public bool Cold;
        public bool Cut;
        public bool Smash;
    }
    public Upgrades PlayerUpgrades = new Upgrades 
    { 
        Jump = false, 
        Dash = false, 
        Swim = false, 
        Glide = false, 
        Heat = false, 
        Cold = false, 
        Cut = false, 
        Smash = false 
    };
    // cinemachine
    private float _cinemachineTargetPitch=0;
    

    [Header("Player State")]
    public Quaternion _tgtYaw;
    public bool _isSprinting = false;
    public bool _isUnderwater;
    public float _baseSpeed;
    public float _finalSpeed;
    public float _movementSharpness;
    public float _baseAirSpeed;
    public float _finalAirSpeed;
    public float _airAccel;
    public float _drag;
    public float _jumpUpSpeed;
    public Vector3 _gravity;
    public float _terminalVelocity;
    public float _holdJumpAccel = 0; //upwards acceleration while holding jump button- used for glide and swimming
    public bool _isCrouching = false;
    public bool _jumpConsumed = false;
    public enum MovementState
    {
        Normal,
        Dash,
    }
    public MovementState _movementState;

    private PlayerInput _playerInput;
    private InputHandler _input;
    private GameObject _mainCamera;

    private Collider[] _probedColliders = new Collider[8];
    private RaycastHit[] _probedHits = new RaycastHit[8];
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
    private Vector3 lastInnerNormal = Vector3.zero;
    private Vector3 lastOuterNormal = Vector3.zero;
    private float _rotationVelocity;
    
    private bool IsCurrentDeviceMouse
    {
        get
        {
            return _playerInput.currentControlScheme == "KeyboardMouse";

        }
    }
    private float _crouchLerpTime=0;
    private void Awake()
    {
        _tgtYaw = this.transform.rotation;
        // Handle initial state
        SetMovementState(MovementState.Normal);

        // Assign the characterController to the motor
        Motor.CharacterController = this;

        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
        _baseSpeed = config.BaseSpeed;
        _finalSpeed = _baseSpeed;
        _movementSharpness = config.MovementSharpness;
        _baseAirSpeed = config.BaseAirSpeed;
        _airAccel = config.AirAccel;
        _drag = config.AirDrag;
        _jumpUpSpeed = config.JumpUpSpeed;
        _gravity = Vector3.down * config.DefaultGravityStrength;
        _terminalVelocity = config.BaseTerminalVelocity;
    }
    private void Start()
    {
        _input = GetComponent<InputHandler>();
        _playerInput = GetComponent<PlayerInput>();
    }
    /// <summary>
    /// Handles movement state transitions and enter/exit callbacks
    /// </summary>
    public void SetMovementState(MovementState newState)
    {

        //When exiting a movement state
        switch (_movementState)
        {
            case MovementState.Normal:
                {
                    break;
                }
            case MovementState.Dash:
                {
                    _dashTimer = 0;
                    Motor.AllowSteppingWithoutStableGrounding = false;
                    break;
                }
        }

        MovementState tmpOldState = _movementState;
        _movementState = newState;

        //When entering a movement state
        switch (_movementState)
        {
            case MovementState.Normal:
                {
                    break;
                }
            case MovementState.Dash:
                {
                    _dashTimer = 0;

                    _dashInputVec = (_moveInputVector.magnitude > 0.05f) ? Vector3.ProjectOnPlane(_moveInputVector.normalized,Motor.CharacterUp) : Motor.CharacterForward;
                    _dashReady = false;
                    Motor.AllowSteppingWithoutStableGrounding = true;
                    break;
                }
        }
    }

    
    /// <summary>
    /// This is called every frame, determines input and player state
    /// </summary>
    private void Update()
    {
        // Clamp input
        Vector3 inputVector = Vector3.ClampMagnitude(new Vector3(_input.move.x,0,_input.move.y),1f);

        // Calculate camera direction and rotation on the character plane
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(transform.forward, Motor.CharacterUp);

        // Move and look inputs
        _moveInputVector = cameraPlanarRotation * inputVector;
        //Character state updates
        switch (_movementState)
        {
            case MovementState.Normal:
                {
                    // Request jump if jump input
                    if (_input.jump)
                    {
                        _timeSinceJumpRequested = 0f;
                        _jumpRequested = true;
                    }

                    //dash
                    if (PlayerUpgrades.Dash && _input.dash && _dashReady )
                    {
                        SetMovementState(MovementState.Dash);
                    }
                    if (!_dashReady)
                    {
                        _dashTimer += Time.deltaTime;
                        if (_dashTimer>config.DashCooldownLength)
                        {
                            _dashReady = true;  
                        }
                    }
                    
                    _shouldBeCrouching = _input.crouch;
                    bool stable = Motor.GroundingStatus.IsStableOnGround;

                    if (!_isCrouching && _shouldBeCrouching)
                    {
                        _isCrouching = true;
                        if (stable)
                        {
                            _isSprinting = false;
                        }
                        Motor.SetCapsuleDimensions(0.5f, config.CrouchedCapsuleHeight, config.CrouchedCapsuleHeight * 0.5f);
                    }
                    if (_input.sprint)
                    {
                        if (stable)
                        {
                            _isSprinting = !_isCrouching;
                            //_isSprinting = true;
                        }
                    }
                    else
                    {
                        _isSprinting = false;
                    }

                    if (_isSprinting)
                    {
                        _finalSpeed = _baseSpeed * config.SprintMultiplier;
                    }
                    else if (_isCrouching)
                    {
                        _finalSpeed = _baseSpeed * config.CrouchMultiplier;
                    }
                    else
                    {
                        _finalSpeed = _baseSpeed;
                    }
                    _finalAirSpeed = _finalSpeed;
                    break;
                }
            case MovementState.Dash:
                {
                    _dashTimer += Time.deltaTime;
                    if (_dashTimer > config.DashLength || Motor.BaseVelocity.sqrMagnitude<0.1)
                    {
                        SetMovementState(MovementState.Normal);
                    }
                    break;
                }
        }
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
        switch (_movementState)
        {
            case MovementState.Normal:
                {
                    //if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
                    //{
                        // Smoothly interpolate from current to target look direction
                        //Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                        // Set the current rotation (which will be used by the KinematicCharacterMotor)
                        currentRotation = _tgtYaw;
                    //}

                 
                    
                    //Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                    //currentRotation = Quaternion.FromToRotation(currentUp, Vector3.up) * currentRotation;

                    break;
                }
            case MovementState.Dash:
                {
                    currentRotation = _tgtYaw;
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
        switch (_movementState)
        {
            case MovementState.Normal:
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
                        currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-_movementSharpness * deltaTime));
                    }
                    // Air movement
                    else
                    {
                        // Add move input
                        if (_moveInputVector.sqrMagnitude > 0f)
                        {
                            Vector3 addedVelocity = _baseAirSpeed * deltaTime * _moveInputVector * _airAccel;

                            Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                            // Limit air velocity from inputs
                            if (currentVelocityOnInputsPlane.magnitude < _baseAirSpeed)
                            {
                                // clamp addedVel to make total vel not exceed max vel on inputs plane
                                Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, _baseAirSpeed);
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

                            // Prevent air-climbing sloped walls
                            if (Motor.GroundingStatus.FoundAnyGround)
                            {
                                if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                                {
                                    Vector3 perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpendicularObstructionNormal);
                                }
                            }

                            // Apply added velocity
                            currentVelocity += addedVelocity;
                        }

                        // Gravity ONLY POINTS DOWN IN CURRENT IMPLEMENTATION, but if you want tell me and I'll change this
                        currentVelocity += (_gravity + new Vector3(0, _input.jump ? _holdJumpAccel : 0, 0)) * deltaTime;
                        currentVelocity.y = (currentVelocity.y < -_terminalVelocity) ? -_terminalVelocity : currentVelocity.y;
                        // Drag
                        currentVelocity *= (1f / (1f + (_drag * deltaTime)));
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
                            currentVelocity += (jumpDirection * _jumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                            _jumpRequested = false;
                            _jumpConsumed = true;
                            _jumpedThisFrame = true;
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
            case MovementState.Dash:
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
        switch (_movementState)
        {
            case MovementState.Normal:
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
                    if (_isCrouching && !_shouldBeCrouching)
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
                            _isCrouching = false;
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

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void AddVelocity(Vector3 velocity)
    {
        switch (_movementState)
        {
            case MovementState.Normal:
                {
                    _internalVelocityAdd += velocity;
                    break;
                }
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    protected void OnLanded()
    {
    }

    protected void OnLeaveStableGround()
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
    public void LateUpdate()
    {
        if (_input.look.sqrMagnitude >= 0.01f)
        {
            //Don't multiply mouse input by Time.deltaTime
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetPitch += _input.look.y * config.RotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = _input.look.x * config.RotationSpeed * deltaTimeMultiplier;

            // clamp our pitch rotation
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, -90f, 90f);

            // Update Cinemachine camera target pitch
            config.CinemachineCameraTarget.transform.localEulerAngles = new Vector3(_cinemachineTargetPitch, 0f, 0f);

            // rotate the player left and right
            _tgtYaw *= Quaternion.Euler(0f, _rotationVelocity, 0f);
            //CinemachineCameraTarget.transform.Rotate(new Vector3(0, _rotationVelocity,0));
        }
        if (_isCrouching && _crouchLerpTime<1)
        {
            _crouchLerpTime = Mathf.Min(_crouchLerpTime + Time.deltaTime* config.CrouchLerpSpeed, 1);
            config.CinemachineCameraTarget.transform.localPosition = new Vector3(0, Mathf.SmoothStep(config.CinemachineBaseHeight, config.CinemachineBaseHeight / 2, _crouchLerpTime), 0);
        } else if (!_isCrouching && _crouchLerpTime>0)
        {
            _crouchLerpTime = Mathf.Max(_crouchLerpTime - Time.deltaTime* config.CrouchLerpSpeed, 0);
            config.CinemachineCameraTarget.transform.localPosition = new Vector3(0, Mathf.SmoothStep(config.CinemachineBaseHeight, config.CinemachineBaseHeight / 2, _crouchLerpTime), 0);
        }
        
    }
    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;

        return Mathf.Clamp(lfAngle, lfMin, lfMax);
        
    }
    public Collider tempwatercollider;
    private void OnTriggerEnter(Collider other)
    {
        if (other==tempwatercollider)
        {
            _isUnderwater = true;
            _holdJumpAccel = config.WaterSwimUpAccel;
            _baseSpeed = config.WaterBaseSpeed;
            _baseAirSpeed = config.WaterBaseAirSpeed;
            _drag = config.WaterAirDrag;
            _movementSharpness = config.WaterMovementSharpness;
            _jumpUpSpeed = config.WaterJumpUpSpeed;
            _gravity = Vector3.down * (config.DefaultGravityStrength - config.Buoyancy);
            _terminalVelocity = config.WaterTerminalVelocity;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        
        if (other == tempwatercollider)
        {
            _isUnderwater = false;
            _holdJumpAccel = 0;
            _baseSpeed = config.BaseSpeed;
            _baseAirSpeed = config.BaseAirSpeed;
            _drag = config.AirDrag;
            _movementSharpness = config.MovementSharpness;
            _jumpUpSpeed = config.JumpUpSpeed;
            _gravity = Vector3.down * config.DefaultGravityStrength;
            _terminalVelocity = config.BaseTerminalVelocity;
        }
    }
}
 