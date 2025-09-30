using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CrackController : MonoBehaviour
{
    private float life = 5f;
    private bool createCollider = false;
    public void Configure(float lifetime, bool withCollider, string sortingLayer, int order)
    {
        life = Mathf.Max(0.05f, lifetime);
        createCollider = withCollider;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (!string.IsNullOrEmpty(sortingLayer)) sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
        }
        if (createCollider)
        {
            var col = GetComponent<Collider2D>();
            if (col == null)
            {
                var b = gameObject.AddComponent<BoxCollider2D>();
                b.isTrigger = false;
            }
        }
        StartCoroutine(Life());
    }
    private IEnumerator Life()
    {
        yield return new WaitForSeconds(life);
        Destroy(gameObject);
    }
}