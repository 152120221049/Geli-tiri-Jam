using UnityEngine;

/// <summary>
/// Ok mermi bileşeni (Normal Ok ve Ateşli Ok).
/// Hız yönüne göre döner, düşmanlara çarpınca hasar verir.
/// Ateşli Ok çarptığı yerde alev alanı bırakır.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour
{
    [Header("Mermi Ayarları")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool isFireArrow = false;

    [Header("Ateşli Ok Ayarları")]
    [SerializeField] private float fireAreaRadius = 1.0f;
    [SerializeField] private float fireAreaDuration = 3f;
    [SerializeField] private float fireDamage = 10f;

    private Rigidbody2D rb;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0.15f; // Hafif eğim etkisi
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    /// <summary>Oku belirtilen yönde fırlat.</summary>
    public void Launch(Vector2 direction, float arrowSpeed, float arrowDamage, bool fireArrow)
    {
        speed = arrowSpeed;
        damage = arrowDamage;
        isFireArrow = fireArrow;

        rb.linearVelocity = direction.normalized * speed;

        // Ok yönüne bak
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (hasHit) return;

        // Ok hız yönüne doğru döner
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;
        hasHit = true;

        // Hasar log
        Debug.Log($"🏹 [OK İSABET] {collision.gameObject.name} — {damage} hasar!");

        // Ateşli Ok alev alanı
        if (isFireArrow)
        {
            SpawnFireArea(transform.position);
        }

        // Ok saplanır ve biraz sonra kaybolur
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Static;

        Destroy(gameObject, 2f);
    }

    private void SpawnFireArea(Vector2 position)
    {
        GameObject fireObj = new GameObject("ArrowFireArea");
        fireObj.transform.position = position;

        SpriteRenderer sr = fireObj.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(32, 32);
        Color fireColor = new Color(1f, 0.35f, 0f, 0.5f);
        Color[] pixels = new Color[32 * 32];
        float center = 16f;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * 32 + x] = dist <= 14f ? fireColor : Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        sr.sortingOrder = 5;
        fireObj.transform.localScale = Vector3.one * fireAreaRadius * 2f;

        CircleCollider2D col = fireObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        Debug.Log($"🔥 [ATEŞLİ OK] Alev alanı oluşturuldu: {position}");

        Destroy(fireObj, fireAreaDuration);
    }
}
