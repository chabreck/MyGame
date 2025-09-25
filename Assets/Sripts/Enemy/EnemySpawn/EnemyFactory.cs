using System.Collections.Generic;
using UnityEngine;

public class EnemyFactory : MonoBehaviour
{
    [SerializeField] private EnemyDatabase _database;
    [SerializeField] private Transform _enemyContainer;
    [SerializeField] private int defaultInitialPoolSize = 200;

    private Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();
    private Dictionary<GameObject, string> _prefabToId = new Dictionary<GameObject, string>();

    void Start()
    {
        if (_database != null)
        {
            foreach (var data in _database.enemies)
            {
                if (data == null) continue;
                CreatePool(data.id, data.prefab, Mathf.Max(1, defaultInitialPoolSize));
            }
        }
    }

    private void CreatePool(string id, GameObject prefab, int size)
    {
        if (string.IsNullOrEmpty(id) || prefab == null) return;
        if (_pools.ContainsKey(id)) return;
        var pool = new ObjectPool(prefab, _enemyContainer, size);
        _pools.Add(id, pool);
        if (!_prefabToId.ContainsKey(prefab)) _prefabToId.Add(prefab, id);
    }

    public GameObject SpawnEnemy(string enemyId, Vector2 position)
    {
        if (!_pools.ContainsKey(enemyId)) return null;
        var pool = _pools[enemyId];
        var obj = pool.Get();
        if (obj == null) return null;
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.gameObject.SetActive(true);
        var enemy = obj.GetComponent<EnemyStats>();
        if (enemy != null && _database != null)
        {
            enemy.Init(_database.GetData(enemyId));
        }
        return obj.gameObject;
    }

    public GameObject SpawnEnemy(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return null;
        if (_prefabToId.ContainsKey(prefab))
        {
            string id = _prefabToId[prefab];
            return SpawnEnemy(id, position);
        }
        string genId = prefab.name + "_gen";
        if (!_pools.ContainsKey(genId))
        {
            CreatePool(genId, prefab, Mathf.Max(1, defaultInitialPoolSize));
        }
        var pool = _pools[genId];
        var obj = pool.Get();
        if (obj == null) return null;
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.gameObject.SetActive(true);
        return obj.gameObject;
    }

    public void ReturnAllOffscreen(Camera cam, Transform player)
    {
        if (cam == null || player == null) return;
        foreach (var kv in _pools)
        {
            var pool = kv.Value;
            var actives = pool.GetActiveObjectsSnapshot();
            foreach (var po in actives)
            {
                if (po == null || po.gameObject == null) continue;
                Vector3 worldPos = po.transform.position;
                Vector3 vp = cam.WorldToViewportPoint(worldPos);
                if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
                {
                    po.ReturnToPool();
                }
            }
        }
    }
}
