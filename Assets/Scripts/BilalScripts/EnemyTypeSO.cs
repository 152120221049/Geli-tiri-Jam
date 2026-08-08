using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnemyType", menuName = "ScriptableObjects/EnemyTypeSO", order = 1)]
public class EnemyTypeSO : ScriptableObject
{
    public int enemyType = 1;
    public string enemyName = "Yeni Dusman";
    public int health = 100;
    public float damage = 10f;
    public float minSpeed = 4f;
    public float maxSpeed = 10f;
    
    [Header("Type 3 (Yenilmez Boss) Settings")]
    public float attackCooldown = 5f;
    public float attackDistance = 3f; // Önündeki alan (saldırı menzili)

    [Header("Animations")]
    [Tooltip("Sırasıyla: 0: Idle, 1: Run, 2: Attack, 3: TakeDamage, 4: Die")]
    public List<string> animationTriggers = new List<string>();
}
