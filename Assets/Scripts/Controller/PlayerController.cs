using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using System;

public enum PlayerState {
    START,
    HP_INITIALIZED,
    REGISTERED
}

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [field: SerializeField]
    public PlayerUIController playerUIController { get; private set; }

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private int _defaultCoins = 20;

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
    private Transform _aimTarget;

    [SerializeField]
    private int _defaultHp = 10;

    public int maxHP => _defaultHp;

    private PhotonNetworkVariable _networkVariableHP;
    private PhotonNetworkVariable _networkVariableCoin;
    private PhotonNetworkVariable _networkVariablePlayerPosition;
    public int HP => (int)_networkVariableHP.value;
    public int Coins => _networkVariableCoin == null ? 0 : (int)_networkVariableCoin.value;
    public Action statUpdateAction;

    private PlayerState _playerState;

    public bool enableInput
    {
        set { if (value) _playerInput.ActivateInput(); else _playerInput.DeactivateInput(); }
    }

    public int playerId => photonView.ControllerActorNr;
    public string playerName => photonView.Controller.NickName;
    public string playerUserId => photonView.Controller.UserId;

    public bool isMine => photonView.IsMine;
    
    private CharacterController _characterController;
    private PlayerInput _playerInput;
    private int _animatorParameterIdMoveX;
    private int _animatorParameterIdMoveZ;
    private int _animatorParameterIdRun;

    private InputAction _actionMove;
    private InputAction _actionTarget;

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
                _enemyTarget.playerUIController.IsTarget = false;
            }
            _enemyTarget = value;
            if (_enemyTarget != null && !_enemyTarget.isMine)
            {
                _enemyTarget.playerUIController.IsTarget = true;
            }
        }
    }

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();

        _animatorParameterIdMoveX = Animator.StringToHash("MoveX");
        _animatorParameterIdMoveZ = Animator.StringToHash("MoveZ");
        _animatorParameterIdRun = Animator.StringToHash("Running");

        _actionMove = _playerInput.actions["Move"];
        _actionTarget = _playerInput.actions["Target"];
        _actionTarget.performed += onTargetAction;
    }

    private void loadPlayerTransform()
    {
        if (_networkVariablePlayerPosition.value != null)
        {
            try
            {
                PlayerTransform playerTransform = JsonUtility.FromJson<PlayerTransform>(
                    (string)_networkVariablePlayerPosition.value);
                transform.position = new Vector3(
                    playerTransform.x, transform.position.y, playerTransform.z);
                rotationTransform.rotation = Quaternion.Euler(
                    playerTransform.rotation_x,
                    playerTransform.rotation_y,
                    playerTransform.rotation_z);
            }
            catch (ArgumentException)
            {
            }
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        Debug.Log("PlayerWeaponManager.OnEnable");

        _networkVariableHP = PhotonNetworkManager.Instance.RegisterNetworkVariable(
            photonView, "HP", maxHP, onStatUpdate);
        if (photonView.IsMine && HP == 0)
        {
            _playerState = PlayerState.START;
            _networkVariableHP.LocalSetValue(maxHP);
        }
        else
        {
            _playerState = PlayerState.HP_INITIALIZED;
        }

        playerUIController.player = this;
        playerUIController.gameObject.SetActive(!photonView.IsMine);

        if (isMine)
        {
            _networkVariableCoin = PhotonNetworkManager.Instance.RegisterNetworkVariable(
                photonView, "Coin", _defaultCoins, onStatUpdate);

            _networkVariablePlayerPosition = PhotonNetworkManager.Instance.RegisterNetworkVariable(
                photonView, "Position", null, null);
            loadPlayerTransform();
            InvokeRepeating(nameof(SavePlayerTransform), 0, 0.5f);
        }
    }

    public override void OnDisable()
    {
        GameController.Instance.UnregisterPlayer(this);
        playerUIController.gameObject.SetActive(false);
        CancelInvoke(nameof(SavePlayerTransform));
        PhotonNetworkManager.Instance.UnregisterNetworkVariable(_networkVariableHP);
        if (_networkVariablePlayerPosition != null)
        {
            PhotonNetworkManager.Instance.UnregisterNetworkVariable(_networkVariablePlayerPosition);
        }
        if (_networkVariableCoin != null)
        {
            PhotonNetworkManager.Instance.UnregisterNetworkVariable(_networkVariableCoin);
        }
        base.OnDisable();
    }

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
        if (!isMine)
        {
            return;
        }

        if (_playerState == PlayerState.HP_INITIALIZED && GetComponent<PlayerWeaponManager>().enabled)
        {
            _playerState = PlayerState.REGISTERED;
            GameController.Instance.RegisterPlayer(this);
        }
        else if (_playerState == PlayerState.REGISTERED)
        {
            if (HP == 0)
            {
                _networkVariablePlayerPosition.LocalSetValue(null);
                GameController.Instance.OnActivePlayerDeath();
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
            }
            else if (right != 0)
            {
                rotationTransform.Rotate(Vector3.up, right * Time.deltaTime * _playerRotateSpeed);
                updateDirection();
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

            photonView.RPC(
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
        _networkVariableHP.LocalSetValue(Mathf.Clamp(HP - damage, 0, maxHP));
    }

    private void onStatUpdate()
    {
        if (_playerState == PlayerState.START)
        {
            _playerState = PlayerState.HP_INITIALIZED;
        }
        statUpdateAction?.Invoke();
    }

    [PunRPC]
    public void SetTarget(int playerId)
    {
        if (!photonView.IsMine)
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
        if (!photonView.IsMine)
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

    public void SavePlayerTransform()
    {
        PlayerTransform playerTransform = new();
        playerTransform.x = transform.position.x;
        playerTransform.z = transform.position.z;

        Vector3 eulerAngles = rotationTransform.rotation.eulerAngles;
        playerTransform.rotation_x = eulerAngles.x;
        playerTransform.rotation_y = eulerAngles.y;
        playerTransform.rotation_z = eulerAngles.z;

        _networkVariablePlayerPosition.LocalSetValue(JsonUtility.ToJson(playerTransform));
    }

    public void AddCoins(int coins)
    {
        if (isMine)
        {
            Debug.LogFormat("AddCoins {0}", coins);
            _networkVariableCoin.LocalSetValue((int)_networkVariableCoin.value + coins);
        }
    }
}
