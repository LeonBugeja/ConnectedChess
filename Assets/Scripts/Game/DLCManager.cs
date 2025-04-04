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

    public Button buyBlueDLCButton;
    public Button buyRedDLCButton;

    private string isEquipped = "";

    public Transform Board;

    void Start()
    {
        buyBlueDLCButton.onClick.AddListener(() => BuyDLC("Blue"));
        buyRedDLCButton.onClick.AddListener(() => BuyDLC("Red"));
    }

    void Update()
    {
        CreditsText.text = balance.ToString();

        foreach (Transform Square in Board.transform)
        {
            if (Square.childCount > 0)
            {
                if (Board.transform.childCount != 0)
                {
                    string ownerColor = IsHost ? "White" : "Black";

                    foreach (Transform boardSquare in Board)
                    {
                        foreach (Transform piece in boardSquare)
                        {
                            if (piece.name.Contains(ownerColor))
                            {
                                Renderer renderer = piece.GetComponent<Renderer>();
                                if (renderer != null)
                                {
                                    string matPath = $"PieceSets/Marble/DLC/{PlayerPrefs.GetString("Equipped")}Pieces/{PlayerPrefs.GetString("Equipped")} {piece.name.Replace("(Clone)", "").Trim().Split(' ')[1]}";
                                    Material equippedMaterial = Resources.Load<Material>(matPath);

                                    renderer.material = equippedMaterial;
                                }
                            }
                        }
                    }

                    isEquipped = PlayerPrefs.GetString("Equipped");
                }
            }
        }
    }

    public void enableDLCui()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            storeGUI.SetActive(true);

            TMP_Text blueDlcButtonText = buyBlueDLCButton.GetComponentInChildren<TMP_Text>();
            TMP_Text redDlcButtonText = buyRedDLCButton.GetComponentInChildren<TMP_Text>();

            if (PlayerPrefs.GetString("OwnedSkins").Contains("Blue"))
            {
                blueDlcButtonText.text = "Owned";
            }

            if (PlayerPrefs.GetString("OwnedSkins").Contains("Red"))
            {
                redDlcButtonText.text = "Owned";
            }
        }
        else
        {
            Debug.Log("Host/Join a game to go online and access the DLC Store");
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

            string[] pieceTypes = { "Bishop", "King", "Knight", "Pawn", "Queen", "Rook" };

            foreach (string pieceType in pieceTypes)
            {
                StorageReference materialRef = storageRef.Child($"dlc/{color.ToLower()}Pieces/{color} {pieceType}.mat");

                string localDir = $"Assets/Resources/PieceSets/Marble/DLC/{color}Pieces";
                if (!Directory.Exists(localDir))
                {
                    Directory.CreateDirectory(localDir);
                }

                string localFilePath = $"{localDir}/{color} {pieceType}.mat";
                await materialRef.GetFileAsync(localFilePath);
                Debug.Log($"Downloaded: {pieceType} material");
            }

            #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
            #endif

            Debug.Log($"All {color} piece materials downloaded successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error downloading DLC: {e.Message}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyDLCBoughtServerRpc(string color)
    {
        NotifyDLCBoughtClientRpc(color);
    }

    [ClientRpc]
    private void NotifyDLCBoughtClientRpc(string color)
    {
        Debug.Log($"DLC '{color}' has been bought by another player!");
    }

    private async void BuyDLC(string color)
    {
        int dlcPrice = 75;

        if (!PlayerPrefs.GetString("OwnedSkins").Contains(color) && balance >= dlcPrice)
        {
            balance -= dlcPrice;

            Debug.Log("Starting DLC download...");
            await DownloadDLC(color);
            Debug.Log("DLC download completed!");

            string currentlyOwned = PlayerPrefs.GetString("OwnedSkins");
            PlayerPrefs.SetString("OwnedSkins", $"{color}, {currentlyOwned}");

            PlayerPrefs.SetString("Equipped", color);

            NotifyDLCBoughtServerRpc(color);
        }
    }
}