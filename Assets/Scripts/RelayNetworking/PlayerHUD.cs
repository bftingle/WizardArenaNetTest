using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerHUD : NetworkBehaviour
{
    private NetworkVariable<NetworkString> playerName = new NetworkVariable<NetworkString>();

    private bool nameSet = false;

    public override void OnNetworkSpawn() {
        if (IsServer) {
            playerName.Value = $"Player {OwnerClientId}";
        }
    }

    public void SetName() {
        var localPlayerOverlay = gameObject.GetComponentInChildren<TextMeshProUGUI>();
        localPlayerOverlay.text = playerName.Value;
    }

    void Start()
    {
        
    }

    private void Update() {
        if (!nameSet && !string.IsNullOrEmpty(playerName.Value)) {
            SetName();
            nameSet = true;
        }
    }
}
