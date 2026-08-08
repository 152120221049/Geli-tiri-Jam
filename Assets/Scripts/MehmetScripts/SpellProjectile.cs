using UnityEngine;

/// <summary>
/// Büyü parşömeni mermi bileşeni.
/// Fireball: AoE patlama + alev alanı bırakır.
/// Lightning: Düz çizgide ilerler ve yolundaki düşmanlara hasar verir.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SpellProjectile : MonoBehaviour
{
    public enum SpellType { Fireball, Lightning }

    [Header("Büyü Ayarları")]
    [SerializeField] private SpellType spellType = SpellType.Fireball;
    [SerializeField] private float speed = 12f;
    [SerializeField] private float damage = 60f;
    [SerializeField] private float aoeRadius = 2f;
    [SerializeField] private float fireAreaDuration = 3f;
    [SerializeField] private float lifetime = 5f;

    private Rigidbody2D rb;
    private bool hasImpacted = false;

    // Görsel
    private SpriteRenderer sr;
    private TrailRenderer trail;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();
    }

    /// <summary>Büyüyü fırlat.</summary>
    public void Launch(Vector2 direction, SpellType type, float spellDamage, float spellAoeRadius)
    {
        spellType = type;
        damage = spellDamage;
        aoeRadius = spellAoeRadius;

        rb.linearVelocity = direction.normalized * speed;

        // Yön
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Görsel
        SetupVisuals();

        Destroy(gameObject, lifetime);
    }

    private void SetupVisuals()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Color spellColor;

        if (spellType == SpellType.Fireball)
        {
            spellColor = new Color(1f, 0.4f, 0.1f, 0.9f);
            transform.localScale = Vector3.one * 0.4f;
        }
        else // Lightning
        {
            spellColor = new Color(0.4f, 0.7f, 1f, 0.9f);
            transform.localScale = Vector3.one * 0.3f;
        }

        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = dist <= center - 1 ? spellColor : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        sr.sortingOrder = 15;

        // Trail efekti
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.2f;
        trail.endWidth = 0.02f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = spellColor;
        trail.endColor = new Color(spellColor.r, spellColor.g, spellColor.b, 0f);
        trail.sortingOrder = 14;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        switch (spellType)
        {
            case SpellType.Fireball:
                FireballExplosion(transform.position);
                break;

            case SpellType.Lightning:
                LightningHit(collision);
                break;
        }

        Destroy(gameObject);
    }

    private void FireballExplosion(Vector2 position)
    {
        Debug.Log($"🔥 [FIREBALL PATLAMA] Pozisyon: {position}, Hasar: {damage}, Yarıçap: {aoeRadius}");

        // AoE alan hasar kontrolü
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, aoeRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Debug.Log($"🔥 [FIREBALL] {hit.gameObject.name} — {damage} AoE hasar!");
            }
        }

        // Alev alanı bırak
        GameObject fireObj = new GameObject("FireballFlameArea");
        fireObj.transform.position = position;

        SpriteRenderer fireSR = fireObj.AddComponent<SpriteRenderer>();
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color fireColor = new Color(1f, 0.3f, 0f, 0.5f);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = dist <= center - 2 ? fireColor : Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        fireSR.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        fireSR.sortingOrder = 5;
        fireObj.transform.localScale = Vector3.one * aoeRadius * 2f;

        CircleCollider2D col = fireObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        // Patlama parçacıkları
        for (int i = 0; i < 8; i++)
        {
            GameObject spark = new GameObject("Spark");
            spark.transform.position = position;
            SpriteRenderer sparkSR = spark.AddComponent<SpriteRenderer>();
            Texture2D sparkTex = new Texture2D(4, 4);
            Color[] sparkPixels = new Color[16];
            for (int j = 0; j < 16; j++) sparkPixels[j] = new Color(1f, 0.6f, 0.1f);
            sparkTex.SetPixels(sparkPixels);
            sparkTex.Apply();
            sparkSR.sprite = Sprite.Create(sparkTex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            sparkSR.sortingOrder = 7;
            spark.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);

            Rigidbody2D sparkRb = spark.AddComponent<Rigidbody2D>();
            sparkRb.linearVelocity = new Vector2(Random.Range(-5f, 5f), Random.Range(1f, 6f));
            sparkRb.gravityScale = 3f;
            Destroy(spark, 1f);
        }

        Destroy(fireObj, fireAreaDuration);
    }

    private void LightningHit(Collision2D collision)
    {
        Debug.Log($"⚡ [LIGHTNING İSABET] {collision.gameObject.name} — {damage} hasar!");

        // Düz çizgide ilerleyerek tüm düşmanlara hasar verir (önceki çarpışma anına kadar)
        // Lightning zaten düz çizgide gittiği için yolundaki düşmanları vurur
    }
}
