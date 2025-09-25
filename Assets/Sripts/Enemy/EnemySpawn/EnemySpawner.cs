using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyDatabase database;
    [SerializeField] private EnemyFactory factory;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;

    private List<EnemyData> allEnemies;
    private List<EnemyData> unlocked = new List<EnemyData>();
    private float timer = 0f;

    private void Start()
    {
        allEnemies = database.GetAllSortedBySpawnTime();
        if (player == null) Debug.LogError("EnemySpawner: Player not assigned!");
        if (mainCamera == null) Debug.LogError("EnemySpawner: MainCamera not assigned!");
    }

    private void Update()
    {
        float t = Time.timeSinceLevelLoad;

        foreach (var data in allEnemies)
        {
            if (t >= data.spawnTime && !unlocked.Contains(data))
            {
                unlocked.Add(data);
                Debug.Log($"[{t:F1}s] Enemy unlocked: {data.id}");
            }
        }

        timer += Time.deltaTime;
        if (timer >= spawnInterval && unlocked.Count > 0)
        {
            var choice = unlocked[Random.Range(0, unlocked.Count)];
            Vector2 pos = GetSpawnPosition();
            factory.SpawnEnemy(choice.id, pos);
            timer = 0f;
        }
    }

    private Vector2 GetSpawnPosition()
    {
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;
        float halfDiagonal = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
        float spawnRadius = halfDiagonal + 1.0f;
        Vector2 dir = Random.insideUnitCircle.normalized;
        return (Vector2)player.position + dir * spawnRadius;
    }
}