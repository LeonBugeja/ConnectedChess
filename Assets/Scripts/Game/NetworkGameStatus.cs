using Unity.Netcode;
using UnityEngine;
using System;
using Unity.Collections;
using UnityEngine.UI;
using System.Collections;

public class NetworkGameStatus : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> GameStatus = new NetworkVariable<FixedString32Bytes>(
        string.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public Text _localStatus;
    private string _previousText;

    void Start()
    {
        if (_localStatus != null)
        {
            _previousText = _localStatus.text;
        }
    }

    void Update()
    {
        if (_localStatus != null && _localStatus.text != _previousText)
        {
            OnTextChanged(_previousText, _localStatus.text);

            _previousText = _localStatus.text;
        }
    }

    private void OnTextChanged(string previousText, string newText)
    {
        RequestGameStatusUpdate(newText);
    }

    public override void OnNetworkSpawn()
    {
        GameStatus.OnValueChanged += OnGameStatusChanged;

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        GameStatus.OnValueChanged -= OnGameStatusChanged;

        base.OnNetworkDespawn();
    }

    private void OnGameStatusChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        _localStatus.text = newValue.ToString();
        _localStatus.gameObject.SetActive(true);
    }

    public void UpdateGameStatus(string newValue)
    {
        if (IsServer)
        {
            GameStatus.Value = new FixedString32Bytes(newValue);
        }
        else
        {
            Debug.LogWarning("Only the server can update the game status.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateGameStatusServerRpc(string newValue)
    {
        GameStatus.Value = new FixedString32Bytes(newValue);
    }

    public void RequestGameStatusUpdate(string newValue)
    {
        if (IsServer)
        {
            UpdateGameStatus(newValue);
            AnalyticsManager.Instance.LogMatchEnd(newValue);
        }
        else
        {
            UpdateGameStatusServerRpc(newValue);
            AnalyticsManager.Instance.LogMatchEnd(newValue);
        }
    }
}