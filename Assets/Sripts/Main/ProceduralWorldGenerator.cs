using UnityEngine;
using System.Collections.Generic;

public class ProceduralWorldGenerator : MonoBehaviour
{
    [Header("Tiles")]
    public GameObject dirtTilePrefab;
    public List<GameObject> grassTilePrefabs; // overlay prefabs (may be transparent)
    public GameObject[] treePrefabs;
    public GameObject[] bushPrefabs; // добавлены кусты
    public GameObject[] rockPrefabs; // добавлены камни

    [Header("Generation Settings")]
    public int chunkSize = 4;
    public int renderDistance = 4;
    [Range(0, 1)] public float grassProbability = 0.8f;
    [Range(0, 1)] public float treeProbability = 0.01f;
    [Range(0, 1)] public float bushProbability = 0.05f; // шанс спавна кустов
    [Range(0, 1)] public float rockProbability = 0.03f; // шанс спавна камней

    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();
    private Transform player;
    private Vector2Int lastPlayerChunkPos;
    private Transform worldParent;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        worldParent = new GameObject("World").transform;
        lastPlayerChunkPos = GetCurrentChunkPos();
        UpdateTerrain();
    }

    void Update()
    {
        Vector2Int currentChunkPos = GetCurrentChunkPos();
        if (currentChunkPos != lastPlayerChunkPos)
        {
            UpdateTerrain();
            lastPlayerChunkPos = currentChunkPos;
        }
    }

    private Vector2Int GetCurrentChunkPos()
    {
        return new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.y / chunkSize)
        );
    }

    private void UpdateTerrain()
    {
        Vector2Int playerChunk = GetCurrentChunkPos();
        
        for (int x = playerChunk.x - renderDistance; x <= playerChunk.x + renderDistance; x++)
        {
            for (int y = playerChunk.y - renderDistance; y <= playerChunk.y + renderDistance; y++)
            {
                Vector2Int chunkPos = new Vector2Int(x, y);
                if (!loadedChunks.ContainsKey(chunkPos))
                {
                    GenerateChunk(chunkPos);
                }
            }
        }
        UnloadFarChunks(playerChunk);
    }

    private void GenerateChunk(Vector2Int chunkPos)
    {
        int startX = chunkPos.x * chunkSize;
        int startY = chunkPos.y * chunkSize;

        GameObject chunkParent = new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
        chunkParent.transform.SetParent(worldParent);
        chunkParent.transform.position = new Vector3(startX, startY, 0f);

        for (int x = startX; x < startX + chunkSize; x++)
        {
            for (int y = startY; y < startY + chunkSize; y++)
            {
                Vector3 spawnPos = new Vector3(x + 0.5f, y + 0.5f, 0f);

                // Сначала проверяем, нужно ли создавать землю
                bool shouldCreateDirt = true;
                
                // Проверяем, будет ли трава
                if (grassTilePrefabs != null && grassTilePrefabs.Count > 0 && Random.value < grassProbability)
                {
                    shouldCreateDirt = false; // Не создаем землю, если есть трава
                    GameObject grassPrefab = grassTilePrefabs[Random.Range(0, grassTilePrefabs.Count)];
                    if (grassPrefab != null)
                    {
                        Instantiate(grassPrefab, spawnPos, Quaternion.identity, chunkParent.transform);
                    }
                }

                // Создаем землю только если нужно
                if (shouldCreateDirt && dirtTilePrefab != null)
                {
                    Instantiate(dirtTilePrefab, spawnPos, Quaternion.identity, chunkParent.transform);
                }
                else if (shouldCreateDirt)
                {
                    Debug.LogWarning("dirtTilePrefab is not assigned!");
                }

                // Деревья (с проверкой расстояния от краев чанка)
                if (treePrefabs != null && treePrefabs.Length > 0 && Random.value < treeProbability)
                {
                    // Деревья не у краев чанка
                    if (x > startX && x < startX + chunkSize - 1 && 
                        y > startY && y < startY + chunkSize - 1)
                    {
                        GameObject treePrefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                        if (treePrefab != null)
                        {
                            Instantiate(treePrefab, spawnPos, Quaternion.identity, chunkParent.transform);
                        }
                    }
                }

                // Кусты
                if (bushPrefabs != null && bushPrefabs.Length > 0 && Random.value < bushProbability)
                {
                    GameObject bushPrefab = bushPrefabs[Random.Range(0, bushPrefabs.Length)];
                    if (bushPrefab != null)
                    {
                        Instantiate(bushPrefab, spawnPos, Quaternion.identity, chunkParent.transform);
                    }
                }

                // Камни
                if (rockPrefabs != null && rockPrefabs.Length > 0 && Random.value < rockProbability)
                {
                    GameObject rockPrefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
                    if (rockPrefab != null)
                    {
                        Instantiate(rockPrefab, spawnPos, Quaternion.identity, chunkParent.transform);
                    }
                }
            }
        }
        loadedChunks.Add(chunkPos, chunkParent);
    }

    private void UnloadFarChunks(Vector2Int playerChunk)
    {
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var kv in loadedChunks)
        {
            Vector2Int pos = kv.Key;
            if (Mathf.Abs(pos.x - playerChunk.x) > renderDistance || Mathf.Abs(pos.y - playerChunk.y) > renderDistance)
            {
                chunksToRemove.Add(pos);
            }
        }

        foreach (var cp in chunksToRemove)
        {
            GameObject parent = loadedChunks[cp];
            if (parent != null) Destroy(parent);
            loadedChunks.Remove(cp);
        }
    }
}