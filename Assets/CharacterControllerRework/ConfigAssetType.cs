using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CharacterSystem 
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "ScriptableObjects/ConfigAsset", order = 1)]
    public class ConfigAssetType : ScriptableObject
    {
        [Header("Walking Grounded Movement")]
        public float BaseSpeed = 4f;
        public float MovementSharpness = 15f;
        [Header("Walking Air Movement")]
        public float BaseAirSpeed = 4f;
        public float AirAccel = 10f;
        public float AirDrag = 0.1f;
        [Header("Sprinting Movement")]
        public float SprintMultiplier = 2f;
        [Header("Crouching Movement")]
        public float CrouchMultiplier = 0.5f;

        [Header("Jumping")]
        public bool AllowJumpingWhenSliding = false;
        public float JumpUpSpeed = 10f;
        public float JumpPreGroundingGraceTime = 0f;
        public float JumpPostGroundingGraceTime = 0f;
        public float SuperJumpMultiplier = 3f;
        [Header("Underwater")]
        public float WaterAscendAccel = 10f;
        public float WaterAscendVel = 5f;
        public float WaterBaseSpeed = 3f;
        public float WaterMovementSharpness = 4f;
        public float WaterBaseAirSpeed = 3f;
        public float WaterAirAccel = 7.5f;
        public float WaterAirDrag = 0.8f;
        public float WaterJumpUpSpeed = 3f;
        public float WaterTerminalVelocity = 3f;
        public float Buoyancy = 25f;
        [Header("Climbing")]
        public float ClimbAccel = 30;
        public float ClimbUpVel = 6f;
        public float ClimbDownVel = 3f;
        public float ClimbSlowFall = 10f; //same as buoyancy but would be weird to call it that
        public float ClimbAirDrag = 1f;
        [Header("Misc")]
        public List<Collider> IgnoredColliders = new List<Collider>();
        public float DefaultGravityStrength = 30f;
        public float CrouchedCapsuleHeight = 50f;
        public float BaseTerminalVelocity = 50f;
        public float RespawnTimerLength = 1f;
        [Header("Dash")]
        public float DashLength = 0.1f;
        public float DashCooldownLength = 2f;
        public float DashSpeed = 20f;
        [Header("Glide")]
        public float GlideTerminalVelocity = 2f;
        [Header("Hazard")]
        //public float HazardRespawnTime;
        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        //public GameObject CinemachineCameraTarget;
        public float CinemachineBaseHeight = 1.375f;
        public float RotationSpeed = 1f;
        public float CrouchLerpSpeed = 4f;

    }
}