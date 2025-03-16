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

            //BoardManager.Instance.PlayerSide = PlayerSide;

            Debug.Log($"Player {OwnerClientId} assigned as {PlayerSide}");
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

    public void AssignPlayerSide(Side side)
    {
        if (IsServer)
        {
            AssignPlayerSideClientRpc(side);
        }
    }

    [ClientRpc]
    protected void AssignPlayerSideClientRpc(Side side)
    {
        PlayerSide = side;
        Debug.Log($"Assigned as {side}");
    }
}