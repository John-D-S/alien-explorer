using System.Collections.Generic;
using UnityEngine;

public class WaypointGroup : MonoBehaviour
{
    public bool refreshWaypoints;
    public bool setWaypointsOnGround;
    public bool randomizeWaypointPositions;
    
    [Header("Randomization Settings")]
    public float randomRadius = 2f;
    public float minHeight = 1f;
    public float maxHeight = 5f;
    public float heightAboveGround = 0.1f;

    [HideInInspector]
    public List<Transform> waypoints = new List<Transform>();

    private void SetWaypointOnGround(Transform waypointTransform)
    {
        RaycastHit hit;
        Vector3 startPos = waypointTransform.position + Vector3.up * 100f;
        
        if (Physics.Raycast(startPos, Vector3.down, out hit, 200f))
        {
            waypointTransform.position = hit.point + Vector3.up * heightAboveGround;
        }
        else
        {
            Debug.LogWarning($"Could not find ground for waypoint: {waypointTransform.name}");
        }
    }

    private void RandomizePosition(Transform waypointTransform)
    {
        Vector2 randomCircle = Random.insideUnitCircle * randomRadius;
        float randomHeight = Random.Range(minHeight, maxHeight);
        
        waypointTransform.position = new Vector3(
            transform.position.x + randomCircle.x,
            transform.position.y + randomHeight,
            transform.position.z + randomCircle.y
        );
    }

    private void RefreshAllWaypoints()
    {
        waypoints.Clear();
        foreach (Transform waypointTransform in transform)
        {
            waypoints.Add(waypointTransform);
            
            if (randomizeWaypointPositions)
            {
                RandomizePosition(waypointTransform);
            }
            
            if (setWaypointsOnGround)
            {
                SetWaypointOnGround(waypointTransform);
            }
        }
    }

    private void OnValidate()
    {
        // Handle refreshing waypoints
        if (refreshWaypoints)
        {
            RefreshAllWaypoints();
            refreshWaypoints = false;
        }

        // Handle setting waypoints on ground
        if (setWaypointsOnGround)
        {
            foreach (Transform waypoint in waypoints)
            {
                SetWaypointOnGround(waypoint);
            }
            setWaypointsOnGround = false;
        }

        // Handle randomization
        if (randomizeWaypointPositions)
        {
            foreach (Transform waypoint in waypoints)
            {
                RandomizePosition(waypoint);
            }
            randomizeWaypointPositions = false;
        }
    }

    // Optional: Visualize waypoints in the editor
    private void OnDrawGizmos()
    {
        if (waypoints.Count == 0) return;

        // Draw spheres at waypoint positions
        Gizmos.color = Color.blue;
        foreach (Transform waypoint in waypoints)
        {
            if (waypoint != null)
            {
                Gizmos.DrawWireSphere(waypoint.position, 0.5f);
            }
        }
    }
}