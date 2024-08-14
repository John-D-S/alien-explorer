using UnityEngine;

public class Token : MonoBehaviour
{
    public TokenType upgradeType;
    private UpgradeManager upgradeManager;

    private void Start()
    {
        upgradeManager = FindObjectOfType<UpgradeManager>();
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
