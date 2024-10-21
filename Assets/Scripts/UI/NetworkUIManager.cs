using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkUIManager : MonoBehaviour
{
    [SerializeField]
    private RectTransform _panelNetworkFunctions;

    public void StartNetworkAsServer()
    {
        NetworkManager.Singleton.StartServer();
        _panelNetworkFunctions.gameObject.SetActive(false);
    }

    public void StartNetworkAsHost()
    {
        NetworkManager.Singleton.StartHost();
        _panelNetworkFunctions.gameObject.SetActive(false);
    }

    public void StartNetworkAsClient()
    {
        NetworkManager.Singleton.StartClient();
        _panelNetworkFunctions.gameObject.SetActive(false);
    }
}
