using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerData : INetworkSerializable
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

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera _cameraThirdPerson;
    [SerializeField]
    private CinemachineVirtualCamera _cameraAim;
    [SerializeField]
    private Animator _animator;
    [SerializeField]
    private Transform _rotationTransform;
    [SerializeField]
    private float _playerWalkSpeed = 4.0f;
    [SerializeField]
    private float _playerRunSpeed = 8.0f;
    [SerializeField]
    private float _jumpHeight = 1.0f;
    [SerializeField]
    private float _gravity = -9.81f;
    [SerializeField]
    private float _rotationSpeed = 1f;
    [SerializeField]
    private float _animationSmoothTime = 0.1f;
    [SerializeField]
    private float _viewAngle = 45f;

    [SerializeField]
    private Transform _bulletParent;
    [SerializeField]
    private Transform _aimTarget;
    [SerializeField]
    private WeaponController _weapon;
    public WeaponController Weapon => _weapon;
    [SerializeField]
    private int _defaultHp = 10;

    public NetworkVariable<PlayerData> playerData { get; private set; }

    public PlayerUIController PlayerUIController { get; private set; }
    private CharacterController _characterController;
    private PlayerInput _playerInput;
    private int _animatorParameterIdMoveX;
    private int _animatorParameterIdMoveZ;
    private int _animatorParameterIdRun;
    private int _animatorParameterIdJump;

    private InputAction _actionMove;
    private InputAction _actionAim;
    private InputAction _actionTarget;
    private InputAction _actionShoot;
    private InputAction _actionRun;
    private float _velocityY;
    private Transform _cameraMainTransform;

    private Vector2 _currentAnimationBlendVector = Vector2.zero;
    private Vector2 _currentAnimationVelocity;
    private float _currentAnimationRunValue = 0f;
    private float _currentAnimationRunVelocity = 0f;

    private Vector3 _currentDirectionForward;
    private Vector3 _currentDirectionRight;

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
            if (_enemyTarget != null)
            {
                _enemyTarget.PlayerUIController.IsTarget = true;
            }
        }
    }

    public PlayerController() : base()
    {
        playerData = new NetworkVariable<PlayerData>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
    }

    private void Awake()
    {
        PlayerUIController = GetComponent<PlayerUIController>();
        _characterController = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();

        _animatorParameterIdMoveX = Animator.StringToHash("MoveX");
        _animatorParameterIdMoveZ = Animator.StringToHash("MoveZ");
        _animatorParameterIdRun = Animator.StringToHash("Running");
        _animatorParameterIdJump = Animator.StringToHash("Jump");

        _actionMove = _playerInput.actions["Move"];
        _actionAim = _playerInput.actions["Aim"];
        _actionTarget = _playerInput.actions["Target"];
        _actionTarget.performed += onTargetAction;

        _actionShoot = _playerInput.actions["Shoot"];
        _actionRun = _playerInput.actions["Run"];

        _cameraMainTransform = Camera.main.transform;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _weapon.Initialize();
        GameController.Instance.RegisterPlayer(OwnerClientId, this);
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

    // Start is called before the first frame update
    void Start()
    {
        _velocityY = 0;
        _currentDirectionForward = Vector3.forward;
        _currentDirectionRight = Vector3.right;
    }


    // Update is called once per frame
    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        Vector2 inputMove = _actionMove.ReadValue<Vector2>();
        float inputMagnitude = inputMove.magnitude;

        if (EnemyTarget != null)
        {
            Vector3 enemyPosition = new Vector3(
                EnemyTarget.transform.position.x,
                transform.position.y,
                EnemyTarget.transform.position.z);
            _rotationTransform.LookAt(enemyPosition);
            _currentDirectionForward = _rotationTransform.forward;
            _currentDirectionRight = _rotationTransform.right;

            if (inputMagnitude > 0.01)
            {
                Vector2 forward = new Vector2(_currentDirectionForward.x, _currentDirectionForward.z);
                float angle = Vector2.SignedAngle(inputMove, forward) * Mathf.Deg2Rad;
                inputMove.y = Mathf.Cos(angle) * inputMagnitude;
                inputMove.x = Mathf.Sin(angle) * inputMagnitude;
            }
        }
        else if (inputMove.magnitude > 0.01)
        {
            float angleForward = Mathf.Rad2Deg * Mathf.Atan2(inputMove.y, inputMove.x) - 90;
            _rotationTransform.rotation = Quaternion.Euler(0, -1 * angleForward, 0);
            //            _currentDirectionForward = _rotationTransform.rotation * Vector3.forward;
            //            _currentDirectionRight = _rotationTransform.rotation * Vector3.right;
            _currentDirectionForward = _rotationTransform.forward;
            _currentDirectionRight = _rotationTransform.right;
            inputMove.x = 0;
            inputMove.y = inputMagnitude;
        }

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
            _weapon.FireBullet(_bulletParent, _currentDirectionForward, this);
        }
    }

    /*
    void UpdateOld()
    {
        bool isGrounded = _characterController.isGrounded;
        if (isGrounded && _velocityY < 0)
        {
            _velocityY = 0f;
        }

        Vector2 inputMove = _actionMove.ReadValue<Vector2>();

        _currentAnimationBlendVector = Vector2.SmoothDamp(
            _currentAnimationBlendVector,
            inputMove,
            ref _currentAnimationVelocity,
            _animationSmoothTime);

        _currentAnimationRunValue = Mathf.SmoothDamp(
            _currentAnimationRunValue,
            _actionRun.ReadValue<float>(),
            ref _currentAnimationRunVelocity,
            _animationSmoothTime);

        Vector3 move =
            _currentAnimationBlendVector.x * _cameraMainTransform.right.normalized +
            _currentAnimationBlendVector.y * _cameraMainTransform.forward.normalized;
        move.y = 0f;
        if (move.magnitude > 0.001f)
        {
            float speed = Mathf.Lerp(_playerWalkSpeed, _playerRunSpeed, _currentAnimationRunValue);
            move = move * Time.deltaTime * speed;
            Debug.Log("move: " + move);
            CollisionFlags collision = _characterController.Move(move);
            if ((collision & CollisionFlags.Sides) != 0)
            {
                Debug.Log("Collision Sides");
            }
            if ((collision & CollisionFlags.Above) != 0)
            {
                Debug.Log("Collision Above");
            }
            if ((collision & CollisionFlags.Below) != 0)
            {
//                Debug.Log("Collision Below");
            }


        }
        _animator.SetFloat(_animatorParameterIdMoveX, _currentAnimationBlendVector.x);
        _animator.SetFloat(_animatorParameterIdMoveZ, _currentAnimationBlendVector.y);
        _animator.SetFloat(_animatorParameterIdRun, _currentAnimationRunValue);

        if (_actionJump.triggered && isGrounded)
        {
            _velocityY += Mathf.Sqrt(_jumpHeight * -3.0f * _gravity);
            _animator.SetTrigger(_animatorParameterIdJump);
        }
        _velocityY += _gravity * Time.deltaTime;
        _characterController.Move(
            new Vector3(0, _velocityY, 0) * Time.deltaTime);

        Debug.Log("Rotation: " + _rotationTransform.rotation);

        Quaternion targetRotation = Quaternion.Euler(0, _cameraMainTransform.eulerAngles.y, 0);
        _rotationTransform.rotation = Quaternion.Lerp(
            _rotationTransform.rotation,
            targetRotation,
            _rotationSpeed * Time.deltaTime);

        if (_actionAim.triggered)
        {
            bool isAiming = _cameraAim.Priority > _cameraThirdPerson.Priority;
            _cameraAim.Priority = _cameraThirdPerson.Priority + (isAiming ? -1 : 1);
        }

        _aimTarget.transform.position = _cameraMainTransform.position + _cameraMainTransform.forward * 20;
    }
    */
    public void onShootAction(InputAction.CallbackContext context)
    {
//        _weapon.FireBullet(_bulletParent);
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
        if (EnemyTarget == null)
        {
            EnemyTarget = GameController.Instance.GetClosestTarget();
        }
        else
        {
            EnemyTarget = null;
        }
    }

    public bool IsFacing(Vector3 position)
    {
        Vector3 positionDelta = position - transform.position;
        positionDelta.y = 0;
        Vector3 playerForward = _rotationTransform.forward;
        playerForward.y = 0;
        return Vector3.Angle(playerForward, positionDelta) <= _viewAngle;
    }

    [Rpc(SendTo.Owner)]
    public void OnBulletHitRpc(int damage)
    {
        playerData.Value.currentHP -= damage;
        playerData.SetDirty(true);
        playerData.OnValueChanged?.Invoke(null, playerData.Value);
    }
}
