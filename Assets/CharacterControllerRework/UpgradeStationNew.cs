using UnityEngine;
namespace CharacterSystem
{
    public class UpgradeStationNew : MonoBehaviour
    {
        private UpgradeManagerNew upgradeManager;

        private void Start()
        {
            upgradeManager = FindObjectOfType<UpgradeManagerNew>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                upgradeManager.ShowUpgradeUI();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                upgradeManager.HideUpgradeUI();
            }
        }
    }
}