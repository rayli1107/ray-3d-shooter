using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _panelBackground;

    [SerializeField]
    private BaseMessageBox _panelMessageBox;

    [field: SerializeField]
    public WeaponUIController WeaponUIController { get; private set; }

    [field: SerializeField]
    public LobbyPanel LobbyPanel { get; private set; }

    [field: SerializeField]
    public LoginPanel LoginPanel { get; private set; }

    [field: SerializeField]
    public ModalObject GameMenu { get; private set; }

    public static UIManager Instance { get; private set; }
    private List<ModalObject> _modalObjects;
    private float _timeScale;

    private PlayerInput _playerInput;
    private InputAction _actionEscape;

    private void Awake()
    {
        Instance = this;
        _modalObjects = new List<ModalObject>();
        _playerInput = GetComponent<PlayerInput>();
        _actionEscape = _playerInput.actions["Escape"];
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    private void Update()
    {
        if (_actionEscape.triggered)
        {
            if (_modalObjects.Count > 0)
            {
                _modalObjects[_modalObjects.Count - 1].OnCancel();
            }
            else if (PhotonNetworkManager.Instance.inRoom)
            {
                GameMenu.gameObject.SetActive(true);
            }
        }
    }

    public void RegisterModalItem(ModalObject modalObject)
    {
        if (_modalObjects.Count == 0)
        {
            if (GameController.Instance.activePlayer != null)
            {
                GameController.Instance.activePlayer.enableInput = false;
            }
            _panelBackground.gameObject.SetActive(true);
        }
        _modalObjects.Add(modalObject);
    }

    public void UnregisterModalItem(ModalObject modalObject)
    {
        _modalObjects.Remove(modalObject);
        if (_modalObjects.Count == 0)
        {
            _panelBackground.gameObject.SetActive(false);
            if (GameController.Instance.activePlayer != null)
            {
                GameController.Instance.activePlayer.enableInput = true;
            }
        }
    }

    public void ShowMessageBox(
        string message,
        MessageBoxHandler handler = null,
        bool showButtonOk = false,
        bool showButtonCancel = false)
    {
        _panelMessageBox.message = message;
        _panelMessageBox.callbackHandler = handler;
        _panelMessageBox.buttonOKEnabled = showButtonOk;
        _panelMessageBox.buttonCancelEnabled = showButtonCancel;
        _panelMessageBox.gameObject.SetActive(true);
    }

    public void HideMessageBox()
    {
        _panelMessageBox.gameObject.SetActive(false);
    }

    private void exitGameMessageBoxHandler(bool ok)
    {
        if (ok)
        {
            GameMenu.gameObject.SetActive(false);
            PhotonNetworkManager.Instance.LeaveRoom();
        }
    }

    public void OnGameMenuExitGame()
    {
        ShowMessageBox("Return to lobby?", exitGameMessageBoxHandler, true, true);
    }
}


