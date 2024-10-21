using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUIController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _label;
    [SerializeField]
    private Image _imageFill;

    private WeaponController _weapon;
    public WeaponController weapon
    {
        get => _weapon;
        set
        {
            if (_weapon != null)
            {
                weapon.Action -= onWeaponStateUpdate;
            }
            _weapon = value;
            if (_weapon != null)
            {
                weapon.Action += onWeaponStateUpdate;
                onWeaponStateUpdate();
            }
            gameObject.SetActive(_weapon != null);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (weapon != null && weapon.WeaponState == WeaponState.RELOAD)
        {
            float fill = (Time.time - weapon.ReloadStartTime) / weapon.ReloadDuration;
            _imageFill.fillAmount = Mathf.Clamp(1 - fill, 0f, 1f);
        }
    }

    private void onWeaponStateUpdate()
    {
        _label.text = string.Format("{0} / {1}", weapon.CurrentBullets, weapon.MaxBullets);
    }
}
