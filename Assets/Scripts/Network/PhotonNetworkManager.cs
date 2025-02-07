using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum NetworkStatus
{
    DISCONNECTED,
    JOINED_LOBBY,
    CREATING_ROOM,
    CREATED_ROOM,
    JOINING_ROOM,
    JOINED_ROOM,
    DISCONNECTING
}

[Serializable]
public class PhotonNetworkVariableKey
{
    public string playerUserId;
    public string key;
}

public class PhotonNetworkVariable
{
    public Action updateAction;
    public string key { get; private set; }
    public object value { get; private set; }
/*    private object _value;
    public object value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                updateRoomProperties();
                updateAction?.Invoke();
            }
        }
    }*/

    private PhotonView _photonView;
    public PhotonNetworkVariable(PhotonView photonView, string key, object defaultValue, Action updateAction)
    {
        _photonView = photonView;
        this.key = key;
        this.updateAction = updateAction;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object value))
        {
            this.value = value;
        }
        else
        {
            this.value = defaultValue;
            updateRoomProperties(this.value);
        }
    }

    private void updateRoomProperties(object value)
    {
        if (_photonView.IsMine)
        {
            ExitGames.Client.Photon.Hashtable table = new();
            table.Add(key, value);
            PhotonNetwork.CurrentRoom.SetCustomProperties(table);
        }
    }

    public void LocalSetValue(object value)
    {
        if (this.value != value)
        {
            updateRoomProperties(value);
        }
    }

    public void RemoteSetValue(object value)
    {
        if (this.value != value)
        {
            this.value = value;
            updateAction?.Invoke();
        }
    }
}


public class PhotonNetworkManager : MonoBehaviourPunCallbacks
{
    [field: SerializeField]
    public int maxPlayers { get; private set; }

    public static PhotonNetworkManager Instance;
    public bool inRoom => PhotonNetwork.InRoom;

    //    public Action<ExitGames.Client.Photon.Hashtable> roomPropertiesCallback;

    private Dictionary<object, List<PhotonNetworkVariable>> _networkVariables;

    private void Awake()
    {
        Instance = this;
        _networkVariables = new Dictionary<object, List<PhotonNetworkVariable>>();
    }

    public PhotonNetworkVariable RegisterNetworkVariable(
        PhotonView photonView, string key, object defaultValue, Action updateAction)
    {
        PhotonNetworkVariableKey keyObject = new();
        keyObject.playerUserId = photonView.Controller.UserId;
        keyObject.key = key;
        key = JsonUtility.ToJson(keyObject);

        PhotonNetworkVariable variableObject = new(photonView, key, defaultValue, updateAction);

        if (!_networkVariables.TryGetValue(key, out List<PhotonNetworkVariable> variableList))
        {
            variableList = new();
            _networkVariables[key] = variableList;
        }

        if (!variableList.Contains(variableObject))
        {
            variableList.Add(variableObject);
        }

        return variableObject;
    }

    public void UnregisterNetworkVariable(PhotonNetworkVariable variable)
    {
        if (_networkVariables.TryGetValue(
            variable.key, out List<PhotonNetworkVariable> variableList))
        {
            variableList.Remove(variable);
        }
    }

    public void Start()
    {
        if (Application.isEditor)
        {
            OnAuthenticationDone("rayli1107@gmail.com", "Ray");
        }
    }

    public void OnAuthenticationDone(string email, string nickname)
    {
        PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate = 1;
        UIManager.Instance.LoginPanel.gameObject.SetActive(false);
        UIManager.Instance.ShowMessageBox("Connecting...");
        PhotonNetwork.NickName = nickname;
        PhotonNetwork.AuthValues = new();
        PhotonNetwork.AuthValues.UserId = email;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnErrorInfo(ErrorInfo errorInfo)
    {
        base.OnErrorInfo(errorInfo);
        string message = errorInfo.Info;
        UIManager.Instance.ShowMessageBox(message);
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        //        PhotonNetwork.JoinRandomOrCreateRoom();
        //UIManager.Instance.ShowMessageBox("Joining or Creating Room...");
        UIManager.Instance.ShowMessageBox("Joining Lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        UIManager.Instance.HideMessageBox();
        UIManager.Instance.LobbyPanel.ClearRoomSelectionPanels();
        UIManager.Instance.LobbyPanel.gameObject.SetActive(true);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        if (cause != DisconnectCause.DisconnectByClientLogic)
        {
            UIManager.Instance.ShowMessageBox(
                string.Format("Disconected: {0}", cause), null, true);
        }
        Debug.LogFormat("Cause {0}", cause);
        UIManager.Instance.LobbyPanel.gameObject.SetActive(false);
        UIManager.Instance.LoginPanel.gameObject.SetActive(true);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("PhotonNetworkManager.OnJoinedRoom()");
        UIManager.Instance.HideMessageBox();
        UIManager.Instance.LobbyPanel.gameObject.SetActive(false);
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);
        foreach (object key in propertiesThatChanged.Keys)
        {
            if (propertiesThatChanged.TryGetValue(key, out object value) &&
                _networkVariables.TryGetValue(key, out List<PhotonNetworkVariable> variables))
            {
                variables.ForEach(v => v.RemoteSetValue(value));
            }
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        message = string.Format("Cannot create room: {0}", message);
        UIManager.Instance.ShowMessageBox(message, null, true, false);
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public void CreateRoom(string roomName)
    {
        RoomOptions options = new();
        options.PublishUserId = true;
        options.MaxPlayers = maxPlayers;
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void Disconnect()
    {
        UIManager.Instance.LobbyPanel.gameObject.SetActive(false);
        PhotonNetwork.Disconnect();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo roomInfo in roomList)
        {
            if (roomInfo.RemovedFromList)
            {
                UIManager.Instance.LobbyPanel.RemoveRoomSelectionPanel(roomInfo.Name);
            }
            else
            {
                UIManager.Instance.LobbyPanel.AddRoomSelectionPanel(roomInfo);
            }
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        message = string.Format("Cannot join room: {0}", message);
        UIManager.Instance.ShowMessageBox(message, null, true, false);
    }

    public bool IsRoomMaster()
    {
        return PhotonNetwork.IsMasterClient;
    }
}
