using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    public Button startHostButton;
    public Button startServerButton;
    public Button startClientButton;
    
    private void Awake() {
        Cursor.visible = true;
    }

    private void Start() {
        startHostButton.onClick.AddListener(() => {
            if (NetworkManager.Singleton.StartHost()) {
                Debug.Log("Host Started");
            } else {
                Debug.Log("Host Not Started");
            }
        });
        startServerButton.onClick.AddListener(() => {
            if (NetworkManager.Singleton.StartServer()) {
                Debug.Log("Server Started");
            }
            else {
                Debug.Log("Server Not Started");
            }
        });
        startClientButton.onClick.AddListener(() => {
            if (NetworkManager.Singleton.StartClient()) {
                Debug.Log("Client Started");
            }
            else {
                Debug.Log("Client Not Started");
            }
        });
    }

    private void Update() {
        
    }
}
