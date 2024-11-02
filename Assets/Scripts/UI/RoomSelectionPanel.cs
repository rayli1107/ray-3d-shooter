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

    public string roomName
    {
        get => _textRoomName.text;
        set { _textRoomName.text = value; }
    }

    public bool selected
    {
        get => _imageBackground.enabled;
        set { _imageBackground.enabled = value; }
    }

    public void OnSelect()
    {
        UIManager.Instance.LobbyPanel.SetSelectionPanel(this);
    }
}
