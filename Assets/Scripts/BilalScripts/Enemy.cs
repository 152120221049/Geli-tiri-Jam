using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    [Header("Settings")]
    public int enemyType = 1;
    public int health = 100;
    public float damage = 10f;
    public Transform player;

    [Header("Movement Settings")]
    public float minSpeed = 4f;
    public float maxSpeed = 10f;
    
    [Header("Type 3 (Yenilmez Boss) Settings")]
    public float attackCooldown = 5f;
    public float attackDistance = 3f; // Önündeki alan (saldırı menzili)

    [Header("Animations (Inspector'dan Ayarla)")]
    public Animator animator;
    [Tooltip("Sırasıyla: 0: Idle, 1: Run, 2: Attack, 3: TakeDamage, 4: Die")]
    public List<string> animationTriggers = new List<string>();

    [Header("Debug - Read Only")]
    public float currentSpeed;

    private float targetSpeed;
    private float speedTimer;
    private float knockbackTimer = 0f;
    private float climbAmount = 0f;
    private float enemyWidth;
    private float enemyHeight;
    private float lastAttackTime = 0f;

    void Start()
    {
        currentSpeed = Random.Range(minSpeed, maxSpeed);
        targetSpeed = currentSpeed;
        speedTimer = Random.Range(1f, 3f);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            enemyWidth = col.bounds.size.x;
            enemyHeight = col.bounds.size.y;
        }
        else
        {
            enemyWidth = transform.localScale.x;
            enemyHeight = transform.localScale.y;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        if (enemyType == 1 || enemyType == 2)
        {
            PlayAnimation(1); // Run
        }
        else if (enemyType == 3)
        {
            PlayAnimation(0); // Idle
        }
    }

    // JSON'dan gelen verileri bu fonksiyona atayabilirsin (Enemy Spawner yaparsan)
    public void Initialize(EnemyData data)
    {
        enemyType = data.EnemyType;
        health = data.Health;
        damage = data.Damage;
        minSpeed = data.MinSpeed;
        maxSpeed = data.MaxSpeed;
        
        if (enemyType == 3)
        {
            attackCooldown = data.AttackCooldown;
        }

        currentSpeed = Random.Range(minSpeed, maxSpeed);
        targetSpeed = currentSpeed;
        
        gameObject.name = data.Name;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        if (enemyType == 1 || enemyType == 2)
        {
            HandleMovementAndKnockback();
        }
        else if (enemyType == 3)
        {
            HandleType3Behavior();
        }
    }

    private void HandleMovementAndKnockback()
    {
        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            currentSpeed = -2f;
            targetSpeed = -2f;

            if (knockbackTimer <= 0)
            {
                currentSpeed = Random.Range(minSpeed, maxSpeed);
                targetSpeed = currentSpeed;
                speedTimer = Random.Range(1f, 3f);
                PlayAnimation(1); // Run animasyonuna dön
            }
        }
        else
        {
            speedTimer -= Time.fixedDeltaTime;
            if (speedTimer <= 0)
            {
                targetSpeed = Random.Range(minSpeed, maxSpeed);
                speedTimer = Random.Range(1f, 3f);
            }
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * 2f);
        }

        transform.position = Vector3.MoveTowards(transform.position, player.position, currentSpeed * Time.fixedDeltaTime);

        if (climbAmount > 0)
        {
            transform.position += new Vector3(0, climbAmount * Time.fixedDeltaTime, 0);
            climbAmount = Mathf.Lerp(climbAmount, 0f, Time.fixedDeltaTime * 5f);
        }
    }

    private void HandleType3Behavior()
    {
        // Sadece mesafe ile oyuncunun önünde olup olmadığını ölçüyoruz
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= attackDistance)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                DealDamageToPlayer(player.gameObject);
                PlayAnimation(2); // Attack
            }
            else
            {
                PlayAnimation(0); // Idle
            }
        }
        else
        {
            PlayAnimation(0); // Idle
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (enemyType == 1 || enemyType == 2)
            {
                DealDamageToPlayer(collision.gameObject);
                PlayAnimation(2); // Attack
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 1 numara minik canavarlar üst üste binebilir
        if (enemyType == 1 && collision.gameObject.CompareTag("Enemy") && player != null)
        {
            Enemy otherEnemy = collision.gameObject.GetComponent<Enemy>();
            if (otherEnemy != null && otherEnemy.enemyType == 1)
            {
                float y1 = Vector3.Distance(transform.position, player.position);
                float y2 = Vector3.Distance(collision.transform.position, player.position);

                bool shouldIClimb = y1 > y2;
                
                if (Mathf.Abs(y1 - y2) < 0.05f)
                {
                    shouldIClimb = gameObject.GetInstanceID() > collision.gameObject.GetInstanceID();
                }

                if (shouldIClimb)
                {
                    climbAmount = 5f; 
                    
                    if (Mathf.Abs(y1 - y2) >= (enemyWidth / 2f))
                    {
                        if(Mathf.Abs(y1 - y2) < (enemyHeight / 2f)) 
                        {
                            currentSpeed = 8f; 
                            targetSpeed = 8f;
                            speedTimer = 0.5f; 
                        }
                    }
                }
            }
        }
    }

    private void DealDamageToPlayer(GameObject playerObj)
    {
        if (enemyType != 3) 
        {
            knockbackTimer = 1f; // Yenilmez bossta geri tepme yok
        }
        
        Debug.Log(gameObject.name + " Oyuncuya " + damage + " hasar verdi!");
    }

    public void TakeDamage(int damageAmount)
    {
        // 3 numara yenilmez boss can değeri önemsiz ölmez
        if (enemyType == 3)
        {
            Debug.Log("Yenilmez Boss hasar almaz!");
            return; 
        }

        health -= damageAmount;
        Debug.Log(gameObject.name + " " + damageAmount + " hasar aldı! Kalan can: " + health);
        
        PlayAnimation(3); // TakeDamage animasyonu

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " öldü!");
        PlayAnimation(4); // Die animasyonu
        Destroy(gameObject, 0.5f); // Animasyonun oynaması için biraz bekle
    }

    // Animasyonları listeden çeken ve null değilse oynatan fonksiyon
    public void PlayAnimation(int index)
    {
        if (animator == null) return;
        
        if (animationTriggers != null && index >= 0 && index < animationTriggers.Count)
        {
            string animName = animationTriggers[index];
            if (!string.IsNullOrEmpty(animName))
            {
                animator.Play(animName); // Animasyonu ismiyle oynat
            }
        }
    }
}
