using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPanel : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _labelPlayerName;

    [SerializeField]
    private TMP_InputField _inputCreateRoomName;

    [SerializeField]
    private RoomSelectionPanel _prefabRoomSelectionPanel;

    private Dictionary<string, RoomSelectionPanel> _roomSelectionPanels;
    private RoomSelectionPanel _selectedPanel;

    private void OnEnable()
    {
        _labelPlayerName.text = string.Format("Player Name: {0}", PhotonNetwork.NickName);
        SetSelectionPanel(null);
    }

    public void SetSelectionPanel(RoomSelectionPanel panel)
    {
        _selectedPanel = panel;
        foreach (KeyValuePair<string, RoomSelectionPanel> entry in _roomSelectionPanels)
        {
            entry.Value.selected = entry.Value == panel;
        }
    }

    public void OnCreateRoomButton()
    {
        if (_inputCreateRoomName.text.Length == 0)
        {
            UIManager.Instance.ShowMessageBox("Please enter room name.", null, true, false);
            return;
        }

        PhotonNetworkManager.Instance.CreateRoom(_inputCreateRoomName.text);
    }

    public void OnJoinRoomButton()
    {
        if (_selectedPanel == null)
        {
            UIManager.Instance.ShowMessageBox("Please select a room.", null, true, false);
            return;
        }

        PhotonNetworkManager.Instance.JoinRoom(_selectedPanel.roomName);
    }

    public void ClearRoomSelectionPanels()
    {
        if (_roomSelectionPanels == null)
        {
            _roomSelectionPanels = new Dictionary<string, RoomSelectionPanel>();
        }

        foreach (KeyValuePair<string, RoomSelectionPanel> entry in _roomSelectionPanels)
        {
            Destroy(entry.Value.gameObject);
        }
        _roomSelectionPanels.Clear();
    }

    public void AddRoomSelectionPanel(RoomInfo roomInfo)
    {
        if (!_roomSelectionPanels.TryGetValue(roomInfo.Name, out RoomSelectionPanel panel)) {
            panel = Instantiate(
                _prefabRoomSelectionPanel, _prefabRoomSelectionPanel.transform.parent);
            panel.selected = false;
            panel.gameObject.SetActive(true);
            _roomSelectionPanels[roomInfo.Name] = panel;
        }
        panel.SetRoomInfo(roomInfo);
    }

    public void RemoveRoomSelectionPanel(string roomName)
    {
        RoomSelectionPanel panel;
        if (_roomSelectionPanels.TryGetValue(roomName, out panel)) {
            if (panel == _selectedPanel)
            {
                _selectedPanel = null;
            }
            Destroy(panel.gameObject);
            _roomSelectionPanels.Remove(roomName);
        }
    }

    public void OnLogOutButton()
    {
        PhotonNetworkManager.Instance.Disconnect();
    }
}
