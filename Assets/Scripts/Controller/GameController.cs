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

public class GameController : MonoBehaviourPunCallbacks
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

    [SerializeField]
    private int _maxCoins = 10;

    [SerializeField]
    private int _coinValue = 10;

    public static GameController Instance;
    public System.Random Random { get; private set; }

    public PlayerController activePlayer { get; private set; }
    private Dictionary<int, PlayerController> _players;
    private int _coins;

    private void Awake()
    {
        Instance = this;
        Random = new System.Random(Guid.NewGuid().GetHashCode());
        _players = new Dictionary<int, PlayerController>();
        _coins = 0;
    }

    private Vector3 getRandomLocation()
    {
        float rangeX = _startingLocationMax.x - _startingLocationMin.x;
        float rangeY = _startingLocationMax.y - _startingLocationMin.y;

        return new Vector3(
            (float)Random.NextDouble() * rangeX + _startingLocationMin.x,
            0,
            (float)Random.NextDouble() * rangeY + _startingLocationMin.y);
    }

    public void CreatePlayer()
    {
        PhotonNetwork.Instantiate("Player", getRandomLocation(), Quaternion.identity);
    }

    public void SetActivePlayer(PlayerController player)
    {
        activePlayer = player;
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

        UIManager.Instance.RegisterActivePlayer(player);
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
        _players.Remove(player.playerId);
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

    public void ManualLeaveRoom()
    {
        activePlayer.SavePlayerTransform();
        PhotonNetworkManager.Instance.LeaveRoom();
    }

    public void OnActivePlayerDeath()
    {
        DestroyActiveGamePlayer();
        UIManager.Instance.ShowMessageBox(
            "You Lose!", (_) => PhotonNetworkManager.Instance.LeaveRoom(), true);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("GameController.OnJoinedRoom()");
        CreatePlayer();
        InvokeRepeating(nameof(spawnCoin), 1, 5);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        CancelInvoke(nameof(spawnCoin));
    }

    private void spawnCoin()
    {
        if (PhotonNetworkManager.Instance.IsRoomMaster() && _coins < _maxCoins)
        {
            Debug.Log("spawnCoin");

            object[] objectData = new object[1];
            objectData[0] = _coinValue;
            PhotonNetwork.InstantiateRoomObject("Coin", getRandomLocation(), Quaternion.identity, 0, objectData);
        }
    }

    public void OnCoinCreate()
    {
        ++_coins;
    }

    public void OnCoinDestroy()
    {
        --_coins;
    }

    public void DestroyActiveGamePlayer()
    {
        PhotonView playerPhotonView = activePlayer.photonView;
        SetActivePlayer(null);
        PhotonNetwork.Destroy(playerPhotonView);
    }
}
