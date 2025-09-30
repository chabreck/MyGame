using UnityEngine;

[CreateAssetMenu(menuName = "Weather/EarthquakeCondition")]
public class EarthquakeWeatherData : WeatherBase
{
    public float[] speedReduction = new float[3] { 0.10f, 0.20f, 0.30f };
    public float[] crackIntervalMin = new float[3] { 6f, 4f, 3f };
    public float[] crackIntervalMax = new float[3] { 8f, 6f, 4f };
    public float[] customDurations = new float[3] { 20f, 30f, 40f };
    public Sprite crackSprite;
    public bool crackCreatesCollider = true;
    public float crackLifeTime = 8f;
    public float crackSpawnPadding = 1f;
    public string crackSortingLayer = "Default";
    public int crackOrderInLayer = 0;
    public float cameraShakeIntensity = 0.25f;
    public float cameraShakeDurationPerWave = 0.35f;
    public int dustBurstCount = 40;
    public float dustRadius = 1.2f;
    public float dustStartSpeedMin = 0.3f;
    public float dustStartSpeedMax = 1.2f;
    public float dustStartSizeMin = 0.15f;
    public float dustStartSizeMax = 0.45f;
    public Gradient dustColorGradient;
}