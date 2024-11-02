using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using RpcTarget = Photon.Pun.RpcTarget;

public class BulletController : MonoBehaviour
{
    [HideInInspector]
    public Vector3 direction;

    [HideInInspector]
    public int sourcePlayerClientId;

    [HideInInspector]
    public int damage;

    private MeshRenderer _meshRenderer;
    private PhotonView _photonView;
    private float _speed = 50f;
    private float _duration = 3;
    private float _timeStart;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        _timeStart = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_photonView.IsMine)
        {
            return;
        }

        if (Time.time - _timeStart >= _duration)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            transform.position += direction * _speed * Time.deltaTime;
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
