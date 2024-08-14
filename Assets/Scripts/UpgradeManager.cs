using System;

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using TMPro;
using UnityEngine.Serialization;

public enum TokenType
{
    Jump,
    Dash,
    Swim,
    Glide,
    HeatResist,
    ColdResist,
    CutPlants,
    SmashRocks
}

public class UpgradeManager : MonoBehaviour
{
    [System.Serializable]
    public class Upgrade
    {
        public TokenType type;
        public int tokensCollected;
        public bool isActivated;
    }
    
    public PlayerController playerController;
    public Button upgradeButtonPrefab;
    public GameObject upgradeUI;
    public int requiredTokenNumber = 4;
    
    private Dictionary<TokenType, Button> upgradeButtons = new Dictionary<TokenType, Button>();

    [HideInInspector]
    public List<Upgrade> upgrades = new List<Upgrade>
    {
        new Upgrade { type = TokenType.Jump},
        new Upgrade { type = TokenType.Dash},
        new Upgrade { type = TokenType.Swim},
        new Upgrade { type = TokenType.Glide},
        new Upgrade { type = TokenType.HeatResist},
        new Upgrade { type = TokenType.ColdResist},
        new Upgrade { type = TokenType.CutPlants},
        new Upgrade { type = TokenType.SmashRocks}
    };

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        InitializeUpgradeUI();
    }

    private void InitializeUpgradeUI()
    {
        foreach (Upgrade upgrade in upgrades)
        {
            Button button = Instantiate(upgradeButtonPrefab, upgradeUI.transform);
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText == null)
            {
                Debug.LogError("Button prefab does not contain a TextMeshProUGUI component");
                continue;
            }

            buttonText.text = $"{upgrade.type} ({upgrade.tokensCollected}/{requiredTokenNumber})";

            button.onClick.AddListener(() => ActivateUpgrade(upgrades.IndexOf(upgrade)));

            upgradeButtons[upgrade.type] = button;
            UpdateButtonInteractability(upgrade);
        }
    }

    public void CollectToken(TokenType upgradeType)
    {
        Upgrade upgrade = upgrades.Find(u => u.type == upgradeType);
        if (upgrade != null && !upgrade.isActivated)
        {
            upgrade.tokensCollected++;
            UpdateButtonText(upgrade);
            UpdateButtonInteractability(upgrade);
        }
    }

    private void UpdateButtonText(Upgrade upgrade)
    {
        TextMeshProUGUI buttonText = upgradeButtons[upgrade.type].GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = $"{upgrade.type} ({upgrade.tokensCollected}/{requiredTokenNumber})";
        }
    }
    private void UpdateButtonInteractability(Upgrade upgrade)
    {
        upgradeButtons[upgrade.type].interactable = upgrade.tokensCollected >= requiredTokenNumber && !upgrade.isActivated;
    }

    public void ShowUpgradeUI()
    {
        upgradeUI.SetActive(true);
        //Time.timeScale = 0f; // Pause time
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideUpgradeUI()
    {
        upgradeUI.SetActive(false);
        //Time.timeScale = 1f; // Resume time
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ActivateUpgrade(int index)
    {
        Upgrade upgrade = upgrades[index];
        if (upgrade.tokensCollected >= requiredTokenNumber && !upgrade.isActivated)
        {
            upgrade.isActivated = true;
            UpdateButtonInteractability(upgrade);

            switch (upgrade.type)
            {
                case TokenType.Jump:
                    playerController.jumpUpgrade = true;
                    break;
                case TokenType.Dash:
                    playerController.dashUpgrade = true;
                    break;
                case TokenType.Swim:
                    playerController.swimUpgrade = true;
                    break;
                case TokenType.Glide:
                    playerController.glideUpgrade = true;
                    break;
                case TokenType.HeatResist:
                    playerController.heatResist = true;
                    break;
                case TokenType.ColdResist:
                    playerController.coldResist = true;
                    break;
                case TokenType.CutPlants:
                    playerController.cutPlants = true;
                    break;
                case TokenType.SmashRocks:
                    playerController.smashRocks = true;
                    break;
            }

            Debug.Log(upgrade.type.ToString() + " upgrade activated!");
        }
    }
}