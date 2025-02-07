using System;
using UnityEngine;
using UnityEngine.UI;

public enum EnemyState
{
    APPROACHING,
    IDLE,
    STRAFING,
}

public class EnemyController : MonoBehaviour
{
    [SerializeField]
    private Slider _healthBarSlider;
    [SerializeField]
    private Image _targetingImage;
    [SerializeField]
    private float _idleDuration = 1f;
    [SerializeField]
    private float _strafeDuration = 1.5f;
    [SerializeField]
    private Transform _target;
    [SerializeField]
    private Transform _selfRotation;
    [SerializeField]
    private float _distance = 20f;
    [SerializeField]
    private float _speed = 5f;
    [SerializeField]
    private float _animationSmoothTime = 0.1f;
    [SerializeField]
    private WeaponController _weapon;
    [SerializeField]
    private Transform _bulletParent;

    private CharacterController _characterController;
    private Animator _animator;
    private int _animatorParameterIdMoveX;
    private int _animatorParameterIdMoveZ;
    private int _animatorParameterIdRun;

    private EnemyState _enemyState;
    private float _stateStartTime;
    public EnemyState EnemyState
    {
        get => _enemyState;
        private set
        {
            _stateStartTime = Time.time;
            _enemyState = value;
        }
    }


    private int _maxHP;
    private int MaxHP
    {
        get => _maxHP;
        set {
            _maxHP = value;
            _healthBarSlider.minValue = 0;
            _healthBarSlider.maxValue = _maxHP;
            HP = Mathf.Clamp(HP, 0, _maxHP);
        }
    }

    private int _hp;
    public int HP
    {
        get => _hp;
        private set
        {
            _hp = value;
            _healthBarSlider.value = _hp;
        }
    }


    public bool IsTarget
    {
        get => _targetingImage.enabled;
        set { _targetingImage.enabled = value; }
    }

    public Action Action;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animatorParameterIdMoveX = Animator.StringToHash("MoveX");
        _animatorParameterIdMoveZ = Animator.StringToHash("MoveZ");
        _animatorParameterIdRun = Animator.StringToHash("Running");

        _characterController = GetComponent<CharacterController>();
        IsTarget = false;
    }

    private void OnEnable()
    {
        Initialize(5);
    }

    public void Initialize(int maxHP)
    {
        MaxHP = maxHP;
        HP = maxHP;
        EnemyState = EnemyState.IDLE;
//        _weapon.Initialize();
    }

    public void OnBulletHit(int damage)
    {
        HP = Mathf.Max(0, HP - damage);
        if (HP <= 0)
        {
            Destroy(gameObject);
        }
    }

    private bool _strafeRight;
    private Vector2 _currentAnimationBlendVector = Vector2.zero;
    private Vector2 _currentAnimationVelocity;

    private void Update()
    {
        Vector3 targetPosition = new Vector3(_target.position.x, transform.position.y, _target.position.z);
        _selfRotation.LookAt(targetPosition);

        Vector3 positionDelta = targetPosition - transform.position;
        Vector2 move = Vector2.zero;

        switch (EnemyState)
        {
            case EnemyState.IDLE:
                if (positionDelta.magnitude >= _distance)
                {
                    EnemyState = EnemyState.APPROACHING;
                }
                else if (Time.time - _stateStartTime >= _idleDuration)
                {
                    _strafeRight = GameController.Instance.Random.Next(2) == 0;
                    EnemyState = EnemyState.STRAFING;
                }
                break;

            case EnemyState.STRAFING:
                if (positionDelta.magnitude >= _distance)
                {
                    EnemyState = EnemyState.APPROACHING;
                }
                else if (Time.time - _stateStartTime >= _strafeDuration)
                {
                    EnemyState = EnemyState.IDLE;
                }
                else
                {
                    move = _strafeRight ? Vector2.right : Vector2.left;
                }
                break;

            case EnemyState.APPROACHING:
                if (positionDelta.magnitude < _distance)
                {
                    EnemyState = EnemyState.IDLE;
                }
                else
                {
                    move = Vector2.up;
                }
                break;
        }

        _currentAnimationBlendVector = Vector2.SmoothDamp(
            _currentAnimationBlendVector,
            move,
            ref _currentAnimationVelocity,
            _animationSmoothTime);
        if (_currentAnimationBlendVector.magnitude > 0)
        {
            Vector3 actualMove =
                _currentAnimationBlendVector.x * _selfRotation.right +
                _currentAnimationBlendVector.y * _selfRotation.forward;
            actualMove.y = 0;
            _characterController.Move(actualMove * Time.deltaTime * _speed);
        }
        _animator.SetFloat(_animatorParameterIdMoveX, _currentAnimationBlendVector.x);
        _animator.SetFloat(_animatorParameterIdMoveZ, _currentAnimationBlendVector.y);
        _animator.SetFloat(_animatorParameterIdRun, 1.0f);

        if (_enemyState != EnemyState.APPROACHING)
        {
//            _weapon.FireBullet(_bulletParent, _selfRotation.forward, null);
        }
    }
}
