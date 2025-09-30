using UnityEngine;

public class BreakableSpawner : MonoBehaviour
{
    public GameObject breakablePrefab;
    public Transform worldRoot;
    public float checkInterval = 15f;
    [Range(0f,1f)] public float spawnChance = 0.02f;
    public float minSpawnTime = 0f;
    public float minDistanceFromPlayer = 6f;
    public float maxDistanceFromPlayer = 12f;
    public int maxPerCheck = 1;

    private float lastCheck;

    void Start()
    {
        if (worldRoot == null) worldRoot = new GameObject("Breakables").transform;
        lastCheck = Time.time;
    }

    void Update()
    {
        if (Time.timeSinceLevelLoad < minSpawnTime) return;
        if (Time.time - lastCheck < checkInterval) return;
        lastCheck = Time.time;
        if (breakablePrefab == null) return;
        for (int i = 0; i < maxPerCheck; i++)
        {
            if (Random.value > spawnChance) continue;
            Vector3 spawnPos = ComputeSpawnPositionNearPlayer();
            Instantiate(breakablePrefab, spawnPos, Quaternion.identity, worldRoot);
        }
    }

    private Vector3 ComputeSpawnPositionNearPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return Vector3.zero;
        float r = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
        float a = Random.Range(0f, Mathf.PI * 2f);
        return p.transform.position + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
    }
}