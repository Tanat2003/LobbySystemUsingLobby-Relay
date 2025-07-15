using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCameraControl : NetworkBehaviour
{
    [SerializeField] private GameObject playerCamera;

    public override void OnNetworkSpawn()
    {
        playerCamera.SetActive(IsOwner);
    }
}
