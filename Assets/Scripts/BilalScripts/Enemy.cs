using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Settings")]
    public float damage = 10f; // Oyuncuya verilecek hasar miktarı
    public Transform player;
    
    [Header("Debug - Read Only")]
    public float currentSpeed;

    private float targetSpeed;
    private float speedTimer;
    private float knockbackTimer = 0f;
    private float climbAmount = 0f;
    private float enemyWidth; // Düşmanın genişliğini (X eksenindeki) tutacak değişken
    private float enemyHeight; // Düşmanın yüksekliğini (Y eksenindeki) tutacak değişken

    void Start()
    {
        // Başlangıç hızı
        currentSpeed = Random.Range(4f, 10f);
        targetSpeed = currentSpeed;
        speedTimer = Random.Range(1f, 3f);

        // Düşmanın genişliğini (x) hesaplayıp kaydet (Boost formülü için)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            enemyWidth = col.bounds.size.x;
        }
        else
        {
            enemyWidth = transform.localScale.x;
        }

        // Düşmanın yüksekliğini (y) hesaplayıp kaydet
        if (col != null)
        {
            enemyHeight = col.bounds.size.y;
        }
        else
        {
            enemyHeight = transform.localScale.y;
        }

        // Player atanmamışsa otomatik bul
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    void FixedUpdate()
    {
        // 1. Hız ve Geri Tepme (Knockback) Hesaplaması
        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            currentSpeed = -2f; // Geri tepme süresince eksi hız
            targetSpeed = -2f;

            if (knockbackTimer <= 0)
            {
                currentSpeed = Random.Range(4f, 10f);
                targetSpeed = currentSpeed;
                speedTimer = Random.Range(1f, 3f);
            }
        }
        else
        {
            speedTimer -= Time.fixedDeltaTime;
            if (speedTimer <= 0)
            {
                targetSpeed = Random.Range(4f, 10f);
                speedTimer = Random.Range(1f, 3f);
            }
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * 2f);
        }

        // 2. Hareket (Movement) Uygulaması
        if (player != null)
        {
            // Oyuncuya doğru hareket et (hız eksi ise geriye gider)
            transform.position = Vector3.MoveTowards(transform.position, player.position, currentSpeed * Time.fixedDeltaTime);

            // Tırmanma (climb) etkisini uygula
            if (climbAmount > 0)
            {
                transform.position += new Vector3(0, climbAmount * Time.fixedDeltaTime, 0);
                climbAmount = Mathf.Lerp(climbAmount, 0f, Time.fixedDeltaTime * 5f);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log(collision.gameObject.tag);
            DealDamageToPlayer(collision.gameObject);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Diğer düşmanla çarpışınca üste tırmanma mantığı
        if (collision.gameObject.CompareTag("Enemy") && player != null)
        {
            // İkimizin de oyuncuya olan uzaklığını ölç (y1 ve y2)
            float y1 = Vector3.Distance(transform.position, player.position);
            float y2 = Vector3.Distance(collision.transform.position, player.position);

            // Oyuncuya daha UZAK olan (arkada kalan) üste çıkmaya hak kazansın
            bool shouldIClimb = y1 > y2;
            
            // Eğer mesafeler tamamen eşitse sonsuz döngüyü önlemek için ID'ye bak
            if (Mathf.Abs(y1 - y2) < 0.05f)
            {
                shouldIClimb = gameObject.GetInstanceID() > collision.gameObject.GetInstanceID();
            }

            if (shouldIClimb)
            {
                climbAmount = 5f; // Tırmanma gücü (yukarı çıkması için gereken itme)
                
                // Boost koşulu: |y1 - y2| >= x/2
                if (Mathf.Abs(y1 - y2) >= (enemyWidth /2f))
                {
                    if(Mathf.Abs(y1 - y2) < (enemyHeight / 2f)) // Yükseklik farkı da kontrol ediliyor
                    {
                    // Şart sağlandığında anlık bir hız (boost) ver
                    currentSpeed = 8f; 
                    targetSpeed = 8f;
                    speedTimer = 0.5f; // Bu hızda en az yarım saniye kalması için zamanlayıcıyı güncelle
                    }
                }
            }
        }
    }

    private void DealDamageToPlayer(GameObject playerObj)
    {
        knockbackTimer = 1f;
        Debug.Log("Oyuncuya " + damage + " hasar verildi!");
    }
}
