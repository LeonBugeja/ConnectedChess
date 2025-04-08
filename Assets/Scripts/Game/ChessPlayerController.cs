using Unity.Netcode;
using UnityEngine;
using UnityChess;

public class ChessPlayerController : NetworkBehaviour
{
    public Side PlayerSide { get; private set; }

    private NetworkGameStatus _gameStatus;

    private void Start()
    {
        GameObject gameManager = GameObject.Find("GameManager");
        _gameStatus = gameManager.GetComponentInChildren<NetworkGameStatus>();
    }

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
                GameManager.Instance.StartNewGame();

                AnalyticsManager.Instance.LogMatchStart();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            _gameStatus.RequestGameStatusUpdate("Opponent resigned.");
            AnalyticsManager.Instance.LogMatchEnd("Black Resigned");
        }
        else
        {
            _gameStatus.RequestGameStatusUpdate("Host resigned.");
            AnalyticsManager.Instance.LogMatchEnd("White Resigned");
        }
    }
}