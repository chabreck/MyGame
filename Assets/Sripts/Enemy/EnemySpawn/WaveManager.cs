using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Serializable]
    public class WaveSchedule
    {
        public WaveDefinition wave;
        public float startTimeSeconds;
    }

    [SerializeField] private WaveSchedule[] waveSchedules;
    [SerializeField] private MonoBehaviour enemySpawner;
    [SerializeField] private float safeSpawnDistance = 0.1f;
    [SerializeField] private bool pauseRegularSpawnerDuringWave = false;
    [SerializeField] private float offscreenMargin = 1.5f;

    private Transform player;
    private Camera mainCamera;
    private Coroutine schedulerCoroutine;
    private bool isWaveActive = false;

    private void Start()
    {
        var hp = FindObjectOfType<HeroCombat>();
        if (hp != null) player = hp.transform;
        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }

        if (player == null)
        {
            Debug.LogError("WaveManager: Player not found!");
            return;
        }

        mainCamera = Camera.main;
        schedulerCoroutine = StartCoroutine(Scheduler());
    }

    public void StartWaves()
    {
        if (schedulerCoroutine == null) schedulerCoroutine = StartCoroutine(Scheduler());
    }

    public void StopWaves()
    {
        if (schedulerCoroutine != null) { StopCoroutine(schedulerCoroutine); schedulerCoroutine = null; }
    }

    private IEnumerator Scheduler()
    {
        if (waveSchedules == null || waveSchedules.Length == 0) yield break;
        var list = waveSchedules.Where(ws => ws.wave != null).OrderBy(ws => ws.startTimeSeconds).ToArray();
        foreach (var ws in list)
        {
            float target = ws.startTimeSeconds;
            while (Time.timeSinceLevelLoad < target) yield return null;
            StartCoroutine(RunWave(ws.wave));
        }
    }

    private IEnumerator RunWave(WaveDefinition wave)
    {
        isWaveActive = true;
        EnemySpawner regularSpawner = pauseRegularSpawnerDuringWave ? FindObjectOfType<EnemySpawner>() : null;
        if (regularSpawner != null) regularSpawner.enabled = false;

        float endAt = Time.time + Mathf.Max(0.01f, wave.duration);
        foreach (var entry in wave.entries)
        {
            if (entry == null || entry.enemyPrefab == null) continue;
            StartCoroutine(RunWaveEntry(entry, wave.difficultyMultiplier, endAt));
        }

        while (Time.time < endAt) yield return null;

        var factory = FindObjectOfType<EnemyFactory>();
        if (factory != null) factory.ReturnAllOffscreen(mainCamera, player);
        isWaveActive = false;
        if (regularSpawner != null) regularSpawner.enabled = true;
    }

    private IEnumerator RunWaveEntry(WaveDefinition.WaveEntry entry, float difficultyMultiplier, float endAt)
    {
        if (entry == null || entry.enemyPrefab == null) yield break;

        int totalToSpawn = Mathf.Max(1, Mathf.RoundToInt(entry.count * Mathf.Max(1f, difficultyMultiplier)));
        if (entry.spawnInterval <= 0f)
        {
            List<Vector2> spawnPositions = new List<Vector2>();
            for (int i = 0; i < totalToSpawn; i++)
            {
                Vector2 pos2 = ComputeSpawnPosition(entry.pattern, i, totalToSpawn, entry.radius, entry.spread);
                spawnPositions.Add(EnsureOffscreen(pos2, entry.radius));
            }
            int spawned = 0;
            for (int i = 0; i < spawnPositions.Count; i++)
            {
                if (Time.time >= endAt) break;
                Vector3 spawn3 = new Vector3(spawnPositions[i].x, spawnPositions[i].y, 0f);
                bool ok = SpawnEnemy(entry.enemyPrefab, spawn3, difficultyMultiplier);
                if (ok) spawned++;
            }
            yield break;
        }

        for (int i = 0; i < totalToSpawn; i++)
        {
            if (Time.time >= endAt) break;
            Vector2 pos2 = ComputeSpawnPosition(entry.pattern, i, totalToSpawn, entry.radius, entry.spread);
            pos2 = EnsureOffscreen(pos2, entry.radius);
            Vector3 spawn3 = new Vector3(pos2.x, pos2.y, 0f);
            SpawnEnemy(entry.enemyPrefab, spawn3, difficultyMultiplier);
            float wait = Mathf.Max(0.01f, entry.spawnInterval);
            float remain = Mathf.Max(0f, endAt - Time.time);
            if (remain <= 0f) break;
            yield return new WaitForSeconds(Mathf.Min(wait, remain));
        }
    }

    private Vector2 ComputeSpawnPosition(WaveDefinition.SpawnPattern pattern, int index, int total, float radius, float spread)
    {
        Vector2 playerPos = player != null ? (Vector2)player.position : Vector2.zero;
        Vector2 spawnPos = Vector2.zero;

        switch (pattern)
        {
            case WaveDefinition.SpawnPattern.SurroundSquare:
                int side = index % 4;
                int step = index / 4;
                int perSide = Mathf.Max(1, Mathf.CeilToInt((float)total / 4f));
                float along = ((float)step / Mathf.Max(1, perSide - 1)) * (radius * 2f) - radius;
                float x = 0f, y = 0f;
                switch (side)
                {
                    case 0: x = -radius; y = along; break;
                    case 1: x = radius; y = along; break;
                    case 2: x = along; y = -radius; break;
                    default: x = along; y = radius; break;
                }
                x += UnityEngine.Random.Range(-spread, spread);
                y += UnityEngine.Random.Range(-spread, spread);
                spawnPos = playerPos + new Vector2(x, y);
                break;

            case WaveDefinition.SpawnPattern.FlankSides:
                int half = Mathf.Max(1, total / 2);
                float offsetY = ((index % half) - (half - 1) * 0.5f) * (spread + 1f);
                bool left = index < half;
                float flankX = (left ? -1 : 1) * radius + UnityEngine.Random.Range(-spread, spread);
                spawnPos = playerPos + new Vector2(flankX, offsetY);
                break;

            case WaveDefinition.SpawnPattern.Ring:
                float angle = ((float)index / Mathf.Max(1, total)) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.1f, 0.1f);
                float r = radius + UnityEngine.Random.Range(-spread, spread);
                float ringX = Mathf.Cos(angle) * r;
                float ringY = Mathf.Sin(angle) * r;
                spawnPos = playerPos + new Vector2(ringX, ringY);
                break;

            case WaveDefinition.SpawnPattern.FromTop:
                Vector2 topEdge = GetScreenEdgePosition2D(0);
                float topX = UnityEngine.Random.Range(-radius, radius);
                spawnPos = new Vector2(playerPos.x + topX, topEdge.y);
                break;

            case WaveDefinition.SpawnPattern.FromBottom:
                Vector2 bottomEdge = GetScreenEdgePosition2D(1);
                float bottomX = UnityEngine.Random.Range(-radius, radius);
                spawnPos = new Vector2(playerPos.x + bottomX, bottomEdge.y);
                break;

            case WaveDefinition.SpawnPattern.RandomBurst:
            default:
                spawnPos = GetRandomOffscreenPosition(radius);
                break;
        }

        return EnsureSafeDistanceFromPlayer(spawnPos);
    }

    private Vector2 GetScreenEdgePosition2D(int edgeIndex)
    {
        Vector2 playerPos = player != null ? (Vector2)player.position : Vector2.zero;
        if (mainCamera == null)
        {
            float offset = 10f;
            switch (edgeIndex)
            {
                case 0: return playerPos + Vector2.up * offset;
                case 1: return playerPos + Vector2.down * offset;
                case 2: return playerPos + Vector2.left * offset;
                case 3: return playerPos + Vector2.right * offset;
                default: return playerPos;
            }
        }

        Vector3 world;
        switch (edgeIndex)
        {
            case 0:
                world = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, mainCamera.nearClipPlane));
                break;
            case 1:
                world = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, mainCamera.nearClipPlane));
                break;
            case 2:
                world = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, mainCamera.nearClipPlane));
                break;
            default:
                world = mainCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, mainCamera.nearClipPlane));
                break;
        }
        return (Vector2)world;
    }

    private Vector2 GetRandomOffscreenPosition(float radius)
    {
        if (mainCamera == null)
        {
            Vector2 playerPos = player != null ? (Vector2)player.position : Vector2.zero;
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float r = UnityEngine.Random.Range(radius * 0.5f, radius);
            return playerPos + new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        Vector2 playerPos2 = player != null ? (Vector2)player.position : Vector2.zero;
        float halfDiagonal = GetCameraHalfDiagonal();
        float dist = halfDiagonal + offscreenMargin + UnityEngine.Random.Range(0f, radius);
        float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector2 pos2 = playerPos2 + new Vector2(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist);
        return pos2;
    }

    private Vector2 EnsureOffscreen(Vector2 suggested, float radius)
    {
        if (mainCamera == null) return EnsureSafeDistanceFromPlayer(suggested);

        Vector3 vpCheck = mainCamera.WorldToViewportPoint(new Vector3(suggested.x, suggested.y, 0f));
        if (vpCheck.x < 0f || vpCheck.x > 1f || vpCheck.y < 0f || vpCheck.y > 1f) return EnsureSafeDistanceFromPlayer(suggested);

        float halfDiagonal = GetCameraHalfDiagonal();
        Vector2 playerPos2 = player != null ? (Vector2)player.position : Vector2.zero;
        Vector2 dir = suggested - playerPos2;
        if (dir.sqrMagnitude < 0.001f) dir = UnityEngine.Random.insideUnitCircle.normalized;
        dir = dir.normalized;
        float dist = halfDiagonal + offscreenMargin + UnityEngine.Random.Range(0f, radius);
        Vector2 result = playerPos2 + dir * dist;
        return EnsureSafeDistanceFromPlayer(result);
    }

    private float GetCameraHalfDiagonal()
    {
        if (mainCamera == null) return 10f;
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;
        return Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
    }

    private Vector2 EnsureSafeDistanceFromPlayer(Vector2 spawnPos)
    {
        if (player == null) return spawnPos;

        Vector2 playerPos2 = (Vector2)player.position;
        Vector2 direction = spawnPos - playerPos2;
        float distance = direction.magnitude;

        if (distance < safeSpawnDistance)
        {
            spawnPos = playerPos2 + direction.normalized * safeSpawnDistance;
        }

        return spawnPos;
    }

    private bool SpawnEnemy(GameObject prefab, Vector3 pos, float difficultyMultiplier)
    {
        if (prefab == null) return false;

        var factory = FindObjectOfType<EnemyFactory>();
        if (factory != null)
        {
            var go = factory.SpawnEnemy(prefab, pos);
            if (go != null) return true;
        }

        if (enemySpawner != null)
        {
            var m = enemySpawner.GetType().GetMethod("SpawnEnemy", BindingFlags.Public | BindingFlags.Instance);
            if (m != null)
            {
                try
                {
                    var parameters = m.GetParameters();
                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(GameObject))
                    {
                        m.Invoke(enemySpawner, new object[] { prefab, pos });
                        return true;
                    }
                    else if (parameters.Length == 3)
                    {
                        m.Invoke(enemySpawner, new object[] { prefab, pos, difficultyMultiplier });
                        return true;
                    }
                    else
                    {
                        m.Invoke(enemySpawner, new object[] { prefab, pos });
                        return true;
                    }
                }
                catch { }
            }
        }

        UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
        return true;
    }
}
