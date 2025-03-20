using Unity.Netcode;
using System.Collections;
using UnityEngine;
using UnityChess;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;
using TMPro;

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

    private Coroutine connectionTimeoutCoroutine;

    private void Start()
    {
        if (HostButton != null && JoinButton != null)
        {
            HostButton.onClick.AddListener(StartHost);
            JoinButton.onClick.AddListener(JoinAsClient);
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
            GameManager.Instance.StartNewGame();
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
        GameManager.Instance.StartNewGame();
        //rotate the board so player black sees the black pieces at the bottom
        Board.transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
        NetworkManager.StartClient();

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

        if (!NetworkManager.IsServer && clientId == NetworkManager.LocalClientId)
        {
            HandleConnectionFailure();
        }
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