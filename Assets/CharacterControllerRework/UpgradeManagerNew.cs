using System;

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using TMPro;
using UnityEngine.Serialization;
namespace CharacterSystem
{
    [Serializable]
    public class Upgrade
    {
        public bool On;
        public byte Tokens;
        public Upgrade ( bool a )
        {
            On = a;
            Tokens = 0;
        }
        public Upgrade (bool a, byte b)
        {
            On = a;
            Tokens = b;
        }
        public static implicit operator bool(Upgrade up) => up.On;
    }

    [Serializable]
    public class Upgrades
    {
        public Upgrades(bool a, byte b)
        {
            Jump =  new Upgrade(a, b);
            Dash =  new Upgrade(a, b);
            Swim =  new Upgrade(a, b);
            Glide = new Upgrade(a, b);
            Heat =  new Upgrade(a, b);
            Cold =  new Upgrade(a, b);
            Cut =   new Upgrade(a, b);
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

    public class UpgradeManagerNew : MonoBehaviour
    {

        public PlayerController Player;
        public Button upgradeButtonPrefab;
        public GameObject upgradeUI;
        public int requiredTokenNumber = 4;
        private Dictionary<TokenType, Upgrade> TokenToUpgrade;
        private Dictionary<TokenType, Button> upgradeButtons = new Dictionary<TokenType, Button>();
    //    [HideInInspector]
    //    public List<Upgrade> upgrades = new List<Upgrade>
    //{
    //    new Upgrade { type = TokenType.Jump},
    //    new Upgrade { type = TokenType.Dash},
    //    new Upgrade { type = TokenType.Swim},
    //    new Upgrade { type = TokenType.Glide},
    //    new Upgrade { type = TokenType.Heat},
    //    new Upgrade { type = TokenType.Cold},
    //    new Upgrade { type = TokenType.Cut},
    //    new Upgrade { type = TokenType.Smash}
    //};

        private void Start()
        {
            if (Player == null)
            {
                Player = FindObjectOfType<PlayerController>();
            }
            TokenToUpgrade = new Dictionary<TokenType, Upgrade>
            {
                {TokenType.Jump, Player.MyUpgrades.Jump},
                {TokenType.Dash, Player.MyUpgrades.Dash},
                {TokenType.Swim , Player.MyUpgrades.Swim},
                {TokenType.Glide , Player.MyUpgrades.Glide},
                {TokenType.Heat , Player.MyUpgrades.Heat},
                {TokenType.Cold , Player.MyUpgrades.Cold},
                {TokenType.Cut , Player.MyUpgrades.Cut},
                {TokenType.Smash , Player.MyUpgrades.Smash}
            };
            InitializeUpgradeUI();
        }

        private void InitializeUpgradeUI()
        {
            for (int i=0; i<8; i++)
            {
                Button button = Instantiate(upgradeButtonPrefab, upgradeUI.transform);
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText == null)
                {
                    Debug.LogError("Button prefab does not contain a TextMeshProUGUI component");
                    continue;
                }
                
                buttonText.text = $"{(TokenType)i} ({TokenToUpgrade[(TokenType)i].Tokens}/{requiredTokenNumber})";
                int _i = i;
                button.onClick.AddListener(delegate { ActivateUpgrade(_i); });

                upgradeButtons[(TokenType)i] = button;
                UpdateButtonInteractability((TokenType)i, TokenToUpgrade[(TokenType)i]);
            }
        }

        public void CollectToken(TokenType upgradeType)
        {
            Upgrade upgrade = TokenToUpgrade[upgradeType];
            if (!upgrade.On)
            {
                upgrade.Tokens++;
                UpdateButtonText(upgradeType, upgrade);
                UpdateButtonInteractability(upgradeType, upgrade);
            }
        }

        private void UpdateButtonText(TokenType upgradeType, Upgrade upgrade)
        {
            TextMeshProUGUI buttonText = upgradeButtons[upgradeType].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"{upgradeType} ({upgrade.Tokens}/{requiredTokenNumber})";
            }
        }
        private void UpdateButtonInteractability(TokenType upgradeType, Upgrade upgrade)
        {
            upgradeButtons[upgradeType].interactable = upgrade.Tokens >= requiredTokenNumber && !upgrade.On;
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
            Upgrade upgrade = TokenToUpgrade[(TokenType)index];
            if (upgrade.Tokens >= requiredTokenNumber && !upgrade.On)
            {
                upgrade.On = true;
                UpdateButtonInteractability((TokenType)index, upgrade);

                Debug.Log(((TokenType)index).ToString() + " upgrade activated!");
            }
        }
    }
}