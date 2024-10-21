using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModalObject : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        UIManager.Instance.RegisterModalItem(this);
    }

    protected virtual void OnDisable()
    {
        UIManager.Instance.UnregisterModalItem(this);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


}
