using Unity.Netcode;
using UnityEngine;
using System;
using Unity.Collections;
using UnityEngine.UI;
using System.Collections;

public class NetworkStringManager : NetworkBehaviour
{
    public NetworkVariable<FixedString128Bytes> SharedString = new NetworkVariable<FixedString128Bytes>(
        string.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public InputField GameStringInput;

    public event Action<string, bool> OnSharedStringChanged;

    public override void OnNetworkSpawn()
    {
        SharedString.OnValueChanged += OnStringValueChanged;

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        SharedString.OnValueChanged -= OnStringValueChanged;

        base.OnNetworkDespawn();
    }

    private void OnStringValueChanged(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        if(GameStringInput.text != newValue.ToString())
        {
            GameManager.Instance.LoadGame(newValue.ToString());
        }

        OnSharedStringChanged?.Invoke(newValue.ToString(), IsOwner);
    }

    public void UpdateSharedString(string newValue)
    {
        UpdateSharedStringServerRpc(newValue);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateSharedStringServerRpc(string newValue, ServerRpcParams serverRpcParams = default)
    {
        SharedString.Value = new FixedString128Bytes(newValue);
    }
}