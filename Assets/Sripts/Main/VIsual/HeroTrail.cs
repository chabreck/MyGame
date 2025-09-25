using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HeroTrail : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private ParticleSystem trailParticles;
    [SerializeField] private float emissionRate = 20f;
    [SerializeField] private Vector2 particleOffset = new Vector2(0f, -0.3f);
    
    private Rigidbody2D rb;
    private ParticleSystem.EmissionModule emissionModule;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (trailParticles == null)
        {
            trailParticles = GetComponentInChildren<ParticleSystem>();
            if (trailParticles == null)
            {
                CreateDefaultParticleSystem();
            }
        }

        // переносим на offset
        trailParticles.transform.localPosition = particleOffset;

        emissionModule = trailParticles.emission;
        emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
        
        trailParticles.Play();
    }

    private void CreateDefaultParticleSystem()
    {
        GameObject go = new GameObject("TrailParticles");
        go.transform.SetParent(transform);
        go.transform.localPosition = particleOffset;
        trailParticles = go.AddComponent<ParticleSystem>();

        var main = trailParticles.main;
        main.startSpeed = 0.5f;
        main.startSize = 0.05f;
        main.startLifetime = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        emissionModule = trailParticles.emission;
        emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(0f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = Color.white;
    }

    private void Update()
    {
        if (rb.velocity.magnitude > 0.1f)
        {
            emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(emissionRate);

            Vector2 v = rb.velocity.normalized;
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90f;
            trailParticles.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
        }
    }
}
