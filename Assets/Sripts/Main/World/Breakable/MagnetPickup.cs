using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MagnetPickup : MonoBehaviour
{
    public float magnetDuration = 8f;
    public float collectionRadiusBonus = 1.5f;
    public AudioClip collectSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var mods = other.GetComponent<HeroModifierSystem>();
        if (mods != null)
        {
            mods.AddModifier(StatType.CollectionSpeed, collectionRadiusBonus, magnetDuration);
        }
        if (collectSound != null) AudioSource.PlayClipAtPoint(collectSound, transform.position, 0.7f);
        Destroy(gameObject);
    }
}