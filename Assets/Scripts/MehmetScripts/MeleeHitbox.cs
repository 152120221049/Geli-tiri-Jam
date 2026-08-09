using UnityEngine;

/// <summary>
/// Yakın dövüş savurmalarında (Kılıç, Hançer, Sopa vb.) fare yönüne doğru doğan
/// görsel ve fiziksel savurma hitbox'ı (Slash Hitbox Arc).
/// - Düşmanlara temas ettiğinde hasar verir.
/// - Kısa bir süre sonra (ör. 0.18s) kendiliğinden yok olur.
/// - Gelecekte doğrudan sweep animasyonu eklenmeye müsaittir.
/// </summary>
public class MeleeHitbox : MonoBehaviour
{
    private float damage;
    private LayerMask enemyLayer;
    private GameObject attacker;
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Hitbox'ı başlatır ve boyutlandırır.
    /// </summary>
    public static void CreateHitbox(Vector2 origin, Vector2 direction, float range, float arcAngle, float damage, LayerMask enemyLayer, GameObject attacker, float lifetime = 0.18f)
    {
        GameObject hitboxObj = new GameObject("MeleeHitbox_Arc");
        hitboxObj.transform.position = new Vector3(origin.x, origin.y, -0.5f);

        float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        hitboxObj.transform.rotation = Quaternion.Euler(0, 0, angleDeg);

        MeleeHitbox hb = hitboxObj.AddComponent<MeleeHitbox>();
        hb.Initialize(range, arcAngle, damage, enemyLayer, attacker, lifetime);
    }

    private void Initialize(float range, float arcAngle, float damage, LayerMask enemyLayer, GameObject attacker, float lifetime)
    {
        this.damage = damage;
        this.enemyLayer = enemyLayer;
        this.attacker = attacker;

        // Görsel Savurma Efekti (Arc Texture)
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateArcSprite(range, arcAngle);
        spriteRenderer.color = new Color(1f, 0.45f, 0.15f, 0.75f);
        spriteRenderer.sortingOrder = 50;

        // Trigger Collider
        PolygonCollider2D polyCol = gameObject.AddComponent<PolygonCollider2D>();
        polyCol.isTrigger = true;
        polyCol.points = CreateArcColliderPoints(range, arcAngle);

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (attacker != null && collision.gameObject == attacker) return;

        // Layer mask kontrolü
        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            Debug.Log($"⚔️ [MELEE HITBOX] {collision.gameObject.name} hitbox ile vuruldu! Hasar: {damage}");

            // Düşmana hasar verme (örn. Health / Enemy bileşeni)
            collision.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    private Sprite CreateArcSprite(float range, float arcAngle)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(0, size / 2f);
        float halfArc = arcAngle / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                float distNorm = pos.magnitude / size;
                float angle = Vector2.Angle(Vector2.right, pos);

                if (pos.y < 0) angle = -angle;

                if (distNorm >= 0.2f && distNorm <= 1.0f && Mathf.Abs(angle) <= halfArc)
                {
                    float alpha = Mathf.Sin(distNorm * Mathf.PI) * (1f - Mathf.Abs(angle) / halfArc);
                    pixels[y * size + x] = new Color(1f, 0.9f, 0.3f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0f, 0.5f), size / range);
    }

    private Vector2[] CreateArcColliderPoints(float range, float arcAngle)
    {
        int segments = 10;
        Vector2[] points = new Vector2[segments + 2];
        points[0] = Vector2.zero;

        float startAngle = -arcAngle / 2f * Mathf.Deg2Rad;
        float endAngle = arcAngle / 2f * Mathf.Deg2Rad;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
            points[i + 1] = new Vector2(Mathf.Cos(currentAngle) * range, Mathf.Sin(currentAngle) * range);
        }

        return points;
    }
}
