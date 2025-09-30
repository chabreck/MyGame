using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [SerializeField] private WeatherBase[] conditions;
    [SerializeField] private Transform weatherContainer;
    [SerializeField] private WeatherUIController uiController;
    [SerializeField] private float autoSpawnIntervalSeconds = 180f;
    [SerializeField] private bool autoSpawnEnabled = false;

    private GameObject activeVisual;
    private WeatherBase activeCondition;
    private IWeatherEffect activeEffect;
    private Coroutine stopCoroutine;
    private Coroutine autoSpawnCoroutine;
    private int activeDifficulty = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(this);
    }

    private void Start()
    {
        if (uiController == null) uiController = FindObjectOfType<WeatherUIController>();
        if (autoSpawnEnabled) autoSpawnCoroutine = StartCoroutine(AutoSpawnLoop());
    }

    private IEnumerator AutoSpawnLoop()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, autoSpawnIntervalSeconds));
        while (true)
        {
            SpawnRandomWeather();
            yield return new WaitForSeconds(Mathf.Max(0.1f, autoSpawnIntervalSeconds));
        }
    }

    public void SpawnRandomWeather()
    {
        if (conditions == null || conditions.Length == 0)
        {
            Debug.LogWarning("WeatherManager: no conditions assigned");
            return;
        }
        int idx = UnityEngine.Random.Range(0, conditions.Length);
        StartWeather(conditions[idx], 0f);
    }

    public void StartWeatherByIndex(int index, float overrideDuration = 0f)
    {
        if (conditions == null || index < 0 || index >= conditions.Length)
        {
            Debug.LogWarning("WeatherManager: StartWeatherByIndex invalid index");
            return;
        }
        StartWeather(conditions[index], overrideDuration);
    }

    public void StartWeather(WeatherBase cond, float overrideDuration = 0f)
    {
        StopWeather();
        if (cond == null)
        {
            Debug.LogWarning("WeatherManager: StartWeather called with null condition");
            return;
        }

        activeCondition = cond;
        activeDifficulty = UnityEngine.Random.Range(Mathf.Max(1, cond.minDifficulty), Mathf.Max(cond.minDifficulty, cond.maxDifficulty) + 1);
        float chosenDuration = overrideDuration > 0f ? overrideDuration : GetDurationForDifficulty(cond, activeDifficulty);

        Debug.Log($"WeatherManager: Starting weather '{(cond.conditionNameFallback ?? cond.name)}' difficulty={activeDifficulty} duration={chosenDuration}");

        if (cond.visualPrefab != null)
        {
            activeVisual = Instantiate(cond.visualPrefab, weatherContainer);
            activeVisual.transform.localPosition = Vector3.zero;
            activeEffect = activeVisual.GetComponent<IWeatherEffect>();
            if (activeEffect == null)
            {
                var comp = activeVisual.GetComponents<MonoBehaviour>().FirstOrDefault(c => c is IWeatherEffect) as IWeatherEffect;
                if (comp != null) activeEffect = comp;
            }
            if (activeEffect != null)
            {
                Debug.Log("WeatherManager: Found IWeatherEffect on visualPrefab, calling Begin");
                activeEffect.Begin(cond, chosenDuration, activeDifficulty);
            }
            else
            {
                Debug.LogWarning("WeatherManager: visualPrefab present but no IWeatherEffect found on it");
            }
        }
        else
        {
            GameObject go = new GameObject($"{cond.conditionNameFallback ?? cond.name}_AutoEffect");
            if (weatherContainer != null) go.transform.SetParent(weatherContainer, false);
            go.transform.localPosition = Vector3.zero;

            Type effType = FindEffectTypeForData(cond.GetType());
            if (effType != null && typeof(MonoBehaviour).IsAssignableFrom(effType))
            {
                var mb = go.AddComponent(effType) as MonoBehaviour;
                activeEffect = mb as IWeatherEffect;
                if (activeEffect != null)
                {
                    activeVisual = go;
                    Debug.Log($"WeatherManager: Added effect component '{effType.Name}' by reflection, calling Begin");
                    activeEffect.Begin(cond, chosenDuration, activeDifficulty);
                }
                else
                {
                    Debug.LogWarning($"WeatherManager: Added '{effType.Name}' but it does not implement IWeatherEffect");
                    Destroy(go);
                }
            }
            else
            {
                Debug.LogWarning($"WeatherManager: Could not find effect type for data '{cond.GetType().Name}' (checked multiple name variants)");
                Destroy(go);
            }
        }

        if (uiController != null) uiController.Show(cond, activeDifficulty);
        if (chosenDuration > 0f) stopCoroutine = StartCoroutine(StopAfter(chosenDuration));
    }

    private float GetDurationForDifficulty(WeatherBase cond, int difficulty)
    {
        if (cond == null || cond.durations == null || cond.durations.Length == 0) return 0f;
        int idx = Mathf.Clamp(difficulty - 1, 0, cond.durations.Length - 1);
        return cond.durations[idx];
    }

    private IEnumerator StopAfter(float t)
    {
        yield return new WaitForSeconds(t);
        StopWeather();
    }

    public void StopWeather()
    {
        if (stopCoroutine != null) { StopCoroutine(stopCoroutine); stopCoroutine = null; }
        if (activeEffect != null) { try { activeEffect.End(); } catch { } activeEffect = null; }
        if (activeVisual != null) { Destroy(activeVisual); activeVisual = null; }
        if (uiController != null) uiController.Hide();
        activeCondition = null;
        activeDifficulty = 1;
        Debug.Log("WeatherManager: Weather stopped");
    }

    private Type FindEffectTypeForData(Type dataType)
    {
        string name = dataType.Name;
        string baseName = name;
        if (baseName.EndsWith("Data")) baseName = baseName.Substring(0, baseName.Length - "Data".Length);
        if (baseName.EndsWith("Condition")) baseName = baseName.Substring(0, baseName.Length - "Condition".Length);

        string[] tryNames = new string[]
        {
            baseName + "Effect",
            baseName.Replace("Weather","") + "Effect",
            dataType.Name.Replace("Data","") + "Effect",
            dataType.Name.Replace("Condition","") + "Effect",
            "EarthquakeEffect", // quick common fallback
        };

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var n in tryNames.Distinct())
        {
            foreach (var asm in assemblies)
            {
                try
                {
                    var t = asm.GetType(n, false, true);
                    if (t != null && typeof(IWeatherEffect).IsAssignableFrom(t)) return t;
                }
                catch { }
            }
        }

        foreach (var asm in assemblies)
        {
            Type[] types = null;
            try { types = asm.GetTypes(); } catch { continue; }
            foreach (var t in types)
            {
                if (!typeof(IWeatherEffect).IsAssignableFrom(t)) continue;
                if (t.IsAbstract) continue;
                string tn = t.Name.ToLowerInvariant();
                if (tn.Contains(baseName.ToLowerInvariant()) || tn.Contains(dataType.Name.ToLowerInvariant()))
                    return t;
            }
        }

        return null;
    }

    public WeatherBase[] GetConditions() => conditions;
}
