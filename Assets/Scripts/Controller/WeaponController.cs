using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum WeaponState
{
    READY,
    PRE_RECOIL,
    RECOIL,
    RELOAD,
}

public class WeaponController : MonoBehaviour
{
    [SerializeField]
    private int _maxBullets = 7;
    public int MaxBullets => _maxBullets;

    [SerializeField]
    private float _reloadDuration = 2f;
    public float ReloadDuration => _reloadDuration;

    [SerializeField]
    private float _recoilDuration = 0.3f;
    [SerializeField]
    private int _bulletDamage;
    [SerializeField]
    private BulletController _prefabBullet;
    [SerializeField]
    private Transform _barrel;

    private Animator _animator;
    private int _animatorParameterIdFire;
    private int _animatorReadyState;
    private Transform _cameraMainTransform;
    public WeaponState WeaponState { get; private set; }

    public float ReloadStartTime { get; private set; }

    private int _currentBullets;
    public int CurrentBullets
    {
        get => _currentBullets;
        private set
        {
            _currentBullets = value;
            Action?.Invoke();
        }
    }

    private float _recoilStartTime;

    public Action Action;

    private void Awake()
    {
        _animator = null;
        _animatorParameterIdFire = Animator.StringToHash("Fire");
        _cameraMainTransform = Camera.main.transform;
    }

    public void Initialize()
    {
        CurrentBullets = MaxBullets;
        WeaponState = WeaponState.READY;
    }

    public void FireBullet(Transform parent, Vector3 direction, PlayerController sourcePlayer)
    {
        if (WeaponState != WeaponState.READY || CurrentBullets == 0)
        {
            return;
        }

        GameObject bulletObject = PhotonNetwork.Instantiate(
            "Bullet", _barrel.transform.position, Quaternion.identity);
        BulletController bulletController = bulletObject.GetComponent<BulletController>();
        bulletController.direction = direction;
        bulletController.sourcePlayerClientId = sourcePlayer.playerId;
        bulletController.damage = _bulletDamage;

        if (_animator != null) {
            _animatorReadyState = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            _animator.SetTrigger(_animatorParameterIdFire);
        }

        --CurrentBullets;
        if (CurrentBullets > 0)
        {
            WeaponState = WeaponState.PRE_RECOIL;
        }
        else
        {
            ReloadStartTime = Time.time;
            WeaponState = WeaponState.RELOAD;
        }
    }

    private void Update()
    {
        switch (WeaponState)
        {
            case WeaponState.PRE_RECOIL:
                if (_animator == null ||
                    _animator.GetCurrentAnimatorStateInfo(0).shortNameHash != _animatorReadyState) {
                    WeaponState = WeaponState.RECOIL;
                    _recoilStartTime = Time.time;
                }
                break;

            case WeaponState.RECOIL:
                if (_animator == null)
                {
                    if (Time.time - _recoilStartTime > _recoilDuration)
                    {
                        WeaponState = WeaponState.READY;
                    }
                }
                else
                {
                    if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash == _animatorReadyState)
                    {
                        WeaponState = WeaponState.READY;
                    }
                }
                break;

            case WeaponState.RELOAD:
                if (Time.time - ReloadStartTime >= ReloadDuration)
                {
                    CurrentBullets = MaxBullets;
                    WeaponState = WeaponState.READY;
                }
                break;

            default:
                break;
        }
    }
}
