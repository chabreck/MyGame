using System;
using System.Collections;
using UnityEngine;

public class FearNetwork : MonoBehaviour, IUpgrade
{
    [Header("Settings")]
    [SerializeField] private Sprite icon;
    [SerializeField] private string[] tags;
    private const int MAX_LEVEL = 5;

    [Header("Level Effects")]
    [SerializeField] private float baseRadius = 5f;
    [SerializeField] private float slowFactor = 0.4f;
    [SerializeField] private float fearChance = 0.3f;
    [SerializeField] private float fearDuration = 2f;
    [SerializeField] private float screamRadius = 6f;

    public string GetUpgradeID() => "FEAR_NETWORK";
    public string GetTitle(int lvl) => $"Нейросеть Ужаса {new string('I', lvl)}";
    public Sprite Icon => icon;
    public string[] Tags => tags;
    public int MaxLevel => MAX_LEVEL;

    public string GetDescription(int lvl) => lvl switch
    {
        1 => "Враги замедляются рядом",
        2 => "Шанс испуга при контакте",
        3 => "Радиус эффекта увеличен",
        4 => "Испуганные получают двойной урон",
        5 => "При испуге враг пугает других",
        _ => "Нейросеть Ужаса"
    };

    public void Apply(int lvl)
    {
        switch (lvl)
        {
            case 1: StartCoroutine(ApplyFear()); break;
            case 2: fearChance = 0.3f; break;
            case 3: baseRadius *= 1.5f; break;
            case 4: FearEffect.damageMultiplier = 2f; break;
            case 5: FearEffect.onFearApplied += TriggerScream; break;
        }
    }

    private IEnumerator ApplyFear()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            var cols = Physics2D.OverlapCircleAll(transform.position, baseRadius, LayerMask.GetMask("Enemy"));
            foreach (var c in cols)
            {
                var dist = Vector3.Distance(transform.position, c.transform.position);
                float strength = 1 - Mathf.Clamp01(dist / baseRadius);
                c.GetComponent<EnemyStatus>()?.ApplySlow(slowFactor * strength);
                if (UnityEngine.Random.value < fearChance * strength)
                    c.GetComponent<FearEffect>()?.ApplyFear(fearDuration);
            }
        }
    }

    private void TriggerScream(Vector3 pos)
    {
        var cols = Physics2D.OverlapCircleAll(pos, screamRadius, LayerMask.GetMask("Enemy"));
        foreach (var c in cols)
            c.GetComponent<FearEffect>()?.ApplyFear(fearDuration * 0.7f);
    }
}

public class FearEffect : MonoBehaviour
{
    public static float damageMultiplier = 1f;
    public static Action<Vector3> onFearApplied;

    private float endTime;
    private Vector3 dir;

    public void ApplyFear(float duration)
    {
        endTime = Time.time + duration;
        dir = (transform.position - GameObject.FindWithTag("Player").transform.position).normalized;
        onFearApplied?.Invoke(transform.position);
    }

    private void Update()
    {
        if (Time.time < endTime)
            GetComponent<EnemyStatus>()?.ApplyRetreat(dir);
        else
            Destroy(this);
    }
}