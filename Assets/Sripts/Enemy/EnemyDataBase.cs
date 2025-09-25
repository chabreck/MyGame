using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Enemies/Database")]
public class EnemyDatabase : ScriptableObject
{
    public List<EnemyData> enemies = new List<EnemyData>();
    
    public EnemyData GetData(string id) => enemies.Find(e => e.id == id);
    
    public List<EnemyData> GetAllSortedBySpawnTime()
    {
        enemies.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        return enemies;
    }
}