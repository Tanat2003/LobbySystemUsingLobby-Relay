using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllLobbyUISystem : MonoBehaviour
{
    
    void Start()
    {
        LobbyManager.Instance.OnGameStarted += Instance_OnGameStart;
    }

    private void Instance_OnGameStart(object sender, System.EventArgs e)
    {
        gameObject.SetActive(false);
    }

   
}
