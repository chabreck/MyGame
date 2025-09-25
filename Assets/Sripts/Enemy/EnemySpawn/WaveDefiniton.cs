using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Waves/WaveDefinition")]
public class WaveDefinition : ScriptableObject
{
    public float duration = 20f;
    public float difficultyMultiplier = 1f;
    public WaveEntry[] entries;

    [Serializable]
    public class WaveEntry
    {
        public GameObject enemyPrefab;
        public int count = 8;
        public float spawnInterval = 0.25f;
        public SpawnPattern pattern = SpawnPattern.RandomBurst;
        public float radius = 6f;
        public float spread = 1f;
    }

    public enum SpawnPattern
    {
        SurroundSquare,
        FlankSides,
        Ring,
        FromTop,
        FromBottom,
        RandomBurst
    }
}