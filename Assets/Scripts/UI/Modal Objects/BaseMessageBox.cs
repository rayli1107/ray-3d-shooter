using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public delegate void MessageBoxHandler(bool ok);

public class BaseMessageBox : ModalObject
{
    [SerializeField]
    private TextMeshProUGUI _labelMessage;

    [SerializeField]
    private GameObject _panelButtonOK;

    [SerializeField]
    private GameObject _panelButtonCancel;

    public bool buttonOKEnabled
    {
        get => _panelButtonOK != null ? _panelButtonOK.activeInHierarchy : false;
        set
        {
            if (_panelButtonOK != null)
            {
                _panelButtonOK.gameObject.SetActive(value);
            }
        }
    }

    public bool buttonCancelEnabled
    {
        get => _panelButtonCancel != null ? _panelButtonCancel.activeInHierarchy : false;
        set
        {
            if (_panelButtonCancel != null)
            {
                _panelButtonCancel.gameObject.SetActive(value);
            }
        }
    }
    public string message
    {
        get => _labelMessage != null ? _labelMessage.text : null;
        set
        {
            if (_labelMessage != null)
            {
                _labelMessage.text = value;
            }
        }
    }

    [HideInInspector]
    public MessageBoxHandler callbackHandler;

    public void OnButton(bool ok)
    {
        gameObject.SetActive(false);
        callbackHandler?.Invoke(ok);
    }

    public override void OnCancel()
    {
        if (buttonCancelEnabled)
        {
            OnButton(false);
        }
        else if (buttonOKEnabled)
        {
            OnButton(true);
        }
    }
}
