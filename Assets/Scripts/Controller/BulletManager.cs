using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BulletManager : NetworkBehaviour
{
    [SerializeField]
    private BulletController _bulletPrefab;

    public static BulletManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    [Rpc(SendTo.Server)]
    public void SpawnBulletRpc(
        ulong clientId,
        Vector3 position,
        Vector3 direction,
        int damage)
    {
        BulletController bullet = Instantiate(_bulletPrefab, transform);
        bullet.transform.position = position;
        bullet.direction = direction;
        bullet.sourcePlayerClientId = clientId;
        bullet.damage = damage;
        bullet.GetComponent<NetworkObject>().Spawn(true);
    }
}
