using UnityEngine;
namespace CharacterSystem 
{
    public class TokenNew : MonoBehaviour
    {
        public TokenType upgradeType;
        private UpgradeManagerNew upgradeManager;

        private void Start()
        {
            upgradeManager = FindObjectOfType<UpgradeManagerNew>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                upgradeManager.CollectToken(upgradeType);
                Destroy(gameObject);
            }
        }
    }
}