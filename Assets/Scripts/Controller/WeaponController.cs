using ExitGames.Client.Photon;
using Photon.Pun;
using System;
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
    [field: SerializeField]
    public string weaponName { get; private set; }

    [field: SerializeField]
    public int weaponCost { get; private set; }


    [field: SerializeField]
    public Sprite weaponSprite { get; private set; }

    [field: SerializeField]
    public int maxBullets { get; private set; }

    [field: SerializeField]
    public float reloadDuration { get; private set; }

    [field: SerializeField]
    public float recoilDuration { get; private set; }

    [SerializeField]
    public int _bulletDamage = 1;

    [SerializeField]
    public float _bulletSpeed = 150f;

    [SerializeField]
    public float _bulletDuration = 1f;

    [SerializeField]
    public float _bulletSprayDegrees = 5f;

    [SerializeField]
    public int _bulletCount = 1;


    [SerializeField]
    private Transform _barrel;

    [field: SerializeField]
    public PlayerController player { get; private set; }

    [field: SerializeField]
    public PlayerWeaponManager playerWeaponManager { get; private set; }

    [field: SerializeField]
    public Transform rightHandGripHint { get; private set; }

    [field: SerializeField]
    public Transform leftHandGripHint { get; private set; }

    [HideInInspector]
    public int weaponIndex;

    private Animator _animator;
    private int _animatorParameterIdFire;
    private int _animatorReadyState;
    public WeaponState WeaponState { get; private set; }

    public float ReloadStartTime { get; private set; }


    private float _recoilStartTime;

    public Action Action;

    private void Awake()
    {
        _animator = null;
        _animatorParameterIdFire = Animator.StringToHash("Fire");
    }


    private void OnEnable()
    {
        if (player.photonView.IsMine)
        {
            int bulletCount = playerWeaponManager.GetBulletCount(weaponIndex);
            if (bulletCount == 0)
            {
                ReloadStartTime = Time.time;
                WeaponState = WeaponState.RELOAD;
            }
            else
            {
                WeaponState = WeaponState.READY;
            }
            Debug.LogFormat("Weapon.OnEnable {0} {1} {2}", weaponName, bulletCount, WeaponState);
        }
    }

    public void FireBullet(Vector3 direction)
    {
        direction = new Vector3(direction.x, 0, direction.z).normalized;
        Vector3 direction_right = Vector3.Cross(direction, Vector3.up);
        float delta = Mathf.Tan(Mathf.Deg2Rad * _bulletSprayDegrees);
        for (int i = 0; i < _bulletCount; ++i)
        {
            float deltaX = delta * ((float)GameController.Instance.Random.NextDouble() * 2 - 1);
            float deltaY = delta * ((float)GameController.Instance.Random.NextDouble() * 2 - 1);
            Vector3 newDirection = direction + deltaX * direction_right + deltaY * Vector3.up;

            BulletData bulletData = new();
            bulletData.ownerId = player.playerId;
            bulletData.velocity = _bulletSpeed * newDirection.normalized;
            bulletData.damage = _bulletDamage;
            bulletData.duration = _bulletDuration;
            object[] objectData = new object[1];
            objectData[0] = JsonUtility.ToJson(bulletData);

            PhotonNetwork.Instantiate(
                "Bullet", _barrel.transform.position, Quaternion.identity, 0, objectData);
        }
        if (_animator != null) {
            _animatorReadyState = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            _animator.SetTrigger(_animatorParameterIdFire);
        }

        if (playerWeaponManager.GetBulletCount(weaponIndex) > 0)
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
        if (!player.photonView.IsMine)
        {
            return;
        }

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
                    if (Time.time - _recoilStartTime > recoilDuration)
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
                if (Time.time - ReloadStartTime >= reloadDuration)
                {
                    playerWeaponManager.ReloadWeapon(weaponIndex);
                    WeaponState = WeaponState.READY;
                }
                break;

            default:
                break;
        }
    }
}
