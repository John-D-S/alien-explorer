using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEditor;

public class Creature : MonoBehaviour
{
    // Dummy checkbox for validation
    [SerializeField] private bool validateTrigger;
    
    // Read-only field to display upgrade type
    [SerializeField] private string upgradeType;
    
    [SerializeField] private CreatureData _creatureData;
    public CreatureData creatureData 
    {
        get => _creatureData;
        set 
        {
            _creatureData = value;
            if (value != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = value.creatureSprite;
            }
        }
    }

    public bool staticCreature = false;
    [SerializeField] private WaypointGroup waypointGroup;
    public GameObject scanMask;
    public Transform spriteTransform;
    
    // Movement settings
    [SerializeField] private float minWaitTime = 5f;
    [SerializeField] private float maxWaitTime = 15f;
    [SerializeField] private float moveSpeed = 5f;
    
    // Component references
    private NavMeshAgent navAgent;
    private SpriteRenderer spriteRenderer;
    private int currentWaypointIndex = -1;
    private bool isMoving = false;
    private Transform mainCameraTransform;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        validateTrigger = false;
        // Update sprite when creature data is changed in inspector
        if (_creatureData != null)
        {
            // Update GameObject name to match creature data if different
            if (gameObject.name != _creatureData.name)
            {
                gameObject.name = _creatureData.name;
            }
            
            // Update sprite if available
            if (spriteTransform != null)
            {
                spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = _creatureData.creatureSprite;
                }
            }

            // Update the upgrade type display
            upgradeType = _creatureData.ability.ToString();
        }
    }
    #endif

    private void Start()
    {
        if (!staticCreature && (waypointGroup == null || waypointGroup.waypoints.Count == 0))
        {
            Debug.LogError($"Moving Creature {gameObject.name} is missing WaypointGroup or has no waypoints!");
            enabled = false;
            return;
        }

        // Get and setup the sprite renderer from the child object
        spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        if (creatureData != null)
        {
            spriteRenderer.sprite = creatureData.creatureSprite;
        }
        
        mainCameraTransform = Camera.main.transform;
        
        // Check if this creature type has been scanned before
        Scanner scanner = FindObjectOfType<Scanner>();
        if (scanner != null && scanner.HasScannedCreature(creatureData) || staticCreature)
        {
            scanMask.SetActive(false);
        }

        // Get the NavMeshAgent component
        navAgent = GetComponent<NavMeshAgent>();
    
        // Enable/disable NavMeshAgent based on locomotion type
        if (navAgent != null)
        {
            navAgent.enabled = (creatureData.locomotionType == CreatureData.Locomotion.Walking);
        }
        
        if(!staticCreature)
        {    
            // Initialize movement based on locomotion type
            if (creatureData.locomotionType == CreatureData.Locomotion.Walking)
            {
                StartCoroutine(WalkingBehavior());
            }
            else
            {
                StartCoroutine(SwimmingFlyingBehavior());
            }
        }
    }

    private void LateUpdate()
    {
        if (mainCameraTransform != null && spriteTransform != null)
        {
            // Make sprite face camera
            Vector3 directionToCamera = mainCameraTransform.position - spriteTransform.position;
            directionToCamera.y = 0; // Keep the sprite vertical
            spriteTransform.rotation = Quaternion.LookRotation(-directionToCamera);

            // Determine movement direction relative to camera
            Vector3 moveDirection;
            if (creatureData.locomotionType == CreatureData.Locomotion.Walking && navAgent != null)
            {
                moveDirection = navAgent.velocity;
            }
            else if (currentWaypointIndex >= 0 && currentWaypointIndex < waypointGroup.waypoints.Count)
            {
                moveDirection = (waypointGroup.waypoints[currentWaypointIndex].position - transform.position).normalized;
            }
            else
            {
                return;
            }

            // Project movement direction onto camera's right vector to determine if moving left or right
            float dotProduct = Vector3.Dot(moveDirection.normalized, mainCameraTransform.right);
            
            // Flip sprite based on movement direction relative to camera view
            if (Mathf.Abs(dotProduct) > 0.1f) // Only flip if there's significant horizontal movement
            {
                spriteRenderer.flipX = dotProduct > 0; // Flip if moving right relative to camera
            }
        }
    }
    
    private bool IsWaypointOnGround(Vector3 position)
    {
        // Cast a short ray downward from slightly above the position
        Vector3 rayStart = position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 2f))
        {
            return true;
        }
        return false;
    }

    private IEnumerator WalkingBehavior()
    {
        while (true)
        {
            // Choose random waypoint
            int newIndex = Random.Range(0, waypointGroup.waypoints.Count);
            currentWaypointIndex = newIndex;
            
            // Move to waypoint
            navAgent.SetDestination(waypointGroup.waypoints[currentWaypointIndex].position);
            
            // Wait until reaching destination
            while (navAgent.pathStatus != NavMeshPathStatus.PathComplete)
                yield return null;
                
            // Random wait at waypoint
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator SwimmingFlyingBehavior()
    {
        while (true)
        {
            // Choose random waypoint (not current)
            int newIndex = Random.Range(0, waypointGroup.waypoints.Count);
            while (newIndex == currentWaypointIndex && waypointGroup.waypoints.Count > 1)
            {
                newIndex = Random.Range(0, waypointGroup.waypoints.Count);
            }
            
            Vector3 target = waypointGroup.waypoints[newIndex].position;
            
            // Check for obstacles with spherecast
            if (!Physics.SphereCast(transform.position, 0.5f, (target - transform.position).normalized, 
                out RaycastHit hit, Vector3.Distance(transform.position, target)))
            {
                currentWaypointIndex = newIndex;
                isMoving = true;
                
                // Lerp to new position
                float startTime = Time.time;
                float journeyLength = Vector3.Distance(transform.position, target);
                float duration = journeyLength / moveSpeed;
                Vector3 startPos = transform.position;
                float elapsedTime = 0f;
                
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / duration);
                    transform.position = Vector3.Lerp(startPos, target, t);
                    yield return null;
                }

                isMoving = false;
                
                // For flying creatures, wait longer if on ground
                if (creatureData.locomotionType == CreatureData.Locomotion.Flying && IsWaypointOnGround(transform.position))
                {
                    yield return new WaitForSeconds(Random.Range(10f, 30f));
                }
                else
                {
                    yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
                }
            }
            yield return new WaitForSeconds(0.1f); // Wait before trying again if blocked
        }
    }

    public void OnScanned()
    {
        scanMask.SetActive(false);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Creature))]
public class CreatureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Creature creature = (Creature)target;
        
        // Draw the default inspector excluding our custom fields
        DrawPropertiesExcluding(serializedObject, "upgradeType");
        
        // Draw the read-only upgrade type field
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("Upgrade Type", serializedObject.FindProperty("upgradeType").stringValue);
        EditorGUI.EndDisabledGroup();
        
        // Apply any changes
        serializedObject.ApplyModifiedProperties();
    }
}
#endif