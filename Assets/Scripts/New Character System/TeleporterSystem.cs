using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem 
{
    public class TeleporterSystem : MonoBehaviour, IUsable
    {
        public bool IsRepaired;
        public Mesh RepairMesh;
        public Material RepairMat;
        public Transform ReceiveDestination;
        public PlayerController _player;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        //public static List<TeleporterSystem> AllTeleporters;
        // Start is called before the first frame update
        void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

        }

        public void Interact(PlayerController player)
        {
            if (IsRepaired){
                _player = player;
                _player.SetMapActive(true);
                _player.SetMovementState(PlayerController.MovementState.InMenu);
            }
            else
            {
                Repair();
            }
        }
        void Repair()
        {
            _meshFilter.mesh = RepairMesh;
            _meshRenderer.material = RepairMat;
            IsRepaired = true;
        }
        public void ButtonPressed(PlayerController player)
        {
            player.SetMapActive(false);
            player.Teleport(ReceiveDestination.position);
            player.SetMovementState(PlayerController.MovementState.Normal);
        }
    }
}
