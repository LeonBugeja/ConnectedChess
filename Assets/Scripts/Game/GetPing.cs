using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class GetPing : NetworkBehaviour
{
    [SerializeField] private float pingUpdateInterval = 2.0f;

    private float currentPing = 0f;

    private bool isInitialized = false;
    private string networkRole = "None";

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && IsClient)
            networkRole = "Host";
        else if (IsServer)
            networkRole = "Server";
        else if (IsClient)
            networkRole = "Client";

        isInitialized = true;

        if (IsServer)
        {
            StartCoroutine(MeasurePingCoroutine());
        }
    }

    public override void OnNetworkDespawn()
    {
        isInitialized = false;
        base.OnNetworkDespawn();
    }

    private IEnumerator MeasurePingCoroutine()
    {
        yield return new WaitForSeconds(1.0f);

        while (isInitialized && IsServer)
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (clientId != NetworkManager.ServerClientId)
                {
                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { clientId }
                        }
                    };

                    SendPingRequestClientRpc(clientId, NetworkManager.Singleton.ServerTime.Time, clientRpcParams);
                }
            }

            yield return new WaitForSeconds(pingUpdateInterval);
        }
    }

    [ClientRpc]
    private void SendPingRequestClientRpc(ulong clientId, double serverTime, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            PingResponseServerRpc(clientId, serverTime);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PingResponseServerRpc(ulong clientId, double originalTime)
    {
        float rtt = (float)(NetworkManager.Singleton.ServerTime.Time - originalTime);

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        float pingMs = rtt * 1000f;
        Debug.Log($"Ping between server and client is: {pingMs:F1}ms");
        SendPingResultClientRpc(clientId, pingMs, clientRpcParams);
    }

    [ClientRpc]
    private void SendPingResultClientRpc(ulong clientId, float pingMs, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            currentPing = pingMs;
            Debug.Log($"Ping between server and client is: {currentPing:F1}ms");
        }
    }
}