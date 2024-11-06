using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace CharacterSystem 
{
    public class TeleportButton : MonoBehaviour
    {
        public TeleporterSystem teleporter;
        private Button button;
        public PlayerController player;
        void Awake()
        {
            button = GetComponent<Button>();

        }
        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
        }
        public void OnPress()
        {
            teleporter.ButtonPressed(player);

        }
        private void OnEnable()
        {
            button.interactable = teleporter.IsRepaired;
        }
    }
}