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
        NetworkVariableWritePermission.Owner);

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
        if (!IsOwner)
        {
            StartCoroutine(RequestOwnershipAndApplyUpdate(newValue));
        }
        else
        {
            SharedString.Value = new FixedString128Bytes(newValue);
        }
    }

    private IEnumerator RequestOwnershipAndApplyUpdate(string newValue)
    {
        RequestOwnershipServerRpc();

        yield return SharedString.Value = new FixedString128Bytes(newValue);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;

        NetworkObject.ChangeOwnership(clientId);
    }
}