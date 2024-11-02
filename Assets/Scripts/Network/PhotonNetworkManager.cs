using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

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

public class PhotonNetworkManager : MonoBehaviourPunCallbacks
{
    public static PhotonNetworkManager Instance;
    public bool inRoom => PhotonNetwork.InRoom;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        //        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate = 1;
        UIManager.Instance.ShowMessageBox("Connecting...");
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
        Debug.Log("OnJoinLobby()");
        UIManager.Instance.HideMessageBox();
        UIManager.Instance.LobbyPanel.ClearRoomSelectionPanels();
        UIManager.Instance.LobbyPanel.gameObject.SetActive(true);
    }

    public override void OnLeftLobby()
    {
        base.OnLeftLobby();
        Debug.Log("OnLeftLobby()");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("OnJoinedRoom()");

        UIManager.Instance.HideMessageBox();
        UIManager.Instance.LobbyPanel.gameObject.SetActive(false);
        GameController.Instance.CreatePlayer();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        message = string.Format("Cannot create room: {0}", message);
        UIManager.Instance.ShowMessageBox(message, null, true, false);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        Debug.Log("OnLeftRoom()");
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("OnRoomListUpdate");
        foreach (RoomInfo roomInfo in roomList)
        {
            if (roomInfo.RemovedFromList)
            {
                Debug.LogFormat("Removing {0}", roomInfo.Name);
                UIManager.Instance.LobbyPanel.RemoveRoomSelectionPanel(roomInfo.Name);
            }
            else
            {
                Debug.LogFormat("Adding {0}", roomInfo.Name);
                UIManager.Instance.LobbyPanel.AddRoomSelectionPanel(roomInfo.Name);
            }
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        message = string.Format("Cannot join room: {0}", message);
        UIManager.Instance.ShowMessageBox(message, null, true, false);
    }
}
