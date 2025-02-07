using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using RpcTarget = Photon.Pun.RpcTarget;

public class CoinController : MonoBehaviourPunCallbacks
{
    private int _coinValue;

    public override void OnEnable()
    {
        base.OnEnable();
        _coinValue = (int)photonView.InstantiationData[0];
        GameController.Instance.OnCoinCreate();
    }

    public override void OnDisable()
    {
        GameController.Instance.OnCoinDestroy();
        base.OnDisable();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController target = other.GetComponent<PlayerController>();
        if (target != null && target.isMine)
        {
            target.AddCoins(_coinValue);
            
            if (photonView.IsMine)
            {
                Debug.Log("Local Destroy Coin");
                CoinDestroyRPC();
            }
            else
            {
                Debug.Log("Remote Destroy Coin");
                photonView.RPC("CoinDestroyRPC", RpcTarget.OthersBuffered);
            }
        }
    }

    [PunRPC]
    public void CoinDestroyRPC()
    {
        Debug.LogFormat("CoinDestroyRPC {0}", photonView.IsMine);
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(photonView);
        }
    }
}
