using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera _cameraThirdPerson;

    [SerializeField]
    private CinemachineVirtualCamera _cameraAim;

    [SerializeField]
    private float _targetDistance = 200f;

    [SerializeField]
    private Transform _enemyParent;

    public static GameController Instance;
    public System.Random Random { get; private set; }

    private PlayerController _player;
    private List<EnemyController> _enemiesInView;
    private Dictionary<ulong, PlayerController> _players;

    private void Awake()
    {
        Instance = this;
        Random = new System.Random(Guid.NewGuid().GetHashCode());
        _enemiesInView = new List<EnemyController>();
        _players = new Dictionary<ulong, PlayerController>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetActivePlayer(PlayerController player)
    {
        _player = player;
        UIManager.Instance.WeaponUIController.weapon = player.Weapon;
        if (_cameraThirdPerson != null)
        {
            _cameraThirdPerson.Follow = player != null ? player.transform : null;
            _cameraThirdPerson.LookAt = player != null ? player.transform : null;
        }
        if (_cameraAim != null)
        {
            _cameraAim.Follow = player != null ? player.transform : null;
            _cameraAim.LookAt = player != null ? player.transform : null;
        }
    }

    public void RegisterPlayer(ulong clientId, PlayerController player)
    {
        _players[clientId] = player;
    }

    public void UnregisterPlayer(ulong clientId)
    {
        _players.Remove(clientId);
    }

    public PlayerController GetClosestTarget()
    {
        ulong playerId = NetworkManager.Singleton.LocalClientId;

        PlayerController currentTarget = null;
        float currentDistance = Mathf.Infinity;
        foreach (KeyValuePair<ulong, PlayerController> entry in _players)
        {
            if (entry.Key != playerId)
            {
                Vector3 positionDelta = entry.Value.transform.position - _player.transform.position;
                positionDelta.y = 0;
                float distance = positionDelta.magnitude;
                if (distance <= Mathf.Min(_targetDistance, currentDistance) &&
                    _player.IsFacing(entry.Value.transform.position))
                {
                    currentDistance = distance;
                    currentTarget = entry.Value;
                }
            }
        }
        return currentTarget;
    }

    /*
    public EnemyController GetClosestEnemy()
    {
        _enemiesInView.Clear();
        EnemyController[] enemies = _enemyParent.GetComponentsInChildren<EnemyController>();

        EnemyController currentEnemy = null;
        float currentDistance = Mathf.Infinity;

        foreach (EnemyController enemy in enemies)
        {
            enemy.IsTarget = false;

            Vector3 positionDelta = enemy.transform.position - _player.transform.position;
            positionDelta.y = 0;
            Vector3 playerForward = _player.transform.forward;
            playerForward.y = 0;
            float distance = positionDelta.magnitude;
            if (distance <= Mathf.Min(_targetDistance, currentDistance) &&
                Vector3.Angle(playerForward, positionDelta) <= _targetAngle)
            {
                distance = currentDistance;
                currentEnemy = enemy;
            }
        }
        return currentEnemy;
    }
    */
}
