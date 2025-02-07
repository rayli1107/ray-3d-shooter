using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class WeaponPurchasePanel : ModalObject
{
    [SerializeField]
    private TextMeshProUGUI _labelCoins;

    [SerializeField]
    private WeaponPurchaseSubPanel _prefabWeaponPurchaseSubPanel;


    private List<WeaponPurchaseSubPanel> _weaponPurchaseSubPanels;

    private PlayerController _playerController;

    public void RegisterActivePlayer(PlayerController playerController)
    {
        if (_playerController != playerController)
        {
            if (_playerController != null && _weaponPurchaseSubPanels != null)
            {
                _weaponPurchaseSubPanels.ForEach(p => Destroy(p.gameObject));
                _weaponPurchaseSubPanels.Clear();
            }

            _playerController = playerController;

            if (_playerController == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                if (_weaponPurchaseSubPanels == null)
                {
                    _weaponPurchaseSubPanels = new List<WeaponPurchaseSubPanel>();
                }

                PlayerWeaponManager weaponManager = playerController.GetComponent<PlayerWeaponManager>();
                for (int weaponIndex = 0; weaponIndex < weaponManager.weaponsCount; ++weaponIndex)
                {
                    WeaponPurchaseSubPanel subPanel = Instantiate(
                        _prefabWeaponPurchaseSubPanel,
                        _prefabWeaponPurchaseSubPanel.transform.parent);
                    subPanel.Initialize(_playerController, weaponManager, weaponIndex);
                    subPanel.gameObject.SetActive(true);
                    _weaponPurchaseSubPanels.Add(subPanel);
                }
            }
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _playerController.statUpdateAction += onStatUpdate;
        onStatUpdate();
    }

    protected override void OnDisable()
    {
        _playerController.statUpdateAction -= onStatUpdate;
        base.OnDisable();
    }

    private void onStatUpdate()
    {
        _labelCoins.text = _playerController.Coins.ToString();
    }
}
