using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class WeaponPurchaseSubPanel : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _labelWeaponName;

    [SerializeField]
    private TextMeshProUGUI _labelWeaponCost;


    [SerializeField]
    private Image _imageWeapon;

    [SerializeField]
    private Transform _panelPurchaseButton;

    [SerializeField]
    private Button _buttonPurchase;


    private PlayerController _playerController;
    private PlayerWeaponManager _playerWeaponManager;
    private int _weaponIndex;

    public void Initialize(
        PlayerController playerController,
        PlayerWeaponManager playerWeaponManager,
        int weaponIndex)
    {
        _playerController = playerController;
        _playerWeaponManager = playerWeaponManager;
        _weaponIndex = weaponIndex;

        WeaponController weapon = playerWeaponManager.GetWeapon(weaponIndex);
        _labelWeaponName.text = weapon.weaponName;
        _labelWeaponCost.text = weapon.weaponCost.ToString();
        _imageWeapon.sprite = weapon.weaponSprite;
    }

    private void OnEnable()
    {
        _playerWeaponManager.weaponDataUpdate += onWeaponUnlockedUpdate;
        _playerController.statUpdateAction += onPlayerStatUpdate;
        onWeaponUnlockedUpdate();
        onPlayerStatUpdate();
    }

    private void OnDisable()
    {
        _playerWeaponManager.weaponDataUpdate -= onWeaponUnlockedUpdate;
        _playerController.statUpdateAction -= onPlayerStatUpdate;
    }

    private void onPlayerStatUpdate()
    {
        WeaponController weapon = _playerWeaponManager.GetWeapon(_weaponIndex);
        _buttonPurchase.enabled = _playerController.Coins >= weapon.weaponCost;
    }
    private void onWeaponUnlockedUpdate()
    {
        _panelPurchaseButton.gameObject.SetActive(
            !_playerWeaponManager.IsWeaponUnlocked(_weaponIndex));
    }

    public void onWeaponPurchaseButton()
    {
        _playerWeaponManager.PurchaseWeapon(_weaponIndex);
    }
}
