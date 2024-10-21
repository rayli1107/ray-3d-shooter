using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BulletController : NetworkBehaviour
{
    [HideInInspector]
    public Vector3 direction;

    [HideInInspector]
    public ulong sourcePlayerClientId;

    [HideInInspector]
    public int damage;

    private float _speed = 50f;
    private float _duration = 3;
    private float _timeStart;

    private void OnEnable()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        _timeStart = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (Time.time - _timeStart >= _duration)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
        else
        {
            transform.position += direction * _speed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner)
        {
            return;
        }

        PlayerController target = other.GetComponent<PlayerController>();
        if (target != null)
        {
            NetworkObject targetNetworkObject = target.GetComponent<NetworkObject>();
            if (sourcePlayerClientId != targetNetworkObject.OwnerClientId)
            {
                target.OnBulletHitRpc(damage);
            }
        }
        /*
                if (sourcePlayer)
                {
                    EnemyController controller = other.GetComponent<EnemyController>();
                    if (controller != null)
                    {
                        controller.OnBulletHit(damage);
                    }
                }
        */
        GetComponent<NetworkObject>().Despawn(true);
    }
}
