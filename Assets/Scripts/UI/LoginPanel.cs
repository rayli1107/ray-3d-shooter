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
    private TMP_InputField _inputSignupEmail;

    [SerializeField]
    private TMP_InputField _inputSignupPassword;

    [SerializeField]
    private TMP_InputField _inputSignupNickname;

    [SerializeField]
    private TMP_InputField _inputLoginEmail;

    [SerializeField]
    private TMP_InputField _inputLoginPassword;

    public void Signup()
    {
        LoginController.Instance.Signup(
            _inputSignupEmail.text, _inputSignupPassword.text, _inputSignupNickname.text);
    }

    public void SignIn()
    {
        LoginController.Instance.SignIn(_inputLoginEmail.text, _inputLoginPassword.text);
    }
}
