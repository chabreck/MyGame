using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HealthPickup : MonoBehaviour
{
    public float healAmount = 100f;
    public AudioClip collectSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var hh = other.GetComponent<HeroHealth>();
        if (hh != null) hh.Heal(healAmount);
        if (collectSound != null) AudioSource.PlayClipAtPoint(collectSound, transform.position, 0.7f);
        Destroy(gameObject);
    }
}