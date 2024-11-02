using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using System;
using ExitGames.Client.Photon;
using Photon.Realtime;

/*public class PlayerData : INetworkSerializable
{
    private int _maxHP;
    public int maxHP
    {
        get => _maxHP;
        set
        {
            _maxHP = value;
            currentHP = currentHP;
        }
    }

    private int _currentHP;
    public int currentHP
    {
        get => _currentHP;
        set { _currentHP = Mathf.Clamp(value, 0, maxHP); }
    }

    public PlayerData()
    {
    }

    public PlayerData(int hp)
    {
        maxHP = hp;
        currentHP = hp;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            FastBufferReader reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out int value);
            maxHP = value;

            reader.ReadValueSafe(out value);
            currentHP = value;
        }
        else
        {
            FastBufferWriter writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(maxHP);
            writer.WriteValueSafe(currentHP);
        }
    }
}
*/

public class PhotonNetworkVariable
{
    public Action updateAction;
    private PlayerController _playerController;
    public bool isMine => _playerController.isMine;
    public static string actorId = "ActorId";
    public string key { get; private set; }
    private object _value;
    public object value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                updateAction?.Invoke();
            }
        }
    }

    public PhotonNetworkVariable(string key, object value, Action updateAction, PlayerController playerController)
    {
        this.updateAction = updateAction;
        this.key = key;
        _playerController = playerController;
        _value = value;
    }
}

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField]
    private Animator _animator;
    [field: SerializeField]
    public Transform rotationTransform { get; private set; }
    [SerializeField]
    private float _playerWalkSpeed = 4.0f;
    [SerializeField]
    private float _playerRunSpeed = 8.0f;
    [SerializeField]
    private float _playerRotateSpeed = 10.0f;
    [SerializeField]
    private float _jumpHeight = 1.0f;
    [SerializeField]
    private float _gravity = -9.81f;
    [SerializeField]
    private float _animationSmoothTime = 0.1f;
    [SerializeField]
    private float _viewAngle = 45f;

    [SerializeField]
    private Transform _bulletParent;
    [SerializeField]
    private Transform _aimTarget;
    [field: SerializeField]
    public WeaponController Weapon { get; private set; }
    [SerializeField]
    private int _defaultHp = 10;
    public int maxHP => _defaultHp;
    public int HP
    {
        get => (int)_networkVariableHP.value;
        set
        {
            int newValue = Mathf.Clamp(value, 0, maxHP);
            if (HP != newValue)
            {
                _networkVariableHP.value = newValue;
            }
        }
    }

    private PhotonNetworkVariable _networkVariableHP;

    public Action statUpdateAction;

    public bool enableInput
    {
        set { if (value) _playerInput.ActivateInput(); else _playerInput.DeactivateInput(); }
    }

    public PlayerUIController PlayerUIController { get; private set; }
    public int playerId => _photonView.ControllerActorNr;
    public string playerName => _photonView.Controller.NickName;
    public bool isMine => _photonView.IsMine;
    
    private CharacterController _characterController;
    private PlayerInput _playerInput;
    private PhotonView _photonView;
    private int _animatorParameterIdMoveX;
    private int _animatorParameterIdMoveZ;
    private int _animatorParameterIdRun;

    private InputAction _actionMove;
    private InputAction _actionTarget;
    private InputAction _actionShoot;

    private Vector2 _currentAnimationBlendVector = Vector2.zero;
    private Vector2 _currentAnimationVelocity;

    private Vector3 _currentDirectionForward;
    private Vector3 _currentDirectionRight;
    private float _currentDirectionAngleNonPlayer;

    private PlayerController _enemyTarget;
    private PlayerController EnemyTarget
    {
        get => _enemyTarget;
        set
        {
            if (_enemyTarget != null)
            {
                _enemyTarget.PlayerUIController.IsTarget = false;
            }
            _enemyTarget = value;
            if (_enemyTarget != null && !_enemyTarget.isMine)
            {
                _enemyTarget.PlayerUIController.IsTarget = true;
            }
        }
    }

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        PlayerUIController = GetComponent<PlayerUIController>();
        _characterController = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();

        _animatorParameterIdMoveX = Animator.StringToHash("MoveX");
        _animatorParameterIdMoveZ = Animator.StringToHash("MoveZ");
        _animatorParameterIdRun = Animator.StringToHash("Running");

        _actionMove = _playerInput.actions["Move"];
        _actionTarget = _playerInput.actions["Target"];
        _actionTarget.performed += onTargetAction;

        _actionShoot = _playerInput.actions["Shoot"];
    }

    public override void OnEnable()
    {
        base.OnEnable();
        Weapon.Initialize();
        GameController.Instance.RegisterPlayer(this);
        _networkVariableHP = new PhotonNetworkVariable("HP", maxHP, onStatUpdate, this);
        PlayerUIController.enabled = true;
    }

    public override void OnDisable()
    {
        GameController.Instance.UnregisterPlayer(this);
        base.OnDisable();
    }

    /*
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _weapon.Initialize();
        GameController.Instance.RegisterPlayer(this);
        if (IsOwner)
        {
            playerData.Value = new PlayerData(_defaultHp);
            GameController.Instance.SetActivePlayer(this);
        }
        PlayerUIController.enabled = true;
    }

    public override void OnNetworkDespawn()
    {
        PlayerUIController.enabled = false;
        if (IsOwner)
        {
            GameController.Instance.SetActivePlayer(null);
        }
        GameController.Instance.UnregisterPlayer(OwnerClientId);
        base.OnNetworkDespawn();
    }
    */

    // Start is called before the first frame update
    void Start()
    {
        if (isMine)
        {
            Vector3 target = new Vector3(0, transform.position.y, 0);
            rotationTransform.LookAt(target);
            updateDirection();
        }
    }

    private void updateDirection()
    {
        _currentDirectionForward = rotationTransform.forward;
        _currentDirectionRight = rotationTransform.right;
    }

    // Update is called once per frame
    private void Update()
    {
        if (isMine)
        {
            if (HP == 0)
            {
                PhotonNetwork.Destroy(_photonView);
                UIManager.Instance.ShowMessageBox(
                    "You Lose!", (_) => { PhotonNetworkManager.Instance.LeaveRoom(); }, true);
                return;
            }
            Vector2 inputMove = _actionMove.ReadValue<Vector2>();
            float right = Pointer.current.delta.right.ReadValue() - Pointer.current.delta.left.ReadValue();
 
            if (EnemyTarget != null)
            {
                Vector3 enemyPosition = new Vector3(
                    EnemyTarget.transform.position.x,
                    transform.position.y,
                    EnemyTarget.transform.position.z);
                rotationTransform.LookAt(enemyPosition);
                updateDirection();
                /*
                if (inputMagnitude > 0.01)
                {
                    Vector2 forward = new Vector2(_currentDirectionForward.x, _currentDirectionForward.z);
                    float angle = Vector2.SignedAngle(inputMove, forward) * Mathf.Deg2Rad;
                    inputMove.y = Mathf.Cos(angle) * inputMagnitude;
                    inputMove.x = Mathf.Sin(angle) * inputMagnitude;
                }*/
            }
            else if (right != 0)
            {
                rotationTransform.Rotate(Vector3.up, right * Time.deltaTime * _playerRotateSpeed);
                updateDirection();
            }

/*            if (inputMove.magnitude > 0.01)
            {
                float angleForward = Mathf.Rad2Deg * Mathf.Atan2(inputMove.y, inputMove.x) - 90;
                Quaternion rotation = getRotation(angleForward);
                if (rotationTransform.rotation != rotation)
                {
                    rotationTransform.rotation = rotation;
                    _photonView.RPC("SetDirection", RpcTarget.OthersBuffered, angleForward);
                }
                _currentDirectionForward = rotationTransform.forward;
                _currentDirectionRight = rotationTransform.right;
                inputMove.x = 0;
                inputMove.y = inputMagnitude;
            }
*/

            _currentAnimationBlendVector = Vector2.SmoothDamp(
                _currentAnimationBlendVector,
                inputMove,
                ref _currentAnimationVelocity,
                _animationSmoothTime);

            Vector3 move =
                _currentAnimationBlendVector.x * _currentDirectionRight +
                _currentAnimationBlendVector.y * _currentDirectionForward;
            move.y = 0f;
            if (move.magnitude > 0.001f)
            {
                move = move * Time.deltaTime * _playerRunSpeed;
                _characterController.Move(move);
            }

            _animator.SetFloat(_animatorParameterIdMoveX, _currentAnimationBlendVector.x);
            _animator.SetFloat(_animatorParameterIdMoveZ, _currentAnimationBlendVector.y);
            _animator.SetFloat(_animatorParameterIdRun, 1.0f);

            if (_actionShoot.ReadValue<float>() > 0.5f)
            {
                Weapon.FireBullet(_bulletParent, _currentDirectionForward, this);
            }
        }
        else
        {
            if (EnemyTarget != null)
            {
                Vector3 enemyPosition = new Vector3(
                    EnemyTarget.transform.position.x,
                    transform.position.y,
                    EnemyTarget.transform.position.z);
                rotationTransform.LookAt(enemyPosition);
            }
        }
    }

    public void onShootAction(InputAction.CallbackContext context)
    {
//        _weapon.FireBullet(_bulletParent, forward, this);
    }
    /*
    private void onEnemyStateChange()
    {
        if (EnemyTarget.HP <= 0)
        {
            EnemyTarget = null;
        }
    }*/

    public void onTargetAction(InputAction.CallbackContext context)
    {
        if (isMine)
        {
            if (EnemyTarget == null)
            {
                EnemyTarget = GameController.Instance.GetClosestTarget();
            }
            else
            {
                EnemyTarget = null;
            }

            _photonView.RPC(
                "SetTarget",
                RpcTarget.OthersBuffered,
                EnemyTarget == null ? -1 : EnemyTarget.playerId);
        }
    }

    public bool IsFacing(Vector3 position)
    {
        Vector3 positionDelta = position - transform.position;
        positionDelta.y = 0;
        Vector3 playerForward = rotationTransform.forward;
        playerForward.y = 0;
        return Vector3.Angle(playerForward, positionDelta) <= _viewAngle;
    }

    public void OnBulletHit(int damage)
    {
        int newHP = Mathf.Clamp(HP - damage, 0, maxHP);
        if (HP != newHP)
        {
            Hashtable table = new();
            table.Add(_networkVariableHP.key, newHP);
            PhotonNetwork.LocalPlayer.SetCustomProperties(table);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer,Hashtable properties)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, properties);

        object value;
        if (targetPlayer == _photonView.Owner &&
            properties.TryGetValue(_networkVariableHP.key, out value))
        {
            _networkVariableHP.value = value;
        }
    }

    private void onStatUpdate()
    {
        statUpdateAction?.Invoke();
    }

    [PunRPC]
    public void SetTarget(int playerId)
    {
        if (!_photonView.IsMine)
        {
            EnemyTarget = GameController.Instance.GetPlayer(playerId);
            if (EnemyTarget == null)
            {
                rotationTransform.rotation = getRotation(_currentDirectionAngleNonPlayer);
            }
        }
    }

    [PunRPC]
    public void SetDirection(float angle)
    {
        if (!_photonView.IsMine)
        {
            _currentDirectionAngleNonPlayer = angle;
            if (EnemyTarget == null)
            {
                rotationTransform.rotation = getRotation(angle);
            }
        }
    }

    private Quaternion getRotation(float angle)
    {
        return Quaternion.Euler(0, -1 * angle, 0);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rotationTransform.rotation);
        }
        else if (stream.IsReading)
        {
            rotationTransform.rotation = (Quaternion)stream.ReceiveNext();
        }
    }
}
