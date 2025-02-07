using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using RpcTarget = Photon.Pun.RpcTarget;

[Serializable]
public class BulletData
{
    public int ownerId;
    public Vector3 velocity;
    public float duration;
    public int damage;
}

public class BulletController : MonoBehaviour
{
    private MeshRenderer _meshRenderer;
    private PhotonView _photonView;
    private BulletData _bulletData;
    private float _timeStart;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        _bulletData = JsonUtility.FromJson<BulletData>((string)_photonView.InstantiationData[0]);
        Debug.LogFormat(
            "_bulletData playerId {0} velocity {1} damage {2} duration {3}",
            _bulletData.ownerId,
            _bulletData.velocity,
            _bulletData.damage,
            _bulletData.duration);
        _timeStart = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += _bulletData.velocity * Time.deltaTime;

        if (_photonView.IsMine && Time.time - _timeStart >= _bulletData.duration)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController target = other.GetComponent<PlayerController>();
        if (target != null && _photonView.ControllerActorNr != target.playerId)
        {
            _meshRenderer.enabled = false;

            if (target.isMine)
            {
                target.OnBulletHit(1);
                _photonView.RPC("BulletDestroyRPC", RpcTarget.OthersBuffered);
            }
        }
    }

    [PunRPC]
    public void BulletDestroyRPC()
    {
        if (_photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
