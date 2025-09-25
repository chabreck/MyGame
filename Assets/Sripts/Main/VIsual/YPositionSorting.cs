using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YPositionSorting : MonoBehaviour
{
    private SpriteRenderer _sr;

    [Header("Base order")]
    [SerializeField] private int sortingOrderBase = 5000;
    
    private void Awake() => _sr = GetComponent<SpriteRenderer>();

    private void LateUpdate()
    {
        float bottomY = _sr.bounds.min.y;
        int order = Mathf.RoundToInt(sortingOrderBase - bottomY * 100f); //+ order Offset
        _sr.sortingOrder = order;
    }
}