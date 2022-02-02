using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DapperDino.UMT.Lobby.Networking {
    [RequireComponent(typeof(GameNetPortal))]
    public class ClientGameNetPortal : MonoBehaviour {
        public static ClientGameNetPortal Instance => instance;
        private static ClientGameNetPortal instance;

        public DisconnectReason DisconnectReason { get; private set; } = new DisconnectReason();

        public event Action<ConnectStatus> OnConnectionFinished;

        public event Action OnNetworkTimedOut;

        public InputField joinCodeInput;
        public RelayManager relayManager;
        public LobbyManager lobbyManager;
        public PlayerManager playerManager;

        private GameNetPortal gameNetPortal;

        private void Awake() {
            if (instance != null && instance != this) {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() {
            gameNetPortal = GetComponent<GameNetPortal>();

            gameNetPortal.OnNetworkReadied += HandleNetworkReadied;
            gameNetPortal.OnConnectionFinished += HandleConnectionFinished;
            gameNetPortal.OnDisconnectReasonReceived += HandleDisconnectReasonReceived;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback += AddPlayerId;
        }

        private void OnDestroy() {
            if (gameNetPortal == null) { return; }

            gameNetPortal.OnNetworkReadied -= HandleNetworkReadied;
            gameNetPortal.OnConnectionFinished -= HandleConnectionFinished;
            gameNetPortal.OnDisconnectReasonReceived -= HandleDisconnectReasonReceived;

            if (NetworkManager.Singleton == null) { return; }

            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        public void StartClient() {
            var payload = JsonUtility.ToJson(new ConnectionPayload() {
                clientGUID = Guid.NewGuid().ToString(),
                clientScene = SceneManager.GetActiveScene().buildIndex,
                playerName = PlayerPrefs.GetString("PlayerName", "Missing Name")
            });

            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

            NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

            //NetworkManager.Singleton.StartClient();
            StartClientAsync();
        }

        public async void StartClientAsync() {
            if (relayManager.IsRelayEnabled && !string.IsNullOrEmpty(joinCodeInput.text)) {
                await relayManager.JoinRelay(joinCodeInput.text);
                joinCodeInput.readOnly = true;
            }

            if (NetworkManager.Singleton.StartClient()) {
                Debug.Log("Client Started");
                //playerManager.PlayersPlusPlusServerRpc();
                //StartCoroutine(DelayAddCallToServer());
            }
            else {
                Debug.Log("Client Not Started");
            }
        }

        //private IEnumerator DelayAddCallToServer() {
            //yield return new WaitForSeconds(3f);
            //playerManager.AddPlayerIdServerRpc(NetworkManager.Singleton.LocalClientId);
        //}

        private void AddPlayerId(ulong clientId) {
            if (clientId == NetworkManager.Singleton.LocalClientId) {
                playerManager.AddPlayerIdServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        private void HandleNetworkReadied() {
            if (!NetworkManager.Singleton.IsClient) { return; }

            if (!NetworkManager.Singleton.IsHost) {
                gameNetPortal.OnUserDisconnectRequested += HandleUserDisconnectRequested;
            }
        }

        private void HandleUserDisconnectRequested() {
            DisconnectReason.SetDisconnectReason(ConnectStatus.UserRequestedDisconnect);
            NetworkManager.Singleton.Shutdown();

            HandleClientDisconnect(NetworkManager.Singleton.LocalClientId);

            SceneManager.LoadScene("Scene_Menu");
        }

        private void HandleConnectionFinished(ConnectStatus status) {
            if (status != ConnectStatus.Success) {
                DisconnectReason.SetDisconnectReason(status);
            }

            OnConnectionFinished?.Invoke(status);
        }

        private void HandleDisconnectReasonReceived(ConnectStatus status) {
            DisconnectReason.SetDisconnectReason(status);
        }

        private void HandleClientDisconnect(ulong clientId) {
            if (!NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost) {
                gameNetPortal.OnUserDisconnectRequested -= HandleUserDisconnectRequested;

                if (SceneManager.GetActiveScene().name != "Scene_Menu") {
                    if (!DisconnectReason.HasTransitionReason) {
                        DisconnectReason.SetDisconnectReason(ConnectStatus.GenericDisconnect);
                    }

                    SceneManager.LoadScene("Scene_Menu");
                }
                else {
                    OnNetworkTimedOut?.Invoke();
                }
            }
        }
    }
}