using Cinemachine;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum GameState
{
    GAME_INIT,
    GAME_STARTED,
    GAME_FINISHED
}

public class GameController : MonoBehaviour
{
    [field: SerializeField]
    public CinemachineVirtualCamera cameraThirdPerson { get; private set; }

    [SerializeField]
    private CinemachineVirtualCamera _cameraAim;

    [SerializeField]
    private float _targetDistance = 200f;

    [SerializeField]
    private Vector2 _startingLocationMin = new Vector2(-40f, -40f);

    [SerializeField]
    private Vector2 _startingLocationMax = new Vector2(40f, 40f);


    public static GameController Instance;
    public System.Random Random { get; private set; }

    public PlayerController activePlayer { get; private set; }
    private Dictionary<int, PlayerController> _players;

    private void Awake()
    {
        Instance = this;
        Random = new System.Random(Guid.NewGuid().GetHashCode());
        _players = new Dictionary<int, PlayerController>();
    }

    public void CreatePlayer()
    {
        float rangeX = _startingLocationMax.x - _startingLocationMin.x;
        float rangeY = _startingLocationMax.y - _startingLocationMin.y;
        
        Vector3 location = new Vector3(
            (float)Random.NextDouble() * rangeX + _startingLocationMin.x,
            0, 
            (float)Random.NextDouble() * rangeY + _startingLocationMin.y);
        PhotonNetwork.Instantiate("Player", location, Quaternion.identity);
    }

    public void SetActivePlayer(PlayerController player)
    {
        activePlayer = player;
        UIManager.Instance.WeaponUIController.weapon = player == null ? null : player.Weapon;
        if (cameraThirdPerson != null)
        {
            if (player != null)
            {
                cameraThirdPerson.Follow = player.rotationTransform.transform;
                cameraThirdPerson.LookAt = player.rotationTransform.transform;
//                cameraThirdPerson.Follow = player.Weapon.transform;
//                cameraThirdPerson.LookAt = player.Weapon.transform;
            }
            else
            {
                cameraThirdPerson.Follow = null;
                cameraThirdPerson.LookAt = null;
            }
        }
        if (_cameraAim != null)
        {
            _cameraAim.Follow = player != null ? player.rotationTransform.transform : null;
            _cameraAim.LookAt = player != null ? player.rotationTransform.transform : null;
        }
    }

    public void RegisterPlayer(PlayerController player)
    {
        _players[player.playerId] = player;
        if (player.isMine)
        {
            SetActivePlayer(player);
        }
    }

    public void UnregisterPlayer(PlayerController player)
    {
        Debug.LogFormat("UnregisterPlayer {0} {1}", player.playerId, _players.Count);
        _players.Remove(player.playerId);
        if (player.isMine)
        {
            SetActivePlayer(null);
        }
    }

    public PlayerController GetClosestTarget()
    {
        int playerId = activePlayer.playerId;

        PlayerController currentTarget = null;
        float currentDistance = Mathf.Infinity;
        foreach (KeyValuePair<int, PlayerController> entry in _players)
        {
            if (entry.Key != playerId)
            {
                Vector3 positionDelta = entry.Value.transform.position - activePlayer.transform.position;
                positionDelta.y = 0;
                float distance = positionDelta.magnitude;
                if (distance <= Mathf.Min(_targetDistance, currentDistance) &&
                    activePlayer.IsFacing(entry.Value.transform.position))
                {
                    currentDistance = distance;
                    currentTarget = entry.Value;
                }
            }
        }
        return currentTarget;
    }

    public PlayerController GetPlayer(int id)
    {
        return _players.GetValueOrDefault(id, null);
    }
}
