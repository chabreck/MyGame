using UnityEngine;
using System.Collections;

public class ExperienceCollector : MonoBehaviour
{
    [Header("Experience Settings")]
    public int amount;
    
    [Header("Magnetic Pickup Settings")]
    public float magneticRange = 1f;
    public float attractionSpeed = 8f;
    public float collectDistance = 0.5f;
    
    [Header("Visual Effects")]
    public float floatSpeed = 1f;
    public float floatAmplitude = 0.2f;
    
    [Header("Sound Effects")]
    public AudioClip collectSound;
    public float collectSoundVolume = 0.7f;
    
    private Transform player;
    private HeroExperience heroExperience;
    private bool isBeingAttracted = false;
    private bool isCollected = false;
    private Vector3 startPosition;
    private Rigidbody2D rb;

    private static float lastCollectTime = 0f;
    private static int collectCountThisFrame = 0;
    private static float collectSoundCooldown = 0.05f;
    
    private void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.gravityScale = 0f;
        rb.drag = 2f;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            heroExperience = playerObj.GetComponent<HeroExperience>();
        }
    }
    
    private void Update()
    {
        if (player == null || isCollected) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (!isBeingAttracted && distanceToPlayer <= magneticRange)
        {
            StartAttraction();
        }
        
        if (isBeingAttracted)
        {
            MoveTowardsPlayer();
            
            if (distanceToPlayer <= collectDistance)
            {
                CollectExperience();
            }
        }
        else
        {
            FloatAnimation();
        }
    }
    
    private void StartAttraction()
    {
        isBeingAttracted = true;
        
        if (rb != null)
        {
            Vector2 jumpDirection = (player.position - transform.position).normalized;
            rb.AddForce(jumpDirection * 2f, ForceMode2D.Impulse);
        }
    }
    
    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        float currentDistance = Vector2.Distance(transform.position, player.position);
        
        float speedMultiplier = Mathf.Lerp(1f, 2f, 1f - (currentDistance / magneticRange));
        
        if (rb != null)
        {
            rb.velocity = direction * attractionSpeed * speedMultiplier;
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, 
                attractionSpeed * speedMultiplier * Time.deltaTime);
        }
    }
    
    private void FloatAnimation()
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPosition.x, startPosition.y + yOffset, startPosition.z);
    }
    
    private void CollectExperience()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        PlayOptimizedCollectSound();
        
        if (heroExperience != null)
        {
            heroExperience.AddExp(amount);
        }
        
        StartCoroutine(CollectionEffect());
    }
    
    private void PlayOptimizedCollectSound()
    {
        if (collectSound == null || SettingsManager.Instance == null) return;
        
        float currentTime = Time.time;
        collectCountThisFrame++;
        
        if (currentTime - lastCollectTime >= collectSoundCooldown)
        {
            collectCountThisFrame = 0;
            lastCollectTime = currentTime;
            
            float volumeMultiplier = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(collectCountThisFrame / 10f));
            SettingsManager.Instance.PlaySFX(collectSound, collectSoundVolume * volumeMultiplier);
        }
        else if (collectCountThisFrame % 2 == 0)
        {
            float volumeMultiplier = Mathf.Lerp(0.8f, 0.4f, Mathf.Clamp01(collectCountThisFrame / 15f));
            SettingsManager.Instance.PlaySFX(collectSound, collectSoundVolume * volumeMultiplier);
        }
    }
    
    private IEnumerator CollectionEffect()
    {
        Vector3 originalScale = transform.localScale;
        float effectDuration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < effectDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / effectDuration;
            
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
            transform.Rotate(0, 0, 720 * Time.deltaTime);
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magneticRange);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectDistance);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        if (other.CompareTag("Player"))
        {
            if (!isBeingAttracted)
            {
                StartAttraction();
            }
        }
    }
    
    public static void ResetSoundCounters()
    {
        lastCollectTime = 0f;
        collectCountThisFrame = 0;
    }
}