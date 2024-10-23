using UnityEngine;

namespace OldCharacterSystem
{
    public class Teleporter : MonoBehaviour
    {
        public Vector3 teleportDestination;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                other.GetComponent<CharacterController>().enabled = false;
                other.transform.position = teleportDestination;
                other.GetComponent<CharacterController>().enabled = true;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(teleportDestination, 1);
            Gizmos.DrawLine(transform.position, teleportDestination);
        }
    }
}