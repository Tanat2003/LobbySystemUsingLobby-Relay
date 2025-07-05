using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllLobbySystemUI : MonoBehaviour
{
    private void Start()
    {
        LobbyManager.Instance.OnGameStarted += LobbyManager_OnGameStarted;
    }

    private void LobbyManager_OnGameStarted(object sender, System.EventArgs e)
    {
        gameObject.SetActive(false);
    }
}
