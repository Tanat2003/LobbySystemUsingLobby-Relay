using Unity.Netcode;
using UnityEngine;

public class ForTestingMultiWindowinEditor : MonoBehaviour
{
    [SerializeField] private bool testingMultiWindow;
    void Start()
    {
        if (!testingMultiWindow)
            return;
        if (Application.isEditor)
        {
            Debug.Log("Running as HOST (Editor)");
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            Debug.Log("Running as CLIENT (Build)");
            NetworkManager.Singleton.StartClient();
        }

    }

}
