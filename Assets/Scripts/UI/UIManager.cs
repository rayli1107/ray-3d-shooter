using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _panelBackground;

    [SerializeField]
    private BaseMessageBox _panelMessageBox;
    [field: SerializeField]
    public WeaponUIController WeaponUIController { get; private set; }

    public static UIManager Instance { get; private set; }
    private List<ModalObject> _modalObjects;
    private float _timeScale;

    private void Awake()
    {
        Instance = this;
        _modalObjects = new List<ModalObject>();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public void RegisterModalItem(ModalObject modalObject)
    {
        if (_modalObjects.Count == 0)
        {
            _timeScale = Time.timeScale;
            Time.timeScale = 0;
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
            Time.timeScale = _timeScale;
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

 
    public void OnConnectButton()
    {
        PhotonNetworkManager.Instance.PhotonConnect();
    }
}


