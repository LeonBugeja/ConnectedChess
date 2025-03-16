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
}