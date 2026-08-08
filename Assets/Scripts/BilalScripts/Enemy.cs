using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed ;
    public Transform player;
    public float damage = 10f; // Oyuncuya verilecek hasar miktarı

    private float targetSpeed; // Ulaşılmak istenen rastgele hız
    private float speedTimer; // Yeni bir hız belirlemek için geri sayım aracı

    void Start()
    {
        // Hızı 3 ile 6 arasında rastgele bir değere ayarla
        speed = Random.Range(3f, 6f);
        targetSpeed = speed;
        speedTimer = Random.Range(1f, 3f); // 1 ile 3 saniye sonra yeni hız belirlenecek

        // Eğer player Inspector üzerinden atanmamışsa "Player" tag'ine sahip objeyi bulmaya çalış.
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    void Update()
    {
        if (knockbackTimer > 0)
        {
            // Geri tepme (saldırma sonrası) durumu
            knockbackTimer -= Time.deltaTime;
            
            // 1 saniye boyunca hız sabit -2 olsun (oyuncudan uzaklaşsın)
            speed = -2f;
            targetSpeed = -2f;

            // Süre bittiğinde tekrar normal hıza dönmek için ayarla
            if (knockbackTimer <= 0)
            {
                speed = Random.Range(4f, 6.5f);
                targetSpeed = speed;
                speedTimer = Random.Range(2f, 4f);
            }
        }
        else
        {
            // Normal rastgele hız değiştirme mantığı
            speedTimer -= Time.deltaTime;
            if (speedTimer <= 0)
            {
                // Süre dolduğunda yeni bir hedef hız (4 ile 6 arası) ve yeni bir bekleme süresi belirle
                targetSpeed = Random.Range(4f, 6.5f);
                speedTimer = Random.Range(2f, 4f); // 2-4 saniye arasında bir süre bekle
            }

            // Mevcut hızı hedef hıza doğru yumuşak (smooth) bir şekilde değiştir
            speed = Mathf.Lerp(speed, targetSpeed, Time.deltaTime * 2f);
        }

        // Eğer player referansı varsa hareket et. (Hız negatifse oyuncudan uzaklaşır)
        if (player != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, speed * Time.deltaTime);

            // Eğer bir düşmanla çarpışıp üstüne çıkma durumu tetiklendiyse Y ekseninde yumuşakça yüksel
            if (climbAmount > 0)
            {
                transform.position += new Vector3(0, climbAmount * Time.deltaTime, 0);
                // Tırmanma etkisini zamanla yavaşça azalt (pürüzsüz bir bitiş için)
                climbAmount = Mathf.Lerp(climbAmount, 0f, Time.deltaTime * 5f);
            }
        }
    }

    // --- 2D Çarpışma Kontrolleri (Oyun 2D ise bunlar çalışır) ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    // --- 3D Çarpışma Kontrolleri (Oyun 3D ise bunlar çalışır) ---
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    // Hasar verme fonksiyonu (Kendi oyuncu scriptine göre burayı düzenleyebilirsin)
    private void DealDamageToPlayer(GameObject playerObj)
    {
        // Hasar verdiğinde 1 saniye boyunca -2 hızında uzaklaşma süresini başlat
        knockbackTimer = 1f;

        Debug.Log("Oyuncuya " + damage + " hasar verildi!");
        
        // --- İleride eklenecek oyuncu can azaltma kodu ---
        // ÖRNEK KULLANIM: (Oyuncu objesinde "PlayerHealth" isimli bir script olduğunu varsayarsak)
        //
        // PlayerHealth healthScript = playerObj.GetComponent<PlayerHealth>();
        // if (healthScript != null)
        // {
        //     healthScript.TakeDamage(damage); 
        // }
    }
}
