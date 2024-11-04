using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Creature : MonoBehaviour
{
    // Creature data scriptable object
    public CreatureData creatureData;
    
    public Transform[] waypoints;
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

    private void Start()
    {
        // Get and setup the sprite renderer from the child object
        spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = creatureData.creatureSprite;
        
        mainCameraTransform = Camera.main.transform;
        
        // Initialize movement based on locomotion type
        if (creatureData.locomotionType == CreatureData.Locomotion.Walking)
        {
            navAgent = GetComponent<NavMeshAgent>();
            StartCoroutine(WalkingBehavior());
        }
        else
        {
            StartCoroutine(SwimmingFlyingBehavior());
        }
    }

    private void LateUpdate()
    {
        // Make sprite face camera
        Vector3 directionToCamera = mainCameraTransform.position - spriteTransform.position;
        directionToCamera.y = 0; // Keep the sprite vertical
        spriteTransform.rotation = Quaternion.LookRotation(-directionToCamera);

        // Determine movement direction relative to camera
        Vector3 moveDirection;
        if (creatureData.locomotionType == CreatureData.Locomotion.Walking)
        {
            moveDirection = navAgent.velocity;
        }
        else if (currentWaypointIndex >= 0)
        {
            moveDirection = (waypoints[currentWaypointIndex].position - transform.position).normalized;
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

    private IEnumerator WalkingBehavior()
    {
        while (true)
        {
            // Choose random waypoint
            int newIndex = Random.Range(0, waypoints.Length);
            currentWaypointIndex = newIndex;
            
            // Move to waypoint
            navAgent.SetDestination(waypoints[currentWaypointIndex].position);
            
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
            int newIndex;
            do
            {
                newIndex = Random.Range(0, waypoints.Length);
            } while (newIndex == currentWaypointIndex);
            
            Vector3 target = waypoints[newIndex].position;
            
            // Check for obstacles with spherecast
            if (!Physics.SphereCast(transform.position, 0.5f, (target - transform.position).normalized, 
                out RaycastHit hit, Vector3.Distance(transform.position, target)))
            {
                currentWaypointIndex = newIndex;
                isMoving = true;
                
                // Lerp to new position
                float startTime = Time.time;
                Vector3 startPos = transform.position;
                
                while (Time.time - startTime < 1f)
                {
                    float t = (Time.time - startTime) * moveSpeed;
                    transform.position = Vector3.Lerp(startPos, target, t);
                    yield return null;
                }
                
                isMoving = false;
                
                // For flying creatures, wait longer if on ground
                if (creatureData.locomotionType == CreatureData.Locomotion.Flying && 
                    Mathf.Approximately(transform.position.y, 0f))
                {
                    yield return new WaitForSeconds(Random.Range(10f, 30f));
                }
                else
                {
                    yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
                }
            }
            yield return new WaitForSeconds(1f); // Wait before trying again if blocked
        }
    }

    public void OnScanned()
    {
        scanMask.SetActive(false);
    }
}