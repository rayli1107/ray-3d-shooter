using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : MonoBehaviour
{
    [SerializeField]
    private Button _buttonLoginFB;

    public bool enableFacebookLogin
    {
        get => _buttonLoginFB.gameObject.activeInHierarchy;
        set { _buttonLoginFB.gameObject.SetActive(value); }
    }
}
