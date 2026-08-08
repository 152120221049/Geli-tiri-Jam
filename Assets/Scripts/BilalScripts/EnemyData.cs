using System;
using System.Collections.Generic;

[Serializable]
public class EnemyDataContainer
{
    public List<EnemyData> Enemies;
}

[Serializable]
public class EnemyData
{
    public int EnemyType;
    public string Name;
    public int Health;
    public int Damage;
    public float MinSpeed;
    public float MaxSpeed;
    public string Behavior;
    public float AttackCooldown;
    public string VisualPrefabPath;
}
