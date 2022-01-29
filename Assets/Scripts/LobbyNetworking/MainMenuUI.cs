using DapperDino.UMT.Lobby.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace DapperDino.UMT.Lobby {
    public class MainMenuUI : MonoBehaviour {
        [Header("References")]
        [SerializeField] private InputField displayNameInputField;

        private void Start() {
            PlayerPrefs.GetString("PlayerName");
        }

        public void OnHostClicked() {
            PlayerPrefs.SetString("PlayerName", displayNameInputField.text);

            GameNetPortal.Instance.StartHost();
        }

        public void OnClientClicked() {
            PlayerPrefs.SetString("PlayerName", displayNameInputField.text);

            ClientGameNetPortal.Instance.StartClient();
        }
    }
}
