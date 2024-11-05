using UnityEngine;

public class PlayerController: MonoBehaviour
{
    [Header("Movement")]
    public float maxWalkSpeed = 5f;
    public float maxSprintSpeed = 8f;
    public float acceleration = 25f;
    public float jumpForce = 1f;
    public float crouchSpeed = 2.5f;
    public float gravity = -9.81f;
    public float maxSlopeAngle = 45f;

    [Header("Camera")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 90f;

    [Header("Upgrades")]
    public bool jumpUpgrade = false;
    public bool dashUpgrade = false;
    public bool swimUpgrade = false;
    public bool glideUpgrade = false;
    public bool heatResist = false;
    public bool coldResist = false;
    public bool cutPlants = false;
    public bool smashRocks = false;

    [Header("Upgrade Settings")]
    public float superJumpForce = 3f;
    public float dashSpeed = 20f;
    public float dashLength = 0.5f;
    public float dashCooldown = 5f;
    public float swimSinkSpeed = 1f;
    public float glideSpeed = 2f;
    public float glideMovementMultiplier = 1.5f;

    private CharacterController controller;
    private GameObject playerCamera;
    private float verticalRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching;
    private bool isGliding;
    private bool isInWater;
    private bool isDashing;
    private float timeSinceDashPressed = 5;

    //moving platform code
    private Transform currentPlatform = null;
    private Vector3 platformLastPosition;
    private Quaternion platformLastRotation;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        //playerCamera = GetComponentInChildren<Camera>();
        playerCamera = transform.GetChild(0).gameObject;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;

        HandleJump();
        HandleMovement();
        HandleRotation();
        HandleCrouch();
        HandleUpgrades();
        HandleClimbing();
        HandleInteractions();
    }

    bool isSlipping = false;
    private void HandleMovement()
    {
        if(!isSlipping)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            Vector3 moveDirection = (transform.right * x + transform.forward * z).normalized;
            float targetSpeed = isCrouching ? crouchSpeed : (Input.GetKey(KeyCode.LeftShift) ? maxSprintSpeed : maxWalkSpeed);

            // Smooth acceleration
            Vector3 targetVelocity = moveDirection * targetSpeed + new Vector3(0, velocity.y, 0);
            velocity = Vector3.MoveTowards(new Vector3(velocity.x, velocity.y, velocity.z), targetVelocity, acceleration * Time.deltaTime);
        }

        if (isGrounded)
        {
            // Check for steep slopes and platforms
            RaycastHit hit;
            float sphereCastRadius = controller.radius;
            float sphereCastDistance = controller.height / 2 + 0.2f; // Adding a small buffer
            if (Physics.SphereCast(transform.position, sphereCastRadius, Vector3.down, out hit, sphereCastDistance))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                Debug.Log(slopeAngle);
                if (slopeAngle > maxSlopeAngle)
                {
                    // Slide down the slope
                    if(!isSlipping)
                    {
                        velocity = new Vector3(0, velocity.y, 0);
                    }
                    isSlipping = true;
                    velocity += Vector3.ProjectOnPlane(Physics.gravity, hit.normal) * Time.deltaTime;
                }
                else
                {
                    isSlipping = false;
                }

                // Check if we're standing on a moving platform
                if (hit.collider.CompareTag("MovingPlatform"))
                {
                    if (currentPlatform != hit.collider.transform)
                    {
                        currentPlatform = hit.collider.transform;
                        platformLastPosition = currentPlatform.position;
                        platformLastRotation = currentPlatform.rotation;
                    }
                }
                else
                {
                    currentPlatform = null;
                }
            }
            if(!isSlipping)
            {
                velocity.y = gravity; // Apply gravity when grounded
            }
        }
        else
        {
            currentPlatform = null;
            isSlipping = false;
            velocity.y += gravity * Time.deltaTime;
        }

        // Calculate movement
        Vector3 movement = velocity * Time.deltaTime;

        // Adjust for platform movement
        if (currentPlatform != null)
        {
            Vector3 platformDeltaPosition = currentPlatform.position - platformLastPosition;
            Quaternion platformDeltaRotation = currentPlatform.rotation * Quaternion.Inverse(platformLastRotation);

            // Move player with platform's position change
            movement += platformDeltaPosition;

            // Rotate player with platform's rotation change
            transform.rotation = platformDeltaRotation * transform.rotation;

            platformLastPosition = currentPlatform.position;
            platformLastRotation = currentPlatform.rotation;
        }

        // Move the player
        controller.Move(movement);
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);

        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleJump()
    {
        timeSinceLeftCrouch += Time.deltaTime;
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Check for steep slopes before allowing jump
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, controller.height / 2 + 0.2f))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle < maxSlopeAngle)
                {
                    if (jumpUpgrade && (isCrouching || timeSinceLeftCrouch < 1))
                    {
                        velocity.y = Mathf.Sqrt(superJumpForce * -2f * gravity);
                        isGrounded = false;
                    }
                    else
                    {
                        velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                        isGrounded = false;
                    }
                }
            }
        }
    }

    private float timeSinceLeftCrouch = 5;
    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
            controller.height = isCrouching ? 1f : 2f;
            //controller.center = new Vector3(0, isCrouching ? 0.5f : 1f, 0);
        }
        else if(Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
            controller.height = isCrouching ? 1f : 2f;
            timeSinceLeftCrouch = 0;
        }
    }

    private void HandleUpgrades()
    {
        // Dash Upgrade
        timeSinceDashPressed += Time.deltaTime;
        if (dashUpgrade && Input.GetKeyDown(KeyCode.LeftShift) && timeSinceDashPressed > dashLength + dashCooldown)
        {
            isDashing = true;
            timeSinceDashPressed = 0;
        }

        if(isDashing)
        {
            if(timeSinceDashPressed > dashLength)
            {
                Debug.Log("stopDash");
                isDashing = false;
                velocity = new Vector3(0, velocity.y, 0);
                Debug.Log(velocity);
            }
            else
            {
                velocity = transform.forward * dashSpeed;
            }
        }

        // Swim Upgrade
        if (swimUpgrade)
        {
            // Disable collisions with WaterBarrier layer
            Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("WaterBarrier"), true);

            // Check if player is in water
            isInWater = Physics.CheckSphere(transform.position, controller.radius, LayerMask.GetMask("Water"));

            if (isInWater)
            {
                // Swimming logic
                if (Input.GetKey(KeyCode.Space))
                {
                    velocity.y = swimSinkSpeed; // Move up
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    velocity.y = -swimSinkSpeed; // Move down
                }
                else
                {
                    velocity.y = -swimSinkSpeed * 0.5f; // Move down slow
                }
            }
        }
        else
        {
            // Re-enable collisions with WaterBarrier layer if upgrade is not active
            Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("WaterBarrier"), false);
        }

        // Glide Upgrade
        if (glideUpgrade && !isGrounded)
        {
            isGliding = Input.GetKey(KeyCode.Space) && velocity.y < -glideSpeed;
            if (isGliding)
            {
                velocity.y = -glideSpeed;
            }
        }
        else
        {
            isGliding = false;
        }

        // Heat and Cold Resistance
        if (heatResist)
        {
            Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("HotZone"), true);
        }
        if (coldResist)
        {
            Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("ColdZone"), true);
        }
    }

    private bool isOnClimbable;
    private void HandleClimbing()
    {       
        // Check if player is on a climbable
        float capsuleSphereCenterHeight = controller.height * 0.5f - controller.radius;
        isOnClimbable = Physics.CheckCapsule(transform.position + Vector3.up * capsuleSphereCenterHeight, transform.position - Vector3.up * capsuleSphereCenterHeight, controller.radius, LayerMask.GetMask("Climbable"));

        if (isOnClimbable)
        {
            // Swimming logic
            if (Input.GetKey(KeyCode.Space) || Input.GetAxisRaw("Vertical") > 0.1f)
            {
                velocity.y = swimSinkSpeed; // Move up
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                velocity.y = 0; // Move down
            }
            else
            {
                velocity.y = -swimSinkSpeed * 0.5f; // Move down slow
            }
        }
    }

    private void HandleInteractions()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"cut plants: {cutPlants}, smashRocks: {smashRocks}");
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 5f))
            {
                if (hit.collider.CompareTag("Plant") && cutPlants)
                {
                    Destroy(hit.collider.gameObject);
                    Debug.Log("Plant cut!");
                }
                else if (hit.collider.CompareTag("Rock") && smashRocks)
                {
                    Destroy(hit.collider.gameObject);
                    Debug.Log("Rock smashed!");
                }
            }
        }

    }
}
