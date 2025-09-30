using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Weather/WeatherBase")]
public class WeatherBase : ScriptableObject
{
    public LocalizedString conditionNameLocalized;
    public string conditionNameFallback;
    [Min(1)] public int minDifficulty = 1;
    [Min(1)] public int maxDifficulty = 3;
    public GameObject visualPrefab;
    public Sprite icon;
    public float[] durations = new float[3] { 20f, 30f, 40f };
}