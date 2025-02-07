using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RoomSelectionPanel : ModalObject
{
    [SerializeField]
    private TextMeshProUGUI _textRoomName;

    [SerializeField]
    private Image _imageBackground;

    public string roomName { get; private set; }

    public bool selected
    {
        get => _imageBackground.enabled;
        set { _imageBackground.enabled = value; }
    }

    public void SetRoomInfo(RoomInfo roomInfo)
    {
        roomName = roomInfo.Name;
        _textRoomName.text = string.Format(
            "{0} {1} / {2}", roomInfo.Name, roomInfo.PlayerCount, roomInfo.MaxPlayers);
    }

    public void OnSelect()
    {
        UIManager.Instance.LobbyPanel.SetSelectionPanel(this);
    }
}
