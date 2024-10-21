using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public enum NetworkStatus
{
    DISCONNECTED,
    CONNECTING,
    CONNECTED,
    JOINING_LOBBY,
    JOINED_LOBBY,
    CREATING_ROOM,
    CREATED_ROOM,
    JOINING_ROOM,
    JOINED_ROOM,
    DISCONNECTING
}

public class PhotonNetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private bool _skipLobbyRoom;

    [SerializeField]
    private bool _soloRoom = false;

    public static PhotonNetworkManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate = 1;
    }
    public void PhotonConnect()
    {
        /*            AuthenticationValues options = new AuthenticationValues();
                    options.AuthType = CustomAuthenticationType.Custom;
                    options.UserId = userName;
                    PhotonNetwork.AuthValues = options;
        */
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("Connecting");
        UIManager.Instance.ShowMessageBox("Connecting...");
    }

    public override void OnErrorInfo(ErrorInfo errorInfo)
    {
        base.OnErrorInfo(errorInfo);
        string message = errorInfo.Info;
        Debug.Log(message);
        UIManager.Instance.ShowMessageBox(message);
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        if (_skipLobbyRoom)
        {
            PhotonNetwork.JoinRandomOrCreateRoom();
            Debug.Log("Joining or Creating Room...");
            UIManager.Instance.ShowMessageBox("Joining or Creating Room...");
        }
        else
        {
            PhotonNetwork.JoinLobby();
            Debug.Log("Joining Lobby...");
            UIManager.Instance.ShowMessageBox("Joining Lobby...");
        }
    }

    /*
    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        Debug.Log("Joined Lobby");
        UIManager.Instance.HideMessageBox();
        UIManager.Instance.LobbyPanel.gameObject.SetActive(true);
    }

    public override void OnLeftLobby()
    {
        base.OnLeftLobby();
        Debug.Log("On Left Lobby");
    }
    */
    public void LeaveLobby()
    {
        PhotonNetwork.LeaveLobby();
    }

    public void CreateRoom(string roomName)
    {
        RoomOptions options = new RoomOptions();
        options.PublishUserId = true;
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void LeaveRoom()
    {
        Debug.Log("LeaveRoom");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        LeaveLobby();
        UIManager.Instance.HideMessageBox();
//        UIManager.Instance.LobbyPanel.gameObject.SetActive(false);
        Debug.Log("Room Created!");
        if (_skipLobbyRoom)
        {
            if (_soloRoom && PhotonNetwork.LocalPlayer.ActorNumber == 1)
            {
                SceneManager.LoadScene("GameScene");
            }
        }
        else
        {
//            UIManager.Instance.RoomPanel.roomInfo = PhotonNetwork.CurrentRoom;
//            UIManager.Instance.RoomPanel.gameObject.SetActive(true);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        message = string.Format("Cannot create room: {0}", message);
        Debug.Log(message);
        UIManager.Instance.ShowMessageBox(message, null, true, false);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        Debug.Log("OnLeftRoom");
//        UIManager.Instance.RoomPanel.gameObject.SetActive(false);
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }
    /*
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("OnRoomListUpdate");
        foreach (RoomInfo roomInfo in roomList)
        {
            if (roomInfo.RemovedFromList)
            {
                Debug.LogFormat("Removing {0}", roomInfo.Name);
                UIManager.Instance.LobbyPanel.RemoveRoom(roomInfo.Name);
            }
            else
            {
                Debug.LogFormat("Adding {0}", roomInfo.Name);
                UIManager.Instance.LobbyPanel.AddRoom(roomInfo.Name, roomInfo);
            }
        }
    }
    */
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.LogFormat("OnPlayerEnteredRoom {0}", newPlayer.UserId);
        if (_skipLobbyRoom)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
            {
                SceneManager.LoadScene("GameScene");
            }
        }
        else
        {
//            UIManager.Instance.RoomPanel.UpdatePlayers();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.LogFormat("OnPlayerLeftRoom {0}", otherPlayer.UserId);
//        UIManager.Instance.RoomPanel.UpdatePlayers();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        message = string.Format("Cannot join room: {0}", message);
        Debug.Log(message);
        UIManager.Instance.ShowMessageBox(message, null, true, false);
    }

    public void StartGame()
    {
        Room room = PhotonNetwork.CurrentRoom;
        if (room == null || room.PlayerCount < 2)
        {
            UIManager.Instance.ShowMessageBox(
                "Need at least two players.", null, true, false);
            return;
        }

        SceneManager.LoadScene("GameScene");
    }
}
