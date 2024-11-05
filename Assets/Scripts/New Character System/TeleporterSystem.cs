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
        private PlayerController _player;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private MeshRenderer _meshRenderer;
        //public static List<TeleporterSystem> AllTeleporters;
        // Start is called before the first frame update
        void Awake()
        {
        }
        void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
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
            _meshCollider.sharedMesh = RepairMesh;
            _meshRenderer.material = RepairMat;
            IsRepaired = true;
        }
        public void ButtonPressed()
        {
            _player.SetMapActive(false);
            _player.Teleport(ReceiveDestination.position);
            _player.SetMovementState(PlayerController.MovementState.Normal);
        }
    }
}
