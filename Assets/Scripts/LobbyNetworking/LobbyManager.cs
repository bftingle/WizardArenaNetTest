using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DapperDino.UMT.Lobby.Networking;
using DapperDino.UMT.Lobby.UI;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : NetworkBehaviour
{
    public GameObject[] lobbyPlayerModels;
    public Text countdownText;
    public GameObject playerPrefab;
    public PlayerManager playerManager;
    public ServerGameNetPortal serverGameNetPortal;

    private int countdown;
    private bool isCountdown;

    private NetworkList<LobbyPlayerState> lobbyPlayers;
    private NetworkVariable<NetworkString> playerName = new NetworkVariable<NetworkString>();
    private List<NetworkVariable<ulong>> connectedClientIdsList = new List<NetworkVariable<ulong>>();

    private void Awake() {
        lobbyPlayers = new NetworkList<LobbyPlayerState>();

        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn() {
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
        int playersPre = playerManager.PlayersInGame;
        Debug.Log("Players: " + playersPre);
        StartCoroutine(WaitForPlayer(clientId, playersPre));
    }

    private IEnumerator WaitForPlayer(ulong clientId, int playersPre) {
        yield return new WaitUntil(() => playerManager.PlayersInGame > playersPre);
        HandleClientConnectedReady(clientId);
    }

    private void HandleClientConnectedReady(ulong clientId) {
        Debug.Log("Connecting " + clientId);
        var playerData = ServerGameNetPortal.Instance.GetPlayerData(clientId);

        if (!playerData.HasValue) { return; }

        lobbyPlayers.Add(new LobbyPlayerState(
            clientId,
            playerData.Value.PlayerName,
            false
        ));
        Debug.Log(playerData.Value.PlayerName);
    }

    public void HandleClientDisconnect(ulong clientId) {
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
                if (IsEveryoneReady()) {
                    countdown = 3;
                    if (!isCountdown) StartCoroutine(StartGameCountdown());
                }
            }
        }
    }

    private IEnumerator StartGameCountdown() {
        isCountdown = true;
        countdownText.gameObject.SetActive(true);
        for (; countdown > 0; countdown--) {
            countdownText.text = countdown.ToString();
            yield return new WaitForSeconds(1);
            if (!IsEveryoneReady()) {
                countdownText.gameObject.SetActive(false);
                isCountdown = false;
                yield break;
            }
        }
        countdownText.text = countdown.ToString();
        yield return new WaitForSeconds(0.5f);
        StartGameServerRpc();
        isCountdown = false;
        yield break;
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc(ServerRpcParams serverRpcParams = default) {
        if (!IsEveryoneReady()) { return; }

        ServerGameNetPortal.Instance.StartGame();

        StartCoroutine(WaitForSceneToSpawn("SampleScene"));
    }

    private IEnumerator WaitForSceneToSpawn(string sceneName) {
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);

        if (IsServer) {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList) {
                GameObject go = Instantiate(playerPrefab, playerPrefab.transform.position, playerPrefab.transform.rotation);
                go.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId);
            }
        }
    }

    /*[ClientRpc]
    private void SpawnPlayerClientRpc() {
        GameObject go = Instantiate(playerPrefab, playerPrefab.transform.position, playerPrefab.transform.rotation);
        go.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
    }*/

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

        if (IsHost) {
            HandleLobbyPlayerNames();
        } else {
            int playersPre = playerManager.PlayersInGame;
            Debug.Log("Players: " + playersPre);
            StartCoroutine(WaitForPlayerName(playersPre));
        }
    }

    private IEnumerator WaitForPlayerName(int playersPre) {
        yield return new WaitUntil(() => playerManager.PlayersInGame > playersPre);
        HandleLobbyPlayerNames();
    }

    public void HandleLobbyPlayerNames() {
        GetPlayerNameServerRpc(OwnerClientId);
        var localPlayerOverlay = lobbyPlayerModels[0].GetComponentInChildren<TextMeshProUGUI>();
        localPlayerOverlay.text = playerName.Value;
        int j = 1;
        for (ulong clientId = 0; clientId < (ulong)playerManager.PlayersInGame; clientId++) {
            if (clientId == OwnerClientId) continue;

            GetPlayerNameServerRpc(clientId);
            localPlayerOverlay = lobbyPlayerModels[j].GetComponentInChildren<TextMeshProUGUI>();
            localPlayerOverlay.text = playerName.Value;
            j++;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetPlayerNameServerRpc(ulong clientId) {
        playerName.Value = serverGameNetPortal.GetPlayerData(clientId).Value.PlayerName;
    }
}