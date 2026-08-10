using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Canavarların zamanlı, dalga bazlı veya alan içinde otomatik doğmasını sağlayan esnek Spawner sistemi.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public enum SpawnLocationMode
    {
        RandomRadius, // Spawner veya Oyuncu etrafındaki rastgele bir alanda doğma
        SpawnPoints   // Inspector'da verilen belirli noktalardan rastgele seçip doğma
    }

    [Header("Düşman Prefab'ları")]
    [Tooltip("Doğacak canavar prefab'ları. Kod bunlardan rastgele birini seçecektir.")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();

    [Header("Doğma Konum Modu")]
    [SerializeField] private SpawnLocationMode locationMode = SpawnLocationMode.RandomRadius;

    [Header("Noktasal Doğma (SpawnPoints Modu İçin)")]
    [Tooltip("Eğer SpawnPoints modu seçiliyse düşmanların doğacağı Transform noktaları.")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Dairesel Doğma (RandomRadius Modu İçin)")]
    [Tooltip("Doğma merkezini Oyuncu yap (işaretlenmezse Spawner'ın kendi pozisyonu kullanılır).")]
    [SerializeField] private bool usePlayerAsCenter = false;
    [Tooltip("Doğacak alanın minimum uzaklığı (karakterin tam dibinde doğmasınlar diye).")]
    [SerializeField] private float minSpawnRadius = 3f;
    [Tooltip("Doğacak alanın maksimum uzaklığı.")]
    [SerializeField] private float maxSpawnRadius = 10f;

    [Header("Zamanlayıcı ve Limit Ayarları")]
    [Tooltip("Kaç saniyede bir yeni bir düşman doğacağı.")]
    [SerializeField] private float spawnInterval = 3f;
    [Tooltip("Sahnede aynı anda kalabilecek maksimum aktif düşman sayısı.")]
    [SerializeField] private int maxActiveEnemies = 10;
    [Tooltip("Bu spawner'ın toplam doğurabileceği maksimum düşman sayısı (0 = Sınırsız).")]
    [SerializeField] private int maxTotalEnemies = 0;
    [Tooltip("Oyun başlar başlamaz doğma işlemi başlasın mı?")]
    [SerializeField] private bool spawnOnStart = true;

    // ── Dahili Değişkenler ──
    private List<GameObject> activeEnemies = new List<GameObject>();
    private float spawnTimer = 0f;
    private int totalSpawnedCount = 0;
    private bool isSpawningActive = false;
    private Transform playerTransform;

    public bool IsSpawningActive => isSpawningActive;
    public int ActiveEnemyCount => activeEnemies.Count;
    public int TotalSpawnedCount => totalSpawnedCount;

    private void Start()
    {
        FindPlayer();

        if (spawnOnStart)
        {
            StartSpawning();
        }
    }

    private void Update()
    {
        if (!isSpawningActive) return;

        // Ölen/yok edilen düşmanları listeden temizle
        CleanupDestroyedEnemies();

        // Limit kontrolleri
        if (maxActiveEnemies > 0 && activeEnemies.Count >= maxActiveEnemies) return;
        if (maxTotalEnemies > 0 && totalSpawnedCount >= maxTotalEnemies)
        {
            StopSpawning();
            return;
        }

        // Zamanlayıcıyı ilerlet
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemy();
        }
    }

    /// <summary>Doğma işlemini başlatır.</summary>
    public void StartSpawning()
    {
        isSpawningActive = true;
        spawnTimer = 0f;
    }

    /// <summary>Doğma işlemini durdurur.</summary>
    public void StopSpawning()
    {
        isSpawningActive = false;
    }

    /// <summary>Tek bir düşman doğurma işlemini gerçekleştirir.</summary>
    public GameObject SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("⚠️ [EnemySpawner] Doğrulacak düşman prefab'ı listede bulunamadı!", this);
            return null;
        }

        // Rastgele bir prefab seç
        GameObject chosenPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        if (chosenPrefab == null) return null;

        // Doğma pozisyonunu hesapla
        Vector3 spawnPosition = GetSpawnPosition();

        // Düşmanı oluştur
        GameObject newEnemy = Instantiate(chosenPrefab, spawnPosition, Quaternion.identity);

        // Eğer düşman scripti varsa oyuncu referansını atayalım
        Enemy enemyScript = newEnemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            if (playerTransform != null)
            {
                enemyScript.player = playerTransform;
            }
        }

        activeEnemies.Add(newEnemy);
        totalSpawnedCount++;

        Debug.Log($"👾 [EnemySpawner] {newEnemy.name} doğdu! (Aktif: {activeEnemies.Count}, Toplam: {totalSpawnedCount})");

        return newEnemy;
    }

    /// <summary>Seçilen moda göre doğma pozisyonu üretir.</summary>
    private Vector3 GetSpawnPosition()
    {
        if (locationMode == SpawnLocationMode.SpawnPoints && spawnPoints != null && spawnPoints.Count > 0)
        {
            // Temiz noktaları filtrele
            List<Transform> validPoints = spawnPoints.FindAll(p => p != null);
            if (validPoints.Count > 0)
            {
                Transform selectedPoint = validPoints[Random.Range(0, validPoints.Count)];
                return selectedPoint.position;
            }
        }

        // RandomRadius Modu veya varsayılan
        Vector3 centerPos = transform.position;
        if (usePlayerAsCenter)
        {
            FindPlayer();
            if (playerTransform != null) centerPos = playerTransform.position;
        }

        // Rastgele açı ve mesafe
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(minSpawnRadius, maxSpawnRadius);

        Vector3 offset = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
        return centerPos + offset;
    }

    /// <summary>Oyuncu objesini etiket ile bulur.</summary>
    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    /// <summary>Yok edilen düşmanları aktif listeden temizler.</summary>
    private void CleanupDestroyedEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);
            }
        }
    }

    // ═══════════════════════════════════════════
    //  EDITOR GIZMOS (Görselleştirme)
    // ═══════════════════════════════════════════
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;

        if (locationMode == SpawnLocationMode.RandomRadius)
        {
            Vector3 center = transform.position;
            if (usePlayerAsCenter && Application.isPlaying && playerTransform != null)
            {
                center = playerTransform.position;
            }

            // Min ve Max halkalarını çiz
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Turuncu (Min)
            Gizmos.DrawWireSphere(center, minSpawnRadius);

            Gizmos.color = Color.green; // Yeşil (Max)
            Gizmos.DrawWireSphere(center, maxSpawnRadius);
        }
        else if (locationMode == SpawnLocationMode.SpawnPoints && spawnPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.4f);
                    Gizmos.DrawLine(transform.position, point.position);
                }
            }
        }
    }
}
