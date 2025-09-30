using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageFlash : MonoBehaviour
{
    public Sprite whiteSprite;
    public float flashDuration = 0.1f;

    private SpriteRenderer[] renderers;
    private Dictionary<SpriteRenderer, Sprite> originalSprites = new Dictionary<SpriteRenderer, Sprite>();
    private Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private Coroutine flashCoroutine;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;
            originalSprites[r] = r.sprite;
            originalColors[r] = r.color;
        }
    }

    private void OnDestroy()
    {
        RestoreAll();
    }

    public void TriggerFlash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        ApplyFlash();
        yield return new WaitForSeconds(Mathf.Max(0.0001f, flashDuration));
        RestoreAll();
        flashCoroutine = null;
    }

    private void ApplyFlash()
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (whiteSprite != null)
            {
                r.sprite = whiteSprite;
                r.color = Color.white;
            }
            else
            {
                r.color = Color.white;
            }
        }
    }

    private void RestoreAll()
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (originalSprites.ContainsKey(r)) r.sprite = originalSprites[r];
            if (originalColors.ContainsKey(r)) r.color = originalColors[r];
        }
    }
}