using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance => instance;
    private static PlayerManager instance;

    private NetworkVariable<int> playersInGame = new NetworkVariable<int>();

    public int PlayersInGame {
        get {
            return playersInGame.Value;
        }
    }

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) => {
            if (IsServer) {
                Debug.Log(id + " just connected");
                //playersInGame.Value++;
            }
        };
        NetworkManager.Singleton.OnClientDisconnectCallback += (id) => {
            if (IsServer) {
                Debug.Log(id + " just disconnected");
                //playersInGame.Value--;
            }
        };
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayersPlusPlusServerRpc() {
        playersInGame.Value++;
        Debug.Log("PP Runs");
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayersMinusMinusServerRpc() {
        playersInGame.Value--;
    }
}
