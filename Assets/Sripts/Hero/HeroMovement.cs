using System.Collections;
using UnityEngine;

public class HeroMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector3 initialScale;
    private bool isDashing;
    private float nextDash;
    private Vector2 input;
    private float boost;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        initialScale = transform.localScale;
    }

    public void SetMovementInput(Vector2 inp) => input = inp;
    public void AddSpeedBoost(float amt) => boost += amt;

    public void TryDash()
    {
        if (!isDashing && Time.time >= nextDash)
            StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        nextDash = Time.time + dashCooldown;
        var dir = input != Vector2.zero ? input.normalized : Vector2.right;
        rb.velocity = dir * dashSpeed;
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
    }

    private void FixedUpdate()
    {
        if (isDashing)
            return;
        
        rb.velocity = input * (moveSpeed * (1 + boost));
        
        if (input.x < 0)
        {
            if (spriteRenderer != null)
                spriteRenderer.flipX = true;
            else
                transform.localScale = new Vector3(-Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
        }
        else if (input.x > 0)
        {
            if (spriteRenderer != null)
                spriteRenderer.flipX = false;
            else
                transform.localScale = new Vector3(Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
        }
    }

    public void AddSpeedBoost(float amount, float duration)
    {
        boost += amount;
        StartCoroutine(RemoveSpeedBoost(amount, duration));
    }

    private IEnumerator RemoveSpeedBoost(float amount, float duration)
    {
        yield return new WaitForSeconds(duration);
        boost -= amount;
    }
}
