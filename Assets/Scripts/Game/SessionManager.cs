using Unity.Netcode;
using System.Collections;
using UnityEngine;
using UnityChess;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;
using TMPro;
using Firebase.Storage;
using Firebase.Extensions;
using System;
using System.IO;

public class SessionManager : MonoBehaviour
{
    public NetworkManager NetworkManager;

    public NetworkStringManager networkStringManager;
    public InputField BoardState;

    public GameObject Board;

    public TMP_InputField SessionCodeInput;
    public TMP_Text ErrorMessageText;
    public Button HostButton;
    public Button JoinButton;
    public Button SaveButton;
    public Button RestoreButton;
    public Button ResignButton;

    private bool resigned = false;
    private string GUID = Guid.NewGuid().ToString();

    private Coroutine connectionTimeoutCoroutine;

    private void Start()
    {
        if (HostButton != null && JoinButton != null)
        {
            HostButton.onClick.AddListener(StartHost);
            JoinButton.onClick.AddListener(JoinAsClient);
            SaveButton.onClick.AddListener(SaveGame);
            RestoreButton.onClick.AddListener(RestoreGame);
            ResignButton.onClick.AddListener(ResignFromMatch);
        }

        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (networkStringManager != null && BoardState != null)
        {
            BoardState.onValueChanged.AddListener(OnBoardStateValueChanged);

            networkStringManager.OnSharedStringChanged += HandleSharedStringChanged;
        }
    }

    private void StartHost()
    {
        if (NetworkManager != null)
        {
            Debug.Log("Starting Host...");
            NetworkManager.StartHost();
        }
    }

    private void JoinAsClient()
    {
        string sessionCode = SessionCodeInput.text.Trim();

        if (string.IsNullOrEmpty(sessionCode))
        {
            DisplayErrorMessage("Session code cannot be empty.");
            return;
        }

        if (!IsValidSessionCode(sessionCode))
        {
            DisplayErrorMessage("Invalid session code. Please try again.");
            return;
        }

        Debug.Log($"Joining session with code: {sessionCode}");
        NetworkManager.GetComponent<UnityTransport>().SetConnectionData(sessionCode, 7777); //set the IP/Port

        //rotate the board so player black sees the black pieces at the bottom
        GameManager.Instance.StartNewGame();
        Board.transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));

        NetworkManager.StartClient();

        //update chess pieces for rejoining players
        //GameManager.Instance.LoadGame(networkStringManager.SharedString.Value.ToString());
    }

    private void ResignFromMatch()
    {
        if (NetworkManager != null)
        {
            resigned = true;
            NetworkManager.Singleton.Shutdown();
        }
    }

    private void SaveGame()
    {
        string session = BoardState.text;

        byte[] data = System.Text.Encoding.UTF8.GetBytes(session);

        string fileName = $"session_{GUID}.txt";

        StorageReference storageRef = FirebaseStorage.DefaultInstance.RootReference.Child(fileName);

        storageRef.PutBytesAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"Upload failed: {task.Exception?.ToString()}");
                    DisplayErrorMessage("Failed to save game. Please try again.");
                }
            });
    }

    private async void RestoreGame()
    {
        string fileName = $"session_{GUID}.txt";

        try
        {
            FirebaseStorage storage = FirebaseStorage.DefaultInstance;
            StorageReference storageRef = storage.GetReferenceFromUrl("gs://connected-chess.firebasestorage.app/");
            string localDir = $"{Application.persistentDataPath}/Saves";

            StorageReference sessionRef = storageRef.Child(fileName);

            if (!Directory.Exists(localDir))
            {
                Directory.CreateDirectory(localDir);
            }

            string localFilePath = $"{localDir}/{fileName}";
            await sessionRef.GetFileAsync(localFilePath);

            string session = File.ReadAllText(localFilePath);
            BoardState.text = session;
            GameManager.Instance.LoadGame(session);
        }
        catch (System.Exception e)
        {
            DisplayErrorMessage($"Error restoring game: {e.Message}");
        }
    }


    private bool IsValidSessionCode(string sessionCode)
    {
        System.Net.IPAddress ipAddress;
        return System.Net.IPAddress.TryParse(sessionCode, out ipAddress);
    }

    private void DisplayErrorMessage(string message)
    {
        ErrorMessageText.text = message;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (networkStringManager != null)
        {
            networkStringManager.OnSharedStringChanged -= HandleSharedStringChanged;
        }

        if (BoardState != null)
        {
            BoardState.onValueChanged.RemoveListener(OnBoardStateValueChanged);
        }

        Debug.LogWarning($"Client {clientId} disconnected.");

        if (!NetworkManager.IsServer && clientId == NetworkManager.LocalClientId && !resigned)
        {
            HandleConnectionFailure();
        }

        resigned = false;
    }

    private void HandleConnectionFailure()
    {
        Debug.LogError("Connection to the server failed. Please check your session code and try again.");
        DisplayErrorMessage("Connection failed. Please try again.");
    }

    private void OnBoardStateValueChanged(string newValue)
    {
        networkStringManager.UpdateSharedString(newValue);
    }

    private void HandleSharedStringChanged(string newValue, bool isLocalChange)
    {
        if (!isLocalChange && BoardState.text != newValue)
        {
            BoardState.onValueChanged.RemoveListener(OnBoardStateValueChanged);

            BoardState.text = newValue;

            BoardState.onValueChanged.AddListener(OnBoardStateValueChanged);
        }
    }
}