using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Storage;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class DLCManager : NetworkBehaviour
{
    public GameObject storeGUI;

    private int balance = 150;
    public TMP_Text CreditsText;

    public Button buyGreenDLCButton;
    public Button buyMixedDLCButton;

    public Sprite defaultAvatar;

    public Image localPlayerAvatarImage;
    public Image opponentAvatarImage;

    private string isEquipped = "";

    void Start()
    {
        buyGreenDLCButton.onClick.AddListener(() => BuyDLC("Green"));
        buyMixedDLCButton.onClick.AddListener(() => BuyDLC("Mixed"));

        LoadEquippedAvatar();

        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeft;
    }

    void Update()
    {
        CreditsText.text = balance.ToString();
    }

    public void enableDLCui()
    {
        storeGUI.SetActive(true);

        TMP_Text greenDlcButtonText = buyGreenDLCButton.GetComponentInChildren<TMP_Text>();
        TMP_Text mixedDlcButtonText = buyMixedDLCButton.GetComponentInChildren<TMP_Text>();

        if (PlayerPrefs.GetString("OwnedSkins").Contains("Green"))
        {
            greenDlcButtonText.text = "Owned";
        }

        if (PlayerPrefs.GetString("OwnedSkins").Contains("Mixed"))
        {
            mixedDlcButtonText.text = "Owned";
        }
    }

    public void disableDLCui()
    {
        storeGUI.SetActive(false);
    }

    private async Task DownloadDLC(string color)
    {
        try
        {
            FirebaseStorage storage = FirebaseStorage.DefaultInstance;
            StorageReference storageRef = storage.GetReferenceFromUrl("gs://connected-chess.firebasestorage.app/");
            string localDir = $"{Application.persistentDataPath}/DLCs";

            StorageReference avatarRef = storageRef.Child($"{color}Pawn.png");

            if (!Directory.Exists(localDir))
            {
                Directory.CreateDirectory(localDir);
            }

            string localFilePath = $"{localDir}/{color}Pawn.png";
            await avatarRef.GetFileAsync(localFilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error downloading Avatar: {e.Message}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyDLCEquippedServerRpc(string color, ulong clientId)
    {
        NotifyDLCEquippedClientRpc(color, clientId);
    }

    [ClientRpc]
    private void NotifyDLCEquippedClientRpc(string color, ulong clientId)
    {
        Debug.Log($"DLC '{color}' avatar has been equipped by player with ID: {clientId}");

        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            UpdateOpponentAvatar(color);
        }
    }

    private async void BuyDLC(string color)
    {
        int dlcPrice = 75;

        if (!PlayerPrefs.GetString("OwnedSkins").Contains(color) && balance >= dlcPrice)
        {
            balance -= dlcPrice;

            Debug.Log("Downloading Avatar...");
            await DownloadDLC(color);
            Debug.Log("Avatar downloaded!");

            string currentlyOwned = PlayerPrefs.GetString("OwnedSkins");
            PlayerPrefs.SetString("OwnedSkins", $"{color}, {currentlyOwned}");

            PlayerPrefs.SetString("Equipped", color);

            AnalyticsManager.Instance.LogDLCPurchase(color + "ColoredAvatar", dlcPrice);

            UpdateLocalPlayerAvatar(color);
            NotifyDLCEquippedServerRpc(color, NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            PlayerPrefs.SetString("Equipped", color);
            UpdateLocalPlayerAvatar(color);
            NotifyDLCEquippedServerRpc(color, NetworkManager.Singleton.LocalClientId);
        }

        PlayerPrefs.Save();
    }

    private void LoadEquippedAvatar()
    {
        string equippedColor = PlayerPrefs.GetString("Equipped");
        if (!string.IsNullOrEmpty(equippedColor))
        {
            UpdateLocalPlayerAvatar(equippedColor);
            NotifyDLCEquippedServerRpc(equippedColor, NetworkManager.Singleton.LocalClientId);
        }
    }

    private void UpdateLocalPlayerAvatar(string color)
    {
        string localFilePath = $"{Application.persistentDataPath}/DLCs/{color}Pawn.png";

        if (File.Exists(localFilePath))
        {
            byte[] imageData = File.ReadAllBytes(localFilePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);

            localPlayerAvatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
        else
        {
            Debug.LogError($"Avatar file for color '{color}' not found!");
        }
    }

    private void UpdateOpponentAvatar(string color)
    {
        string localFilePath = $"{Application.persistentDataPath}/DLCs/{color}Pawn.png";

        if (File.Exists(localFilePath))
        {
            byte[] imageData = File.ReadAllBytes(localFilePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);

            opponentAvatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
        else
        {
            Debug.LogError($"Avatar file for color '{color}' not found!");
        }
    }

    private void OnPlayerJoined(ulong clientId)
    {
        string equippedColor = PlayerPrefs.GetString("Equipped");
        if (!string.IsNullOrEmpty(equippedColor))
        {
            NotifyDLCEquippedServerRpc(equippedColor, NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnPlayerLeft(ulong clientId)
    {
        opponentAvatarImage.sprite = defaultAvatar;
    }
}