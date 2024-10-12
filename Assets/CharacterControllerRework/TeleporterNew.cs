using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleporterNew : MonoBehaviour
{
    public Vector3 teleportDestination;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.gameObject.GetComponent<CharController>().Teleport(teleportDestination);

        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawSphere(teleportDestination, 1);
        Gizmos.DrawLine(transform.position, teleportDestination);
    }
}
