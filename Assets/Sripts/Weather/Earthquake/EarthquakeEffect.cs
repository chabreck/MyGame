using System.Collections;
using UnityEngine;
using Cinemachine;

public class EarthquakeEffect : MonoBehaviour, IWeatherEffect
{
    public EarthquakeWeatherData d;

    public bool overrideCrackInterval = false;
    public float inspectorCrackIntervalMin = 3f;
    public float inspectorCrackIntervalMax = 6f;
    public float inspectorFixedCrackInterval = 4f;
    public bool useFixedInspectorInterval = false;

    public int cracksPerSpawn = 3;

    public CinemachineVirtualCamera assignedVcam;
    public float cinematShakeFrequencyGain = 2f;

    private float duration;
    private int difficultyIndex;
    private Coroutine spawnCoroutine;
    private Coroutine shakeCoroutine;
    private HeroModifierSystem heroMods;
    private bool active;
    private Transform cracksParent;
    private Camera mainCam;
    private GameObject particleGO;
    private ParticleSystem ps;
    private Texture2D circleTex;
    private Vector3 cameraOriginalPos;

    private CinemachineVirtualCamera runtimeVcam;
    private CinemachineBasicMultiChannelPerlin runtimePerlin;
    private float perlinOrigAmp = 0f;
    private float perlinOrigFreq = 0f;
    private Transform cameraShakeRoot;

    public void Begin(WeatherBase baseData, float overrideDuration, int difficulty)
    {
        d = baseData as EarthquakeWeatherData;
        if (d == null) return;
        difficultyIndex = Mathf.Clamp(difficulty, 1, 3) - 1;
        duration = overrideDuration > 0f ? overrideDuration : (d.customDurations != null && d.customDurations.Length > difficultyIndex ? d.customDurations[difficultyIndex] : (d.durations != null && d.durations.Length > difficultyIndex ? d.durations[difficultyIndex] : 0f));
        heroMods = Object.FindObjectOfType<HeroModifierSystem>();
        float reduce = (d.speedReduction != null && d.speedReduction.Length > difficultyIndex) ? d.speedReduction[difficultyIndex] : 0f;
        if (heroMods != null && reduce != 0f) heroMods.AddModifier(StatType.MoveSpeed, -reduce, duration);
        mainCam = Camera.main;
        cameraOriginalPos = mainCam != null ? mainCam.transform.position : Vector3.zero;
        cracksParent = new GameObject("Earthquake_Cracks").transform;
        cracksParent.SetParent(transform, false);
        SetupParticleSystem();
        active = true;
        spawnCoroutine = StartCoroutine(SpawnLoop());
        shakeCoroutine = StartCoroutine(ShakeLoop());
    }

    public void End()
    {
        if (!active) return;
        active = false;
        if (spawnCoroutine != null) { StopCoroutine(spawnCoroutine); spawnCoroutine = null; }
        if (shakeCoroutine != null) { StopCoroutine(shakeCoroutine); shakeCoroutine = null; }
        if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Destroy(particleGO, 1.0f);
        Destroy(cracksParent.gameObject, 0.1f);
        Destroy(this.gameObject, 0.1f);
        if (mainCam != null)
        {
            if (cameraShakeRoot != null)
            {
                var cam = mainCam.transform;
                cam.SetParent(cameraShakeRoot.parent, true);
                Destroy(cameraShakeRoot.gameObject);
                cameraShakeRoot = null;
            }
            mainCam.transform.position = cameraOriginalPos;
        }
        RestoreCinemachinePerlin();
    }

    private void SetupParticleSystem()
    {
        particleGO = new GameObject("EarthquakeDust");
        particleGO.transform.SetParent(transform, false);
        particleGO.transform.localPosition = Vector3.zero;
        ps = particleGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        main.duration = Mathf.Max(0.1f, duration);
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(d.dustStartSpeedMin * 0.5f, d.dustStartSpeedMax * 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(d.dustStartSizeMin * 0.4f, d.dustStartSizeMax * 0.6f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = Mathf.Max(100, d.dustBurstCount * 3);
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.enabled = true;
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Max(0.01f, d.dustRadius * 0.6f);
        var col = ps.colorOverLifetime;
        col.enabled = true;
        if (d.dustColorGradient == null)
        {
            Gradient g = new Gradient();
            g.SetKeys(new GradientColorKey[] {
                new GradientColorKey(new Color(0.6f,0.5f,0.38f,1f),0f),
                new GradientColorKey(new Color(0.85f,0.78f,0.65f,1f),0.6f),
                new GradientColorKey(new Color(0.95f,0.92f,0.88f,0.6f),1f)
            }, new GradientAlphaKey[] {
                new GradientAlphaKey(1f,0f),
                new GradientAlphaKey(0.6f,0.6f),
                new GradientAlphaKey(0f,1f)
            });
            d.dustColorGradient = g;
        }
        col.color = new ParticleSystem.MinMaxGradient(d.dustColorGradient);
        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.2f);
        sizeCurve.AddKey(0.2f, 1.0f);
        sizeCurve.AddKey(1f, 1.2f);
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        circleTex = CreateCircleTexture(64);
        renderer.material.mainTexture = circleTex;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private IEnumerator SpawnLoop()
    {
        float elapsed = 0f;
        while (elapsed < duration && active)
        {
            float wait = ComputeNextCrackWait();
            yield return new WaitForSeconds(wait);
            if (!active) break;
            for (int i = 0; i < cracksPerSpawn; i++)
                SpawnCrackAndDust();
            elapsed += wait;
        }
        End();
    }

    private float ComputeNextCrackWait()
    {
        if (useFixedInspectorInterval)
        {
            return Mathf.Max(0.01f, inspectorFixedCrackInterval);
        }
        if (overrideCrackInterval)
        {
            return Random.Range(Mathf.Min(inspectorCrackIntervalMin, inspectorCrackIntervalMax), Mathf.Max(inspectorCrackIntervalMin, inspectorCrackIntervalMax));
        }
        float min = (d.crackIntervalMin != null && d.crackIntervalMin.Length > difficultyIndex) ? d.crackIntervalMin[difficultyIndex] : 1f;
        float max = (d.crackIntervalMax != null && d.crackIntervalMax.Length > difficultyIndex) ? d.crackIntervalMax[difficultyIndex] : min;
        return (min >= max) ? min : Random.Range(min, max);
    }

    private void SpawnCrackAndDust()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;
        float halfH = mainCam.orthographicSize;
        float halfW = halfH * mainCam.aspect;
        Vector2 center = mainCam.transform.position;
        float pad = d.crackSpawnPadding;
        float x = Random.Range(center.x - halfW + pad, center.x + halfW - pad);
        float y = Random.Range(center.y - halfH + pad, center.y + halfH - pad);
        Vector3 pos = new Vector3(x, y, 0f);
        if (ps != null)
        {
            ps.transform.position = pos;
            ps.Clear();
            ps.Emit(Mathf.Max(1, Mathf.RoundToInt(d.dustBurstCount * 0.7f)));
        }
        for (int i = 0; i < 1; i++)
        {
            GameObject go = new GameObject("Crack");
            go.transform.SetParent(cracksParent, true);
            float jitterX = Random.Range(-0.3f, 0.3f);
            float jitterY = Random.Range(-0.3f, 0.3f);
            go.transform.position = pos + new Vector3(jitterX, jitterY, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            if (d.crackSprite != null)
            {
                sr.sprite = d.crackSprite;
            }
            else
            {
                sr.sprite = CreateCrackSprite(192, 20);
                float scale = Random.Range(0.8f, 1.2f);
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }
            float angle = Random.Range(0f, 360f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            sr.sortingLayerName = d.crackSortingLayer;
            sr.sortingOrder = d.crackOrderInLayer;
            var crack = go.AddComponent<CrackController>();
            crack.Configure(d.crackLifeTime, d.crackCreatesCollider, d.crackSortingLayer, d.crackOrderInLayer);
        }
    }

    private Sprite CreateCrackSprite(int width, int height)
    {
        int w = Mathf.Max(8, width);
        int h = Mathf.Max(4, height);
        Texture2D tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
        Color32[] cols = new Color32[w * h];
        float centerY = h * 0.5f;
        float thickness = Mathf.Clamp01(0.18f) * h;
        float noiseScale = 0.1f + (float)Random.Range(0, 20) * 0.001f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w;
                float ny = (float)y / h;
                float px = nx * 4.0f;
                float n = Mathf.PerlinNoise(px * (1f + noiseScale), ny * (1f + noiseScale));
                float lineCenter = centerY + (n - 0.5f) * (h * 0.25f);
                float dist = Mathf.Abs(y - lineCenter);
                float a = Mathf.Clamp01(1f - (dist / thickness));
                a *= a;
                float edgeFade = Mathf.Clamp01((float)x / (w * 0.2f));
                a *= Mathf.Clamp01(1f - edgeFade);
                byte alpha = (byte)Mathf.RoundToInt(a * 255f);
                Color32 c = new Color32(20, 18, 16, alpha);
                cols[y * w + x] = c;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        Rect r = new Rect(0, 0, tex.width, tex.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        Sprite sp = Sprite.Create(tex, r, pivot, 100f, 0, SpriteMeshType.FullRect);
        return sp;
    }

    private IEnumerator ShakeLoop()
    {
        AcquireCinemachinePerlin();
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) yield break;
        if (cameraShakeRoot == null)
        {
            cameraShakeRoot = new GameObject("CameraShakeRoot").transform;
            cameraShakeRoot.position = mainCam.transform.position;
            cameraShakeRoot.rotation = mainCam.transform.rotation;
            cameraShakeRoot.SetParent(mainCam.transform.parent, true);
            mainCam.transform.SetParent(cameraShakeRoot, true);
        }
        float elapsed = 0f;
        while (elapsed < duration && active)
        {
            float pause = Random.Range(0.6f, 1.2f);
            yield return new WaitForSeconds(pause);
            if (!active) break;
            float dur = Mathf.Max(0.05f, d.cameraShakeDurationPerWave);
            float intensity = Mathf.Max(0.05f, d.cameraShakeIntensity);
            if (runtimePerlin != null)
            {
                StartCoroutine(DoCinemachineShake(intensity, dur));
            }
            else
            {
                yield return StartCoroutine(DoWorldShake(cameraShakeRoot, intensity * 0.5f, dur));
            }
            elapsed += pause + dur;
        }
        if (cameraShakeRoot != null)
        {
            mainCam.transform.SetParent(cameraShakeRoot.parent, true);
            Destroy(cameraShakeRoot.gameObject);
            cameraShakeRoot = null;
        }
        RestoreCinemachinePerlin();
    }

    private IEnumerator DoCinemachineShake(float intensity, float time)
    {
        if (runtimePerlin == null) yield break;
        runtimePerlin.m_AmplitudeGain = intensity;
        runtimePerlin.m_FrequencyGain = cinematShakeFrequencyGain;
        yield return new WaitForSeconds(time);
        runtimePerlin.m_AmplitudeGain = perlinOrigAmp;
        runtimePerlin.m_FrequencyGain = perlinOrigFreq;
    }

    private IEnumerator DoWorldShake(Transform target, float intensity, float duration)
    {
        if (target == null) yield break;
        float t = 0f;
        Vector3 orig = target.localPosition;
        while (t < duration)
        {
            t += Time.deltaTime;
            float damper = 1f - (t / duration);
            float rx = (Random.value * 2f - 1f) * intensity * damper;
            float ry = (Random.value * 2f - 1f) * intensity * damper;
            target.localPosition = orig + new Vector3(rx, ry, 0f);
            yield return null;
        }
        target.localPosition = orig;
    }

    private void AcquireCinemachinePerlin()
    {
        runtimeVcam = assignedVcam;
        if (runtimeVcam == null)
        {
            try
            {
                var activeBrain = CinemachineCore.Instance.GetActiveBrain(0);
                if (activeBrain != null && activeBrain.ActiveVirtualCamera != null)
                {
                    var active = activeBrain.ActiveVirtualCamera;
                    var go = active != null ? active.VirtualCameraGameObject : null;
                    if (go != null) runtimeVcam = go.GetComponent<CinemachineVirtualCamera>();
                }
            }
            catch { }
        }
        if (runtimeVcam == null) runtimeVcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (runtimeVcam != null)
        {
            runtimePerlin = runtimeVcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (runtimePerlin != null)
            {
                perlinOrigAmp = runtimePerlin.m_AmplitudeGain;
                perlinOrigFreq = runtimePerlin.m_FrequencyGain;
            }
            else
            {
                runtimePerlin = null;
            }
        }
    }

    private void RestoreCinemachinePerlin()
    {
        if (runtimePerlin != null)
        {
            runtimePerlin.m_AmplitudeGain = perlinOrigAmp;
            runtimePerlin.m_FrequencyGain = perlinOrigFreq;
        }
    }

    private Texture2D CreateCircleTexture(int size)
    {
        int s = Mathf.Max(8, size);
        Texture2D tex = new Texture2D(s, s, TextureFormat.ARGB32, false);
        Color32[] cols = new Color32[s * s];
        Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
        float r = s * 0.5f;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(1f - (dist / r));
                a = Mathf.Pow(a, 1.2f);
                Color32 col = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                cols[y * s + x] = col;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    private void OnDestroy()
    {
        active = false;
        RestoreCinemachinePerlin();
    }
}
