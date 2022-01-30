using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DapperDino.UMT.Lobby.Networking;
using DapperDino.UMT.Lobby.UI;
using Unity.Netcode;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    public GameObject[] lobbyPlayerModels;
    public Text countdownText;
    public GameObject playerPrefab;

    private int countdown;
    private bool isCountdown;

    private NetworkList<LobbyPlayerState> lobbyPlayers;
    
    private void Awake() {
        lobbyPlayers = new NetworkList<LobbyPlayerState>();
    }

    public void OnNetworkSpawnManual() {
        if (IsClient) {
            lobbyPlayers.OnListChanged += HandleLobbyPlayersStateChanged;
        }

        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList) {
                HandleClientConnected(client.ClientId);
            }
        }
    }

    public override void OnDestroy() {
        base.OnDestroy();

        lobbyPlayers.OnListChanged -= HandleLobbyPlayersStateChanged;

        if (NetworkManager.Singleton) {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    private bool IsEveryoneReady() {
        foreach (var player in lobbyPlayers) {
            if (!player.IsReady) {
                return false;
            }
        }

        return true;
    }

    private void HandleClientConnected(ulong clientId) {
        var playerData = ServerGameNetPortal.Instance.GetPlayerData(clientId);

        if (!playerData.HasValue) { return; }

        lobbyPlayers.Add(new LobbyPlayerState(
            clientId,
            playerData.Value.PlayerName,
            false
        ));
        Debug.Log(playerData.Value.PlayerName);
    }

    private void HandleClientDisconnect(ulong clientId) {
        for (int i = 0; i < lobbyPlayers.Count; i++) {
            if (lobbyPlayers[i].ClientId == clientId) {
                lobbyPlayers.RemoveAt(i);
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyServerRpc(ServerRpcParams serverRpcParams = default) {
        for (int i = 0; i < lobbyPlayers.Count; i++) {
            if (lobbyPlayers[i].ClientId == serverRpcParams.Receive.SenderClientId) {
                lobbyPlayers[i] = new LobbyPlayerState(
                    lobbyPlayers[i].ClientId,
                    lobbyPlayers[i].PlayerName,
                    !lobbyPlayers[i].IsReady
                );
                Debug.Log("ready info sent");
                if (IsEveryoneReady()) {
                    countdown = 3;
                    Debug.Log("countdown launched");
                    if (!isCountdown) StartCoroutine(StartGameCountdown());
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc(ulong clientId, ServerRpcParams serverRpcParams = default) {
        if (!IsEveryoneReady()) { return; }

        ServerGameNetPortal.Instance.StartGame();

        Debug.Log("guh");
        GameObject go = Instantiate(playerPrefab, playerPrefab.transform.position, playerPrefab.transform.rotation);
        go.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    private IEnumerator StartGameCountdown() {
        Debug.Log("countdown started");
        isCountdown = true;
        countdownText.enabled = true;
        for (; countdown > 0; countdown--) {
            Debug.Log("countdown: " + countdown);
            countdownText.text = countdown.ToString();
            yield return new WaitForSeconds(1);
            if (!IsEveryoneReady()) {
                countdownText.enabled = false;
                isCountdown = false;
                yield break;
            }
        }
        Debug.Log("reached 0");
        countdownText.text = countdown.ToString();
        yield return new WaitForSeconds(3);
        StartGameServerRpc(NetworkManager.Singleton.LocalClientId);
        isCountdown = false;
        yield break;
    }

    public void OnLeaveClicked() {
        GameNetPortal.Instance.RequestDisconnect();
    }

    public void OnReadyClicked() {
        ToggleReadyServerRpc();
    }

    private void HandleLobbyPlayersStateChanged(NetworkListEvent<LobbyPlayerState> lobbyState) {
        for (int i = 0; i < lobbyPlayerModels.Length; i++) {
            if (lobbyPlayers.Count > i) {
                lobbyPlayerModels[i].SetActive(true);
            }
            else {
                lobbyPlayerModels[i].SetActive(false);
            }
        }
    }
}