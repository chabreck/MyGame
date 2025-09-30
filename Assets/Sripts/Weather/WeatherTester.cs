using UnityEngine;

public class WeatherTester : MonoBehaviour
{
    public KeyCode spawnKey = KeyCode.F5;
    public KeyCode stopKey = KeyCode.F6;

    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            if (WeatherManager.Instance != null) WeatherManager.Instance.SpawnRandomWeather();
        }
        if (Input.GetKeyDown(stopKey))
        {
            if (WeatherManager.Instance != null) WeatherManager.Instance.StopWeather();
        }
    }
}