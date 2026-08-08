using UnityEngine;

/// <summary>
/// Fırlatılmış silah mermi bileşeni (Hançer, Kılıç, Küçük Sopa, Taş, Mum).
/// Pierce mekaniğiyle X düşmandan geçer.
/// Durduğunda yere WorldItem olarak düşer (azaltılmış dayanıklılıkla).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ThrownWeapon : MonoBehaviour
{
    [Header("Mermi Ayarları")]
    [SerializeField] private float damage = 5f;
    [SerializeField] private int pierceRemaining = 0;
    [SerializeField] private float lifetime = 5f;

    [Header("WorldItem Düşürme")]
    [SerializeField] private ItemSO weaponItemData;
    [SerializeField] private int remainingDurability = 1;
    [SerializeField] private bool dropAsWorldItem = true;

    [Header("Özel Efektler")]
    [SerializeField] private bool createsLight = false;
    [SerializeField] private bool appliesSlow = false;

    private Rigidbody2D rb;
    private bool hasStopped = false;
    private int enemiesHit = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    /// <summary>Silahı belirtilen yönde fırlat.</summary>
    public void Launch(Vector2 direction, float force, ItemSO itemData, int durability, ThrowStyle style)
    {
        weaponItemData = itemData;
        remainingDurability = durability;

        if (itemData != null)
        {
            damage = itemData.throwDamage;
            pierceRemaining = itemData.pierceCount;
            createsLight = itemData.createsLightOnLand;
            appliesSlow = itemData.appliesSlowOnHit;
        }

        // Fırlatma stili
        if (style == ThrowStyle.Arc)
        {
            rb.gravityScale = 1.5f;
            rb.linearVelocity = direction * force;
            rb.angularVelocity = Random.Range(-540f, 540f);
        }
        else // StraightLine
        {
            rb.gravityScale = 0.05f;
            rb.linearVelocity = direction.normalized * (force * 1.2f);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasStopped) return;

        // Düşmana çarptı mı?
        bool isEnemy = collision.gameObject.CompareTag("Enemy");

        if (isEnemy)
        {
            enemiesHit++;
            Debug.Log($"⚔️ [SİLAH İSABET] {collision.gameObject.name} — {damage} hasar! (Pierce kalan: {pierceRemaining - enemiesHit})");

            if (appliesSlow)
            {
                Debug.Log($"🧊 [YAVAŞLATMA] {collision.gameObject.name} yavaşlatıldı!");
            }

            // Pierce kontrolü — henüz geçebilir mi?
            if (enemiesHit <= pierceRemaining)
            {
                // Geç, durma
                Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider);
                return;
            }
        }

        // Durma — yere düş
        StopAndDrop();
    }

    private void StopAndDrop()
    {
        if (hasStopped) return;
        hasStopped = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 2f;

        // Mum ışık efekti
        if (createsLight)
        {
            SpawnLightSource(transform.position);
        }

        // WorldItem olarak yere düşür (toplanabilir)
        if (dropAsWorldItem && weaponItemData != null && remainingDurability > 0)
        {
            // Kısa gecikmeyle WorldItem oluştur ve bu mermiyi sil
            Invoke(nameof(SpawnWorldItem), 0.3f);
        }
        else
        {
            Destroy(gameObject, 0.5f);
        }
    }

    private void SpawnWorldItem()
    {
        GameObject worldItemObj = new GameObject($"WorldItem_{weaponItemData.itemName}");
        worldItemObj.transform.position = transform.position;

        SpriteRenderer sr = worldItemObj.AddComponent<SpriteRenderer>();
        sr.sprite = weaponItemData.icon;
        sr.sortingOrder = 5;

        BoxCollider2D col = worldItemObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        col.isTrigger = true;

        WorldItem wItem = worldItemObj.AddComponent<WorldItem>();
        wItem.Setup(weaponItemData, 1);

        Debug.Log($"🗡️ [SİLAH DÜŞTÜ] {weaponItemData.itemName} yere düştü (Kalan Dayanıklılık: {remainingDurability})");

        Destroy(gameObject);
    }

    private void SpawnLightSource(Vector2 position)
    {
        GameObject lightObj = new GameObject("CandleLight");
        lightObj.transform.position = position;

        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color lightColor = new Color(1f, 0.9f, 0.4f, 0.4f);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = dist <= 14f ? lightColor : Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        SpriteRenderer sr = lightObj.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        sr.sortingOrder = 4;
        lightObj.transform.localScale = Vector3.one * 3f;

        Debug.Log($"🕯️ [IŞIK] Mum ışığı oluşturuldu: {position}");
        Destroy(lightObj, 30f);
    }
}
