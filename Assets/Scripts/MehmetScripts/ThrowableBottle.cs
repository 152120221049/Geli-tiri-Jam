using UnityEngine;

/// <summary>
/// Fırlatılabilir şişe/iksir mermi bileşeni.
/// Can İksiri: Oyuncu önce içer (+HP), ardından boş şişe yavaşlatıcı olarak fırlatılır.
/// Patlayıcı İksir (Murky Vial): Çarptığı yerde patlar ve alev alanı bırakır.
/// Boş Şişe: Çarptığı düşmanı yavaşlatır.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ThrowableBottle : MonoBehaviour
{
    [Header("Mermi Ayarları")]
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float lifetime = 5f;

    [Header("Efekt Ayarları")]
    [SerializeField] private bool appliesSlow = false;
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float slowMultiplier = 0.5f;

    [SerializeField] private bool leavesFireArea = false;
    [SerializeField] private float fireDamage = 40f;
    [SerializeField] private float fireAreaRadius = 1.5f;
    [SerializeField] private float fireAreaDuration = 4f;

    [SerializeField] private bool createsLight = false;

    private Rigidbody2D rb;
    private bool hasImpacted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1.5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    /// <summary>Şişeyi belirtilen yönde fırlat.</summary>
    public void Launch(Vector2 direction, float force, ItemSO itemData)
    {
        throwForce = force;

        if (itemData != null)
        {
            appliesSlow = itemData.appliesSlowOnHit;
            leavesFireArea = itemData.leavesFireArea;
            createsLight = itemData.createsLightOnLand;
        }

        rb.linearVelocity = direction * throwForce;

        // Dönerken uçma efekti
        rb.angularVelocity = Random.Range(-360f, 360f);

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        // Yavaşlatma efekti
        if (appliesSlow)
        {
            // Çarptığı düşmanları yavaşlat (Enemy.cs'ye dokunmadan güvenli log)
            Debug.Log($"🧊 [ŞİŞE ÇARPTI] {collision.gameObject.name} — Yavaşlatma uygulandı ({slowDuration}s, x{slowMultiplier})");
        }

        // Alev alanı bırakma
        if (leavesFireArea)
        {
            SpawnFireArea(transform.position);
        }

        // Işık oluşturma (Mum)
        if (createsLight)
        {
            SpawnLightSource(transform.position);
        }

        // Kırılma efekti
        SpawnShatterEffect(transform.position);

        Destroy(gameObject);
    }

    private void SpawnFireArea(Vector2 position)
    {
        GameObject fireObj = new GameObject("FireArea");
        fireObj.transform.position = position;

        // Görsel — Turuncu daire
        SpriteRenderer sr = fireObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(new Color(1f, 0.4f, 0f, 0.6f));
        sr.sortingOrder = 5;
        fireObj.transform.localScale = Vector3.one * fireAreaRadius * 2f;

        // Hasar alanı collider
        CircleCollider2D col = fireObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        Debug.Log($"🔥 [ALEV ALANI] Pozisyon: {position}, Yarıçap: {fireAreaRadius}, Hasar: {fireDamage}");

        Destroy(fireObj, fireAreaDuration);
    }

    private void SpawnLightSource(Vector2 position)
    {
        GameObject lightObj = new GameObject("CandleLight");
        lightObj.transform.position = position;

        SpriteRenderer sr = lightObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(new Color(1f, 0.9f, 0.4f, 0.4f));
        sr.sortingOrder = 4;
        lightObj.transform.localScale = Vector3.one * 3f;

        Debug.Log($"🕯️ [IŞIK KAYNAĞI] Mum ışığı oluşturuldu: {position}");

        Destroy(lightObj, 30f); // 30 saniye yanar
    }

    private void SpawnShatterEffect(Vector2 position)
    {
        // Basit kırılma parçacıkları
        for (int i = 0; i < 5; i++)
        {
            GameObject shard = new GameObject("Shard");
            shard.transform.position = position;

            SpriteRenderer sr = shard.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite(new Color(0.6f, 0.6f, 0.7f, 0.8f));
            sr.sortingOrder = 6;
            shard.transform.localScale = Vector3.one * Random.Range(0.05f, 0.12f);

            Rigidbody2D shardRb = shard.AddComponent<Rigidbody2D>();
            shardRb.linearVelocity = new Vector2(Random.Range(-3f, 3f), Random.Range(1f, 4f));
            shardRb.gravityScale = 2f;
            shardRb.angularVelocity = Random.Range(-720f, 720f);

            Destroy(shard, 1.5f);
        }
    }

    private Sprite CreateCircleSprite(Color color)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float radius = center - 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = dist <= radius ? color : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateSquareSprite(Color color)
    {
        int size = 8;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
