using UnityEngine;

public class PlayerController: MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpForce = 1f;
    public float crouchSpeed = 2.5f;
    public float gravity = -9.81f;

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
    private Camera playerCamera;
    private float verticalRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching;
    private bool isGliding;
    private bool isInWater;
    private bool isDashing;
    private float timeSinceDashPressed = 5;
    
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
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
        HandleInteractions();
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z).normalized;

        float speed = isCrouching ? crouchSpeed : (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed);

        if (isGliding)
        {
            speed *= glideMovementMultiplier;
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        velocity.y += (((isGrounded && !isInWater) ? -1000 : 0) + gravity) * Time.deltaTime;
        controller.Move(move * (speed * Time.deltaTime) + velocity * Time.deltaTime);
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
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            isGrounded = false;
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
        // Jump Upgrade
        timeSinceLeftCrouch += Time.deltaTime;
        if (jumpUpgrade && Input.GetButtonDown("Jump") && (isCrouching || timeSinceLeftCrouch < 1))
        {
            velocity.y = Mathf.Sqrt(superJumpForce * -2f * gravity);
            isGrounded = false;
        }

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
