using UnityEngine;
namespace CharacterSystem
{
    public class UpgradeStation : MonoBehaviour
    {
        private UpgradeManager upgradeManager;

        private void Start()
        {
            upgradeManager = FindObjectOfType<UpgradeManager>();
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