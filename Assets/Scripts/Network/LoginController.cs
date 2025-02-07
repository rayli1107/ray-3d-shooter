using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System;

public class LoginController : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private string _firebaseURI;

    public static LoginController Instance;
    public bool inRoom => PhotonNetwork.InRoom;

    private void Awake()
    {
        Instance = this;
    }

    public void SignIn(string email, string password)
    {
        if (email.Length == 0)
        {
            UIManager.Instance.ShowMessageBox("Please enter a valid email.", null, true);
            return;
        }

        if (password.Length == 0)
        {
            UIManager.Instance.ShowMessageBox("Please enter a valid password.", null, true);
            return;
        }

        UIManager.Instance.ShowMessageBox("Signing In...");
        void callback(string s) => signInGetRequestCallback(s, email, password);
        StartCoroutine(GetRequest(_firebaseURI, callback));
    }

    private void signInGetRequestCallback(string body, string email, string password)
    {
        UserInfoList userInfoList;
        try
        {
            userInfoList = JsonUtility.FromJson<UserInfoList>(body);
        }
        catch (ArgumentException)
        {
            userInfoList = new();
        }

        foreach (UserInfo userInfo in userInfoList.users)
        {
            if (userInfo.email == email && userInfo.password == password)
            {
                PhotonNetworkManager.Instance.OnAuthenticationDone(email, userInfo.playerName);
                return;
            }
        }

        UIManager.Instance.ShowMessageBox("Incorrect email or password!", null, true);
    }

    public void Signup(string email, string password, string nickname)
    {
        if (email.Length == 0)
        {
            UIManager.Instance.ShowMessageBox("Please enter a valid email.", null, true);
            return;
        }

        if (password.Length == 0)
        {
            UIManager.Instance.ShowMessageBox("Please enter a valid password.", null, true);
            return;
        }

        if (nickname.Length == 0)
        {
            UIManager.Instance.ShowMessageBox("Please enter a valid nickname.", null, true);
            return;
        }

        // A correct website page.
        UIManager.Instance.ShowMessageBox("Signing Up...");
        void callback(string s) => signupGetRequestCallback(s, email, password, nickname);
        StartCoroutine(GetRequest(_firebaseURI, callback));
    }

    private void signupGetRequestCallback(string body, string email, string password, string nickname)
    {
        UserInfoList userInfoList;
        try
        {
            userInfoList = JsonUtility.FromJson<UserInfoList>(body);
        }
        catch (ArgumentException)
        {
            userInfoList = new();
        }

        foreach (UserInfo userInfo in userInfoList.users)
        {
            if (userInfo.email == email)
            {
                UIManager.Instance.ShowMessageBox("User already exists!", null, true);
                return;
            }
        }

        UserInfo newUserInfo = new();
        newUserInfo.email = email;
        newUserInfo.password = password;
        newUserInfo.playerName = nickname;
        userInfoList.users.Add(newUserInfo);

        body = JsonUtility.ToJson(userInfoList);
        void callback() => PhotonNetworkManager.Instance.OnAuthenticationDone(email, nickname);
        StartCoroutine(PutRequest(_firebaseURI, body, callback));
    }

    IEnumerator GetRequest(string uri, Action<string> callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogErrorFormat("{0} Error: {1}", uri, webRequest.error);
                    UIManager.Instance.ShowMessageBox(webRequest.error, null, true);
                    break;
                case UnityWebRequest.Result.Success:
                    callback?.Invoke(webRequest.downloadHandler.text);
                    break;
            }
        }
    }

    IEnumerator PutRequest(string uri, string body, Action callback)
    {
        Debug.LogFormat("PutRequest:\n{0}", body);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(body);
        using (UnityWebRequest www = UnityWebRequest.Put(uri, data))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                UIManager.Instance.ShowMessageBox(www.error, null, true);
            }
            else
            {
                callback?.Invoke();
            }
        }
    }
}
