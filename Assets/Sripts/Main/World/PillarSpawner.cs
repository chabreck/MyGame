using UnityEngine;

public class PillarSpawner : MonoBehaviour
{
    public GameObject pillarPrefab;
    public Transform worldRoot;
    public float checkInterval = 15f;
    [Range(0f,1f)] public float spawnChance = 0.02f;
    public float minSpawnTime = 300f;

    private float lastCheck;

    private void Start()
    {
        if (worldRoot == null) worldRoot = new GameObject("Pillars").transform;
        lastCheck = Time.time;
    }

    private void Update()
    {
        if (Time.timeSinceLevelLoad < minSpawnTime) return;
        if (Time.time - lastCheck < checkInterval) return;
        lastCheck = Time.time;
        if (pillarPrefab == null) return;
        if (Random.value > spawnChance) return;
        Vector3 spawnPos = ComputeSpawnPositionNearPlayer();
        Instantiate(pillarPrefab, spawnPos, Quaternion.identity, worldRoot);
    }

    private Vector3 ComputeSpawnPositionNearPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return Vector3.zero;
        float r = Random.Range(6f, 12f);
        float a = Random.Range(0f, Mathf.PI*2f);
        return p.transform.position + new Vector3(Mathf.Cos(a)*r, Mathf.Sin(a)*r, 0f);
    }
}