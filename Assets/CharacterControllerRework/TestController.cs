using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using UnityEngine.InputSystem;
using System;

public enum CharacterState
{
    Default,
    Dash,
}

public class TestController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;

    [Header("Walking Grounded Movement")]
    public float DefaultWalkSpeed = 8f;
    public float DefaultMovementSharpness = 15f;
    public float OrientationSharpness = 10f;

    [Header("Walking Air Movement")]
    public float DefaultWalkAirSpeed = 8f;
    public float DefaultWalkAirAccel = 15f;
    public float DefaultDrag = 0.1f;

    [Header("Sprinting Movement")]
    public float SprintMultiplier = 2;

    [Header("Crouching Movement")]
    public float CrouchMultiplier = 0.5f;

    [Header("Jumping")]
    public bool AllowJumpingWhenSliding = false;
    public float NormalJumpUpSpeed = 10f;
    public float NormalJumpForwardSpeed = 0f;
    public float JumpPreGroundingGraceTime = 0f;
    public float JumpPostGroundingGraceTime = 0f;
    public float SuperJumpMultiplier = 3f;
    [Header("Misc")]
    public List<Collider> IgnoredColliders = new List<Collider>();
    public float BonusOrientationSharpness = 10f;
    public Vector3 DefaultGravity = new Vector3(0, -30f, 0);
    public float CrouchedCapsuleHeight = 1f;

    [Header("Dash")]
    public float DashLength=0.5f;
    public float DashCooldownLength=5f;
    public float DashSpeed=15f;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -90.0f;
    public float CinemachineBaseHeight = 1.375f;
    public float RotationSpeed = 1.0f;
    public float CrouchLerpSpeed = 0.25f;
    [Tooltip("0:Jump, 1:Dash, 2:Swim, 3:Glide, 4:Heat, 5:Cold, 6:Cut 7:Smash")]
    public Upgrades PlayerUpgrades = new Upgrades();

    // cinemachine
    private float _cinemachineTargetPitch=0;
    private float _cinemachineTargetHeight;
    public CharacterState CurrentCharacterState { get; private set; }

    private PlayerInput _playerInput;
    private InputHandler _input;
    private GameObject _mainCamera;

    private Collider[] _probedColliders = new Collider[8];
    private RaycastHit[] _probedHits = new RaycastHit[8];
    private Vector3 _moveInputVector;
    private Vector3 _lookInputVector;
    private bool _jumpRequested = false;
    private bool _jumpConsumed = false;
    private bool _jumpedThisFrame = false;
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private float _timeSinceLastAbleToJump = 0f;
    private Vector3 _internalVelocityAdd = Vector3.zero;
    private bool _shouldBeCrouching = false;
    private bool _isCrouching = false;
    private float _dashTimer = 0f;
    private bool _dashReady = true;
    private Vector3 lastInnerNormal = Vector3.zero;
    private Vector3 lastOuterNormal = Vector3.zero;
    private float _rotationVelocity;
    public Quaternion _tgtYaw;
    private bool _isSprinting=false;
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
        TransitionToState(CharacterState.Default);

        // Assign the characterController to the motor
        Motor.CharacterController = this;

        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }
    private void Start()
    {
        _input = GetComponent<InputHandler>();
        _playerInput = GetComponent<PlayerInput>();
    }
    /// <summary>
    /// Handles movement state transitions and enter/exit callbacks
    /// </summary>
    public void TransitionToState(CharacterState newState)
    {
        CharacterState tmpInitialState = CurrentCharacterState;
        OnStateExit(tmpInitialState, newState);
        CurrentCharacterState = newState;
        OnStateEnter(newState, tmpInitialState);
    }

    /// <summary>
    /// Event when entering a state
    /// </summary>
    public void OnStateEnter(CharacterState state, CharacterState fromState)
    {
        switch (state)
        {
            case CharacterState.Default:
                {
                    break;
                }
            case CharacterState.Dash:
                {
                    
                    break;
                }
        }
    }

    /// <summary>
    /// Event when exiting a state
    /// </summary>
    public void OnStateExit(CharacterState state, CharacterState toState)
    {
        switch (state)
        {
            case CharacterState.Default:
                {
                    break;
                }
            case CharacterState.Dash:
                {
                    _dashTimer = 0;
                    break;
                }
        }
    }

    /// <summary>
    /// This is called every frame by ExamplePlayer in order to tell the character what its inputs are
    /// </summary>
    private void Update()
    {
        // Clamp input
        Vector3 moveInputVector = new Vector3(_input.move.x,0,_input.move.y).normalized;



        // Calculate camera direction and rotation on the character plane
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(transform.forward, Motor.CharacterUp);
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
                {
                    // Move and look inputs
                    _moveInputVector = cameraPlanarRotation * moveInputVector;
                    _lookInputVector = _tgtYaw * Vector3.forward;

                    // Jumping input
                    if (_input.jump)
                    {
                        _timeSinceJumpRequested = 0f;
                        _jumpRequested = true;
                    }
                    //dash
                    if (_input.dash && _dashTimer<=0)
                    {
                        TransitionToState(CharacterState.Dash);
                    }
                    // Crouching input
                    if (_input.crouch && _shouldBeCrouching == false)
                    {
                        _shouldBeCrouching = true;

                        if (!_isCrouching)
                        {
                            _isCrouching = true;
                            Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                        }
                    }
                    else if (_shouldBeCrouching == true)
                    {
                        _shouldBeCrouching = false;
                    }
                     
                    if (!_isCrouching)
                    {
                        if ( !_jumpConsumed && _input.sprint)
                        {
                            _isSprinting = true;
                        } else if ( !_input.sprint )
                        {
                            _isSprinting = false;
                        }
                    } else { _isSprinting = false; }
                    //_isSprinting = (!_jumpConsumed && !_isCrouching && _input.sprint);
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
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
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
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its velocity should be right now. 
    /// This is the ONLY place where you can set the character's velocity
    /// </summary>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
                {
                    if (_isCrouching)
                    {
                        DefaultMovement(ref currentVelocity, deltaTime, DefaultGravity, DefaultWalkSpeed * CrouchMultiplier, DefaultMovementSharpness, 
                                        DefaultWalkAirSpeed * CrouchMultiplier, DefaultWalkAirAccel, DefaultDrag, 
                                        (PlayerUpgrades.Jump) ? (NormalJumpUpSpeed * SuperJumpMultiplier) : (NormalJumpUpSpeed), NormalJumpForwardSpeed);
                    }
                    else if (_isSprinting)
                    {
                        DefaultMovement(ref currentVelocity, deltaTime, DefaultGravity, DefaultWalkSpeed * SprintMultiplier, DefaultMovementSharpness, 
                                        DefaultWalkAirSpeed * SprintMultiplier, DefaultWalkAirAccel, DefaultDrag, NormalJumpUpSpeed, NormalJumpForwardSpeed);
                    }
                    else
                    {
                        DefaultMovement(ref currentVelocity, deltaTime, DefaultGravity, DefaultWalkSpeed, DefaultMovementSharpness, DefaultWalkAirSpeed,
                                        DefaultWalkAirAccel, DefaultDrag, NormalJumpUpSpeed, NormalJumpForwardSpeed);
                    }
                    break;
                }
            case CharacterState.Dash:
                {

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
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
                {
                    // Handle jump-related values
                    {
                        // Handle jumping pre-ground grace period
                        if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                        {
                            _jumpRequested = false;
                        }

                        if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
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
                            Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
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
        if (IgnoredColliders.Count == 0)
        {
            return true;
        }

        if (IgnoredColliders.Contains(coll))
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
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
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

            _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

            // clamp our pitch rotation
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Update Cinemachine camera target pitch
            CinemachineCameraTarget.transform.localEulerAngles = new Vector3(_cinemachineTargetPitch, 0f, 0f);

            // rotate the player left and right
            _tgtYaw *= Quaternion.Euler(0f, _rotationVelocity, 0f);
            //CinemachineCameraTarget.transform.Rotate(new Vector3(0, _rotationVelocity,0));
        }
        if (_isCrouching && _crouchLerpTime<1)
        {
            _crouchLerpTime = Mathf.Min(_crouchLerpTime + Time.deltaTime*CrouchLerpSpeed, 1);
            CinemachineCameraTarget.transform.localPosition = new Vector3(0, Mathf.SmoothStep(CinemachineBaseHeight, CinemachineBaseHeight / 2, _crouchLerpTime), 0);
        } else if (!_isCrouching && _crouchLerpTime>0)
        {
            _crouchLerpTime = Mathf.Max(_crouchLerpTime - Time.deltaTime*CrouchLerpSpeed, 0);
            CinemachineCameraTarget.transform.localPosition = new Vector3(0, Mathf.SmoothStep(CinemachineBaseHeight, CinemachineBaseHeight / 2, _crouchLerpTime), 0);
        }
        
    }
    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;

        return Mathf.Clamp(lfAngle, lfMin, lfMax);
        
    }
    private void DefaultMovement(   ref Vector3 currentVelocity, float deltaTime, Vector3 grav, float groundMaxSpeed, float groundSharpness, float airMaxSpeed, float airAccel, 
                                    float airDrag, float jumpUp, float jumpForward, float holdJumpAccel=0f, float terminalVelocity=50f)
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
            Vector3 targetMovementVelocity = reorientedInput * groundMaxSpeed;

            // Smooth movement Velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-groundSharpness * deltaTime));
        }
        // Air movement
        else
        {
            // Add move input
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                Vector3 addedVelocity = _moveInputVector * airAccel * deltaTime;

                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                // Limit air velocity from inputs
                if (currentVelocityOnInputsPlane.magnitude < airMaxSpeed)
                {
                    // clamp addedVel to make total vel not exceed max vel on inputs plane
                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, airMaxSpeed);
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

            // Gravity ONLY POINTS DOWN IN CURRENT IMPLEMENTATION
            currentVelocity += (grav+new Vector3(0,holdJumpAccel,0)) * deltaTime;
            currentVelocity.y = (currentVelocity.y < -terminalVelocity) ? -terminalVelocity : currentVelocity.y;
            // Drag
            currentVelocity *= (1f / (1f + (airDrag * deltaTime)));
        }

        // Handle jumping
        _jumpedThisFrame = false;
        _timeSinceJumpRequested += deltaTime;
        if (_jumpRequested)
        {
            // See if we actually are allowed to jump
            if (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime))
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
                currentVelocity += (jumpDirection * jumpUp) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                currentVelocity += (_moveInputVector * jumpForward);
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
    }
    public class Upgrades
    {
        public byte upgradeByte=0;
        public bool Jump
        {
            get { return Convert.ToBoolean(upgradeByte & (1 << 7)); }
            set { upgradeByte = (byte)((upgradeByte & ~(1 << 7)) | (Convert.ToByte(value) << 7)); }
        }
        public bool Dash
        {
            get { return Convert.ToBoolean(upgradeByte & (1 << 6)); }
            set { upgradeByte = (byte)((upgradeByte & ~(1 << 6)) | (Convert.ToByte(value) << 6)); }
        }
        public bool Swim
        {
            get { return Convert.ToBoolean(upgradeByte & (1 << 5)); }
            set { upgradeByte = (byte)((upgradeByte & ~(1 << 5)) | (Convert.ToByte(value) << 5)); }
        }
        public bool Glide
        {
            get { return Convert.ToBoolean(upgradeByte & (1 << 4)); }
            set { upgradeByte = (byte)((upgradeByte & ~(1 << 4)) | (Convert.ToByte(value) << 4)); }
        }
        public bool Heat
        {
            get { return Convert.ToBoolean(upgradeByte & (1 << 3)); }
            set { upgradeByte = (byte)((upgradeByte & ~(1 << 3)) | (Convert.ToByte(value) << 3)); }
        }
        public bool Cold
        {
            get { return Convert.ToBoolean(upgradeByte & (1 << 2)); }
            set { upgradeByte = (byte)((upgradeByte & ~(1 << 2)) | (Convert.ToByte(value) << 2)); }
        }
        public bool Cut
        {
            get { return Convert.ToBoolean(upgradeByte & (1 << 1)); }
            set { upgradeByte = (byte)((upgradeByte & ~(1 << 1)) | (Convert.ToByte(value) << 1)); }
        }
        public bool Smash
        {
            get { return Convert.ToBoolean(upgradeByte & (1)); }
            set { upgradeByte = (byte)((upgradeByte & ~(1)) | (Convert.ToByte(value))); }
        }
    }
}
 