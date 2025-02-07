using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

[Serializable]
public class PlayerWeaponData
{
    public int[] bulletCount;
    public int currentWeaponIndex;
}

public class PlayerWeaponManager : MonoBehaviour
{
    [SerializeField]
    private TwoBoneIKConstraint _constraintRightHand;

    [SerializeField]
    private TwoBoneIKConstraint _constraintLeftHand;

    [SerializeField]
    private WeaponController[] _weapons;

    [SerializeField]
    private Transform _bulletParent;

    public WeaponController CurrentWeapon { get; private set; }
    public int weaponsCount => _weapons.Length;

    public Action weaponDataUpdate;

    private PhotonView _photonView;
    private PlayerController _playerController;
    private PlayerInput _playerInput;
    private InputAction _actionShoot;
    private InputAction _actionWeaponSelect;

    private PhotonNetworkVariable _networkVariableWeaponsData;
    private PlayerWeaponData _playerWeaponData;
    public int currentWeaponIndex => _playerWeaponData.currentWeaponIndex;

    public bool isMine => _photonView.IsMine;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _playerController = GetComponent<PlayerController>();
        _playerInput = GetComponent<PlayerInput>();
        _actionShoot = _playerInput.actions["Shoot"];
        _actionWeaponSelect = _playerInput.actions["Weapon Select"];

        for (int i = 0; i < _weapons.Length; ++i)
        {
            _weapons[i].weaponIndex = i;
        }
    }

    private void selectWeapon(int delta)
    {
        int index = _playerWeaponData.currentWeaponIndex;
        while (true)
        {
            index = (index + delta + _weapons.Length) % _weapons.Length;
            if (IsWeaponUnlocked(index))
            {
                _playerWeaponData.currentWeaponIndex = index;
                _networkVariableWeaponsData.LocalSetValue(
                    JsonUtility.ToJson(_playerWeaponData));
                return;
            }
        }
    }
/*
    private void onWeaponSelect()
    {
        for (int i = 0; i < _weapons.Length; ++i)
        {
            _weapons[i].gameObject.SetActive(
                i == _playerWeaponData.currentWeaponIndex);
        }
//        CurrentWeapon = _weapons[weaponIndex];
    }
*/

    private void onWeaponsDataUpdate()
    {
        _playerWeaponData = JsonUtility.FromJson<PlayerWeaponData>(
            (string)_networkVariableWeaponsData.value);
        CurrentWeapon = _weapons[_playerWeaponData.currentWeaponIndex];
        for (int i = 0; i < _weapons.Length; ++i)
        {
            _weapons[i].gameObject.SetActive(
                i == _playerWeaponData.currentWeaponIndex);
        }
        weaponDataUpdate?.Invoke();
    }

    private void OnEnable()
    {
        Debug.Log("PlayerWeaponManager.OnEnable");
        _playerWeaponData = new PlayerWeaponData();
        _playerWeaponData.bulletCount = new int[_weapons.Length];
        for (int i = 0; i < _playerWeaponData.bulletCount.Length; ++i)
        {
            _playerWeaponData.bulletCount[i] =
                _weapons[i].weaponCost == 0 ? _weapons[i].maxBullets : -1;
            if (_weapons[i].weaponCost == 0)
            {
                _playerWeaponData.currentWeaponIndex = i;
            }
        }

        string json = JsonUtility.ToJson(_playerWeaponData);
        Debug.LogFormat("Initial Weapon Data: {0}", json);

        _networkVariableWeaponsData = PhotonNetworkManager.Instance.RegisterNetworkVariable(
            _photonView, "Weapons Data", json, onWeaponsDataUpdate);
        onWeaponsDataUpdate();
        Debug.LogFormat("Actual Weapon Data {0}", (string)_networkVariableWeaponsData.value);

        if (!IsWeaponUnlocked(_playerWeaponData.currentWeaponIndex))
        {
            for (int i = 0; i < _playerWeaponData.bulletCount.Length; ++i)
            {
                if (_weapons[i].weaponCost == 0)
                {
                    _playerWeaponData.currentWeaponIndex = i;
                }
            }
            _networkVariableWeaponsData.LocalSetValue(JsonUtility.ToJson(_playerWeaponData));
        }

/*
        _networkVariableCurrentWeapon = PhotonNetworkManager.Instance.RegisterNetworkVariable(
            _photonView, "Current weapon", 0, onWeaponSelect);
        if (IsWeaponUnlocked(weaponIndex))
        {
            onWeaponSelect();
        }
        else
        {
            _networkVariableCurrentWeapon.LocalSetValue(0);
        }
*/
    }

    private void OnDisable()
    {
        PhotonNetworkManager.Instance.UnregisterNetworkVariable(_networkVariableWeaponsData);
//        PhotonNetworkManager.Instance.UnregisterNetworkVariable(_networkVariableCurrentWeapon);
 ///       PhotonNetworkManager.Instance.UnregisterNetworkVariable(_networkVariableUnlockedWeapons);
    }

    private void Update()
    {
        if (isMine)
        {
            float scrollValue = _actionWeaponSelect.ReadValue<float>();
            if (scrollValue > 0)
            {
                selectWeapon(1);
            }
            else if (scrollValue < 0)
            {
                selectWeapon(-1);
            }

            int weaponIndex = _playerWeaponData.currentWeaponIndex;
            if (_actionShoot.ReadValue<float>() > 0.5f &&
                _weapons[weaponIndex].WeaponState == WeaponState.READY &&
                FireBullet(weaponIndex))
            {
                _weapons[weaponIndex].FireBullet(_playerController.rotationTransform.forward);
            }
        }
    }

    public WeaponController GetWeapon(int index)
    {
        return _weapons[index];
    }

    public bool IsWeaponUnlocked(int index)
    {
        return _playerWeaponData.bulletCount[index] >= 0;
    }

    public void ReloadWeapon(int index)
    {
        if (_playerWeaponData.bulletCount[index] >= 0)
        {
            _playerWeaponData.bulletCount[index] = _weapons[index].maxBullets;
            weaponDataUpdate?.Invoke();
        }
    }

    public bool PurchaseWeapon(int index)
    {
        WeaponController weapon = GetWeapon(index);
        if (_playerController.Coins >= weapon.weaponCost && !IsWeaponUnlocked(index))
        {
            _playerController.AddCoins(-1 * weapon.weaponCost);

            _playerWeaponData.bulletCount[index] = weapon.maxBullets;
            weaponDataUpdate?.Invoke();
            _networkVariableWeaponsData.LocalSetValue(JsonUtility.ToJson(_playerWeaponData));
            return true;
        }
        return false;
    }

    public int GetBulletCount(int index)
    {
        return _playerWeaponData.bulletCount[index];
    }

    public bool FireBullet(int index)
    {
        int bulletCount = GetBulletCount(index);
        if (bulletCount > 0)
        {
            --_playerWeaponData.bulletCount[index];
            weaponDataUpdate?.Invoke();
            return true;
        }
        return false;
    }
}
