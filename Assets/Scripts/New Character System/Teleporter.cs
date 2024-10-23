using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    public class Teleporter : MonoBehaviour
    {
        public enum DestMode
        {
            ToPosition,
            ToObject
        }
        public DestMode Mode;
        public Vector3 DestinationPos;
        public GameObject DestinationObj;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                switch (Mode)
                {
                    case DestMode.ToPosition:
                        other.gameObject.GetComponent<PlayerController>().Teleport(DestinationPos);
                        break;
                    case DestMode.ToObject:
                        other.gameObject.GetComponent<PlayerController>().Teleport(DestinationObj.transform.position);
                        break;

                }
                

            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawSphere(transform.position, 1);
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            switch (Mode)
            {
                case DestMode.ToPosition:
                    Gizmos.DrawSphere(DestinationPos, 1);
                    Gizmos.DrawLine(transform.position, DestinationPos);
                    break;
                case DestMode.ToObject:
                    Gizmos.DrawSphere(DestinationObj.transform.position, 1);
                    Gizmos.DrawLine(transform.position, DestinationObj.transform.position);
                    break;

            }
        }
    }
}