using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUIController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _labelWeaponName;

    [SerializeField]
    private TextMeshProUGUI _labelBullets;

    [SerializeField]
    private Image _imageWeapon;

    [SerializeField]
    private Image _imageFill;

    private PlayerWeaponManager _weaponManager;
    public PlayerWeaponManager weaponManager
    {
        get => _weaponManager;
        set
        {
            if (_weaponManager != null)
            {
                _imageWeapon.sprite = null;
                _labelWeaponName.text = "";
                _weaponManager.weaponDataUpdate -= onWeaponStateUpdate;
            }

            _weaponManager = value;

            if (_weaponManager != null)
            {
                _weaponManager.weaponDataUpdate += onWeaponStateUpdate;
                onWeaponStateUpdate();
            }
            gameObject.SetActive(_weaponManager != null);
        }
    }

/*
 * private WeaponController _weapon;
    public WeaponController weapon
    {
        get => _weapon;
        set
        {
            if (_weapon != null)
            {
                _imageWeapon.sprite = null;
                _labelWeaponName.text = "";
                weapon.Action -= onWeaponStateUpdate;
            }
            _weapon = value;
            if (_weapon != null)
            {
                _imageWeapon.sprite = weapon.weaponSprite;
                _labelWeaponName.text = weapon.weaponName;
                weapon.Action += onWeaponStateUpdate;
                onWeaponStateUpdate();
            }
            gameObject.SetActive(_weapon != null);
        }
    }
*/

    // Update is called once per frame
    void Update()
    {
        if (weaponManager != null)
        {
            WeaponController weapon = weaponManager.CurrentWeapon;
            if (weapon != null && weapon.WeaponState == WeaponState.RELOAD)
            {
                float fill = (Time.time - weapon.ReloadStartTime) / weapon.reloadDuration;
                _imageFill.fillAmount = Mathf.Clamp(1 - fill, 0f, 1f);
            }
            else
            {
                _imageFill.fillAmount = 0;
            }
        }
    }

    private void onWeaponStateUpdate()
    {
        if (weaponManager == null)
        {
            _imageWeapon.sprite = null;
            _labelWeaponName.text = "";
            _labelBullets.text = "";
        }
        else
        {
            WeaponController weapon = weaponManager.CurrentWeapon;
            _imageWeapon.sprite = weapon.weaponSprite;
            _labelWeaponName.text = weapon.weaponName;
            _labelBullets.text = string.Format(
                "{0} / {1}",
                weaponManager.GetBulletCount(weaponManager.currentWeaponIndex),
                weapon.maxBullets);

        }
    }
}
