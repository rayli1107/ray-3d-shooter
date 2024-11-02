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
    private TMP_InputField _inputPlayerName;

    [SerializeField]
    private TMP_InputField _inputCreateRoomName;

    [SerializeField]
    private RoomSelectionPanel _prefabRoomSelectionPanel;

    private Dictionary<string, RoomSelectionPanel> _roomSelectionPanels;
    private RoomSelectionPanel _selectedPanel;

    private void OnEnable()
    {
        SetSelectionPanel(null);
    }

    public void SetSelectionPanel(RoomSelectionPanel panel)
    {
        Debug.LogFormat("SetSelectionPanel {0}", panel);
        _selectedPanel = panel;
        foreach (KeyValuePair<string, RoomSelectionPanel> entry in _roomSelectionPanels)
        {
            Debug.LogFormat("  Checking {0} {1}", entry.Value, entry.Value == panel);
            entry.Value.selected = entry.Value == panel;
        }
    }

    public void OnCreateRoomButton()
    {
        if (_inputPlayerName.text.Length == 0)
        {
            UIManager.Instance.ShowMessageBox("Please enter player name.", null, true, false);
            return;
        }
        if (_inputCreateRoomName.text.Length == 0)
        {
            UIManager.Instance.ShowMessageBox("Please enter room name.", null, true, false);
            return;
        }

        PhotonNetwork.LocalPlayer.NickName = _inputPlayerName.text;
        PhotonNetwork.CreateRoom(_inputCreateRoomName.text);
    }

    public void OnJoinRoomButton()
    {
        if (_inputPlayerName.text.Length == 0)
        {
            UIManager.Instance.ShowMessageBox("Please enter player name.", null, true, false);
            return;
        }

        if (_selectedPanel == null)
        {
            UIManager.Instance.ShowMessageBox("Please select a room.", null, true, false);
            return;
        }

        PhotonNetwork.LocalPlayer.NickName = _inputPlayerName.text;
        PhotonNetwork.JoinRoom(_selectedPanel.roomName);
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

    public void AddRoomSelectionPanel(string roomName)
    {
        if (_roomSelectionPanels.ContainsKey(roomName))
        {
            return;
        }

        RoomSelectionPanel panel = Instantiate(
            _prefabRoomSelectionPanel, _prefabRoomSelectionPanel.transform.parent);
        panel.selected = false;
        panel.roomName = roomName;
        panel.gameObject.SetActive(true);
        _roomSelectionPanels[roomName] = panel;
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
}
