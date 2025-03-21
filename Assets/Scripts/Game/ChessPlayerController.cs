using Unity.Netcode;
using UnityEngine;
using UnityChess;

public class ChessPlayerController : NetworkBehaviour
{
    public Side PlayerSide { get; private set; }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
            PlayerSide = playerCount switch
            {
                1 => Side.White,
                2 => Side.Black,
                _ => Side.White
            };

            if (OwnerClientId == 0)
            {
                Debug.Log($"Player {OwnerClientId} (Host), has been assigned to {PlayerSide}");
            }else
            {
                Debug.Log($"Player {OwnerClientId} (Client), has been assigned to {PlayerSide}");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            Debug.Log($"Player {OwnerClientId} disconnected.");
        }
    }

    private void Update()
    {
        
    }
}