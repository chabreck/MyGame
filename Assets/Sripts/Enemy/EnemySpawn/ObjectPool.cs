using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private Queue<PoolableObject> _pool = new Queue<PoolableObject>();
    private HashSet<PoolableObject> _active = new HashSet<PoolableObject>();
    private GameObject _prefab;
    private Transform _parent;
    private int _created = 0;

    public int Available => _pool.Count;
    public int Created => _created;

    public ObjectPool(GameObject prefab, Transform parent, int initialSize = 200)
    {
        _prefab = prefab;
        _parent = parent;
        for (int i = 0; i < initialSize; i++)
        {
            var obj = Instantiate();
            obj.SetPool(this);
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    private PoolableObject Instantiate()
    {
        var go = GameObject.Instantiate(_prefab, _parent);
        var obj = go.GetComponent<PoolableObject>();
        if (obj == null) obj = go.AddComponent<PoolableObject>();
        _created++;
        return obj;
    }

    public PoolableObject Get()
    {
        PoolableObject obj;
        if (_pool.Count == 0) obj = Instantiate();
        else obj = _pool.Dequeue();
        _active.Add(obj);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void ReturnObject(PoolableObject obj)
    {
        if (obj == null) return;
        obj.gameObject.SetActive(false);
        _active.Remove(obj);
        _pool.Enqueue(obj);
    }

    public PoolableObject[] GetActiveObjectsSnapshot()
    {
        var arr = new PoolableObject[_active.Count];
        _active.CopyTo(arr);
        return arr;
    }
}