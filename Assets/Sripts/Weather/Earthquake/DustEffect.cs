using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class DustEffect : MonoBehaviour
{
    public ParticleSystem particleSystemReference;
    public float duration = 2.0f;
    public int burstCount = 40;
    public float radius = 1.2f;
    public float startSpeedMin = 0.3f;
    public float startSpeedMax = 1.2f;
    public float startSizeMin = 0.2f;
    public float startSizeMax = 0.6f;
    public Gradient colorOverLifetime;
    public bool useWorldSpace = true;
    public Sprite particleSprite;
    public Material particleMaterial;
    public float upwardBias = 0.6f;

    private void Awake()
    {
        if (particleSystemReference == null)
        {
            var ps = GetComponent<ParticleSystem>();
            if (ps == null) ps = gameObject.AddComponent<ParticleSystem>();
            particleSystemReference = ps;
        }
        ConfigureParticleSystem();
    }

    private void Start()
    {
        PlayOnce();
    }

    private void ConfigureParticleSystem()
    {
        var ps = particleSystemReference;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = Mathf.Max(0.01f, duration);
        main.loop = false;
        main.simulationSpace = useWorldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeedMin, startSpeedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(startSizeMin, startSizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = Mathf.Max(100, burstCount * 3);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.enabled = true;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Max(0.01f, radius);
        shape.arc = 360f;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = main.simulationSpace;
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vel.y = new ParticleSystem.MinMaxCurve(0.0f, upwardBias);
        vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        if (colorOverLifetime == null)
        {
            Gradient g = new Gradient();
            g.SetKeys(new GradientColorKey[] {
                new GradientColorKey(new Color(0.62f,0.52f,0.39f,1f),0f),
                new GradientColorKey(new Color(0.85f,0.78f,0.65f,1f),0.6f),
                new GradientColorKey(new Color(0.95f,0.92f,0.88f,0.6f),1f)
            }, new GradientAlphaKey[] {
                new GradientAlphaKey(1f,0f),
                new GradientAlphaKey(0.6f,0.6f),
                new GradientAlphaKey(0f,1f)
            });
            colorOverLifetime = g;
        }
        col.color = new ParticleSystem.MinMaxGradient(colorOverLifetime);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.1f);
        sizeCurve.AddKey(0.2f, 1.0f);
        sizeCurve.AddKey(1f, 1.4f);
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        if (r == null) r = ps.gameObject.AddComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        if (particleMaterial != null) r.material = particleMaterial;
        else r.material = new Material(Shader.Find("Sprites/Default"));
        if (particleSprite != null)
        {
            r.material.mainTexture = particleSprite.texture;
        }
    }

    private void PlayOnce()
    {
        var ps = particleSystemReference;
        ps.Clear();
        ps.Play();
        ps.Emit(burstCount);
        float maxLifetime = 1.0f;
        var main = ps.main;
        float lifetimeMax = 0.5f;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
        {
            lifetimeMax = main.startLifetime.constantMax;
        }
        else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
        {
            lifetimeMax = main.startLifetime.constant;
        }
        else
        {
            lifetimeMax = 1.0f;
        }
        float destroyAfter = duration + lifetimeMax + 0.5f;
        Destroy(gameObject, destroyAfter);
    }
}
