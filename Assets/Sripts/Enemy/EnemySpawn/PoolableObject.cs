using UnityEngine;

public class PoolableObject : MonoBehaviour
{
    private ObjectPool _pool;

    public void SetPool(ObjectPool pool)
    {
        _pool = pool;
    }

    public void ReturnToPool()
    {
        if (_pool != null) _pool.ReturnObject(this);
        else Destroy(gameObject);
    }
}