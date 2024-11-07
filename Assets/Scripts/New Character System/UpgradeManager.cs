using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;

namespace CharacterSystem
{
    [Serializable]
    public class Upgrades
    {
        [Serializable]
        public class Upgrade
        {
            public bool On;
            public byte Tokens;
            public Upgrade(bool a)
            {
                On = a;
                Tokens = 0;
            }
            public Upgrade(bool a, byte b)
            {
                On = a;
                Tokens = b;
            }
            public static implicit operator bool(Upgrade up) => up.On;
        }

        public Upgrades(bool a, byte b)
        {
            Jump = new Upgrade(a, b);
            Dash = new Upgrade(a, b);
            Swim = new Upgrade(a, b);
            Glide = new Upgrade(a, b);
            Heat = new Upgrade(a, b);
            Cold = new Upgrade(a, b);
            Cut = new Upgrade(a, b);
            Smash = new Upgrade(a, b);
        }
        public Upgrade Jump;
        public Upgrade Dash;
        public Upgrade Swim;
        public Upgrade Glide;
        public Upgrade Heat;
        public Upgrade Cold;
        public Upgrade Cut;
        public Upgrade Smash;
    }

    public enum TokenType
    {
        Jump,
        Dash,
        Swim,
        Glide,
        Heat,
        Cold,
        Cut,
        Smash
    }

    public class UpgradeManager : MonoBehaviour
    {
        public PlayerController Player;
        public Button upgradeButtonPrefab;
        public GameObject upgradeUI;
        
        // Replace single requirement with dictionary
        [SerializeField]
        private int[] tokenRequirements = new int[8] { 4, 4, 4, 4, 4, 4, 4, 4 };
        private Dictionary<TokenType, int> requiredTokenNumbers = new Dictionary<TokenType, int>();
        
        private Dictionary<TokenType, Upgrades.Upgrade> TokenToUpgrade;
        private Dictionary<TokenType, Button> upgradeButtons = new Dictionary<TokenType, Button>();

        private void Start()
        {
            if (Player == null)
            {
                Player = FindObjectOfType<PlayerController>();
            }

            // Initialize token requirements
            for (int i = 0; i < 8; i++)
            {
                requiredTokenNumbers[(TokenType)i] = tokenRequirements[i];
            }

            TokenToUpgrade = new Dictionary<TokenType, Upgrades.Upgrade>
            {
                {TokenType.Jump, Player.MyUpgrades.Jump},
                {TokenType.Dash, Player.MyUpgrades.Dash},
                {TokenType.Swim, Player.MyUpgrades.Swim},
                {TokenType.Glide, Player.MyUpgrades.Glide},
                {TokenType.Heat, Player.MyUpgrades.Heat},
                {TokenType.Cold, Player.MyUpgrades.Cold},
                {TokenType.Cut, Player.MyUpgrades.Cut},
                {TokenType.Smash, Player.MyUpgrades.Smash}
            };
            InitializeUpgradeUI();
        }

        private void InitializeUpgradeUI()
        {
            for (int i = 0; i < 8; i++)
            {
                Button button = Instantiate(upgradeButtonPrefab, upgradeUI.transform);
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText == null)
                {
                    Debug.LogError("Button prefab does not contain a TextMeshProUGUI component");
                    continue;
                }

                TokenType tokenType = (TokenType)i;
                buttonText.text = $"{tokenType} ({TokenToUpgrade[tokenType].Tokens}/{requiredTokenNumbers[tokenType]})";
                int _i = i;
                button.onClick.AddListener(delegate { ActivateUpgrade(_i); });

                upgradeButtons[tokenType] = button;
                UpdateButtonInteractability(tokenType, TokenToUpgrade[tokenType]);
            }
        }

        public void CollectToken(TokenType upgradeType)
        {
            Upgrades.Upgrade upgrade = TokenToUpgrade[upgradeType];
            if (!upgrade.On)
            {
                upgrade.Tokens++;
                UpdateButtonText(upgradeType, upgrade);
                UpdateButtonInteractability(upgradeType, upgrade);
            }
        }

        private void UpdateButtonText(TokenType upgradeType, Upgrades.Upgrade upgrade)
        {
            TextMeshProUGUI buttonText = upgradeButtons[upgradeType].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"{upgradeType} ({upgrade.Tokens}/{requiredTokenNumbers[upgradeType]})";
            }
        }

        private void UpdateButtonInteractability(TokenType upgradeType, Upgrades.Upgrade upgrade)
        {
            upgradeButtons[upgradeType].interactable = upgrade.Tokens >= requiredTokenNumbers[upgradeType] && !upgrade.On;
        }

        public void ShowUpgradeUI()
        {
            upgradeUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void HideUpgradeUI()
        {
            upgradeUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ActivateUpgrade(int index)
        {
            TokenType tokenType = (TokenType)index;
            Upgrades.Upgrade upgrade = TokenToUpgrade[tokenType];
            if (upgrade.Tokens >= requiredTokenNumbers[tokenType] && !upgrade.On)
            {
                upgrade.On = true;
                UpdateButtonInteractability(tokenType, upgrade);
                Debug.Log($"{tokenType} upgrade activated!");
            }
        }
    }
}