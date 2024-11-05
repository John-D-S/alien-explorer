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
        void Awake()
        {
            button = GetComponent<Button>();
        }
        public void OnPress()
        {
            teleporter.ButtonPressed();

        }
        private void OnEnable()
        {
            button.interactable = teleporter.IsRepaired;
        }
    }
}