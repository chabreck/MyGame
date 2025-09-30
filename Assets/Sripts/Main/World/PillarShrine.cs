using UnityEngine;

public class PillarShrine : MonoBehaviour
{
    public float radius = 2.5f;
    public float requiredStandTime = 8f;
    public GameObject radiusVisualPrefab;
    public int maxUses = 1;

    private int uses = 0;
    private GameObject visualInstance;
    private bool playerInside = false;
    private float insideTimer = 0f;
    private HeroExperience heroExp;

    private void Start()
    {
        SetupColliders();

        if (radiusVisualPrefab != null)
        {
            visualInstance = Instantiate(radiusVisualPrefab, transform.position, Quaternion.identity, transform);
            visualInstance.transform.localScale = Vector3.one * (radius * 2f);
            visualInstance.SetActive(true);
        }

        FindHeroExperience();
    }

    private void SetupColliders()
    {
        var mainCollider = GetComponent<BoxCollider2D>();
        if (mainCollider == null) mainCollider = gameObject.AddComponent<BoxCollider2D>();
        mainCollider.isTrigger = false;

        var triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = radius;
    }

    private void FindHeroExperience()
    {
        heroExp = FindObjectOfType<HeroExperience>();
        if (heroExp == null)
        {
            Debug.LogError("HeroExperience not found in scene!");
        }
    }

    private void Update()
    {
        if (playerInside && uses < maxUses)
        {
            insideTimer += Time.deltaTime;
            Debug.Log($"Pillar progress: {insideTimer}/{requiredStandTime}");

            if (insideTimer >= requiredStandTime)
            {
                GrantLevel();
                uses++;
                
                if (uses >= maxUses)
                {
                    if (visualInstance != null)
                        visualInstance.SetActive(false);
                    GetComponent<Collider2D>().enabled = false;
                    var trigger = GetComponent<CircleCollider2D>();
                    if (trigger != null) trigger.enabled = false;
                }
                
                playerInside = false;
                insideTimer = 0f;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (uses >= maxUses) return;
        if (!other.CompareTag("Player")) return;
        
        playerInside = true;
        insideTimer = 0f;
        Debug.Log("Player entered pillar area");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInside = false;
        insideTimer = 0f;
        Debug.Log("Player left pillar area");
    }

    private void GrantLevel()
    {
        if (heroExp == null)
        {
            FindHeroExperience();
            if (heroExp == null)
            {
                Debug.LogError("Cannot grant level - HeroExperience not found!");
                return;
            }
        }

        try
        {
            int currentExp = heroExp.CurrentExp;
            int expToNextLevel = heroExp.GetExpRequirementForNextLevel();
            int expNeeded = expToNextLevel - currentExp;

            if (expNeeded <= 0)
            {
                Debug.Log("Player is already at max level or has enough exp");
                return;
            }

            heroExp.AddExp(expNeeded);
            Debug.Log($"Pillar granted level! +{expNeeded} EXP");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in GrantLevel: {e.Message}");
        }
    }
}