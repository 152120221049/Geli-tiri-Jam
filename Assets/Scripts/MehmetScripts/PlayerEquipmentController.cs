using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Oyuncunun elindeki/aktif Hotbar slotundaki ekipman, silah, fırlatılabilir eşya ve büyü kullanımını yönetir.
/// - Sol Tık: Yakın dövüş savurması (Melee), Yay ile ok atma, İksir içme, Büyü yapma
/// - Sağ Tık Basılı Tut → Bırak: Nişan çizgisi (AimTrajectoryUI) ile silah / şişe / taş / mum fırlatma
/// </summary>
public class PlayerEquipmentController : MonoBehaviour
{
    private static PlayerEquipmentController _instance;
    public static PlayerEquipmentController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerEquipmentController>();
                if (_instance == null)
                {
                    GameObject pObj = GameObject.FindGameObjectWithTag("Player");
                    if (pObj == null)
                    {
                        var cc = FindObjectOfType<MemoScripts.CharacterController>();
                        if (cc != null) pObj = cc.gameObject;
                    }

                    if (pObj != null)
                    {
                        _instance = pObj.AddComponent<PlayerEquipmentController>();
                    }
                    else
                    {
                        GameObject go = new GameObject("PlayerEquipmentController");
                        _instance = go.AddComponent<PlayerEquipmentController>();
                    }
                }
            }
            return _instance;
        }
    }

    [Header("Atış & Saldırı Noktası")]
    [SerializeField] private Transform throwSpawnPoint;

    [Header("Dövüş Ayarları")]
    [SerializeField] private LayerMask enemyLayer = ~0;

    /// <summary>Atışların, büyülerin ve vuruş hitboxlarının çıkış noktası (Transform atanmadıysa player position).</summary>
    public Vector2 GetAttackOrigin()
    {
        if (throwSpawnPoint != null) return throwSpawnPoint.position;
        return transform.position;
    }

    // Bekleme süresi (Cooldown)
    private float lastAttackTime;
    private bool isAimingToThrow = false;
    private InventoryItem aimingItem = null;

    // Sadak (Quiver) verileri — Her Sadak InventoryItem'ı için runtime QuiverData tutar
    private Dictionary<InventoryItem, QuiverData> quiverDataMap = new Dictionary<InventoryItem, QuiverData>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Update()
    {
        // Envanter açıkken veya Not okunurken aksiyon yapılmaz
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInventoryOpen)
        {
            if (isAimingToThrow) CancelAiming();
            return;
        }

        if (NoteUI.Instance != null && NoteUI.Instance.IsReadingNote)
        {
            if (isAimingToThrow) CancelAiming();
            return;
        }

        HandleSolTikActions();
        HandleSagTikThrowAiming();
    }

    // ═══════════════════════════════════════════
    //  SOL TIK AKSİYONLARI (Melee, Yay, İksir, Büyü)
    // ═══════════════════════════════════════════

    private void HandleSolTikActions()
    {
        // UI üzerinde tıklama varsa işleme
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        bool leftClicked = false;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            leftClicked = true;
#else
        if (Input.GetMouseButtonDown(0))
            leftClicked = true;
#endif

        if (!leftClicked) return;

        InventoryItem activeItem = InventoryManager.Instance != null ? InventoryManager.Instance.GetActiveHotbarItem() : null;
        if (activeItem == null || activeItem.itemData == null) return;

        ItemSO data = activeItem.itemData;

        // 6) Görev Eşyası (Taş, Mum vb.)
        if (data.itemType == ItemType.QuestItem)
        {
            if (data.isThrowable)
            {
                ExecuteThrow(activeItem);
            }
            return;
        }

        // 1) Yay kullanımı (Bow)
        if (data.itemType == ItemType.WeaponTool && data.itemName.Contains("Yay"))
        {
            TryShootBow(activeItem);
            return;
        }

        // 2) Yakın Dövüş (Melee)
        if (data.canMeleeAttack && Time.time >= lastAttackTime + data.attackCooldown)
        {
            PerformMeleeAttack(activeItem);
            return;
        }

        // 3) Can İksiri (İç + Boş Şişeyi Fırlat)
        if (data.itemType == ItemType.Consumable && data.itemName.Contains("Can İksiri"))
        {
            DrinkHealthPotion(activeItem);
            return;
        }

        // 4) Patlayıcı İksir / Şişe (Doğrudan Sol Tıkla da Atılabilir)
        if (data.itemType == ItemType.ThrowableFlask)
        {
            ThrowFlask(activeItem);
            return;
        }

        // 5) Büyü Parşömeni (Fireball / Lightning)
        if (data.itemType == ItemType.SpellScroll)
        {
            CastSpell(activeItem);
            return;
        }
    }

    /// <summary>Yakın dövüş savurması (Kılıç, Sopa, Hançer, Asa, Büyük Kılıç).</summary>
    private void PerformMeleeAttack(InventoryItem item)
    {
        // Stamina Kontrolü
        if (PlayerStamina.Instance != null && !PlayerStamina.Instance.ConsumeStamina(item.itemData.staminaCost))
        {
            return;
        }

        lastAttackTime = Time.time;
        ItemSO data = item.itemData;

        Vector2 origin = GetAttackOrigin();
        Vector2 mouseWorld = AimTrajectoryUI.Instance != null ? AimTrajectoryUI.Instance.GetMouseWorldPosition() : origin + Vector2.right;
        Vector2 dir = (mouseWorld - origin).normalized;

        Debug.Log($"⚔️ [MELEE] {data.itemName} ile savurma yapıldı! Hasar: {data.meleeDamage}, Menzil: {data.meleeRange}m");

        // Farenin baktığı koniye (arc) düşen düşmanları bul
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, data.meleeRange, enemyLayer);
        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;
            Vector2 toEnemy = (Vector2)col.transform.position - origin;
            float angle = Vector2.Angle(dir, toEnemy);

            if (angle <= data.swingArcAngle / 2f)
            {
                Debug.Log($"⚔️ [MELEE VURUS] {col.gameObject.name} vuruldu! Hasar: {data.meleeDamage}");
            }
        }

        // Dayanıklılık düşür
        if (item.itemData.HasDurability)
        {
            item.Use();
            if (item.IsBroken)
            {
                Debug.LogWarning($"💥 {item.itemData.itemName} kırıldı!");
                InventoryManager.Instance.RemoveItemCompletely(item);
            }
            else
            {
                InventoryManager.Instance.NotifyInventoryChanged();
            }
        }
    }

    /// <summary>Yay ile Sadak'tan ok çekip ateşle.</summary>
    private void TryShootBow(InventoryItem bowItem)
    {
        // Envanterdeki Sadak'ı (Quiver) bul
        InventoryItem quiverItem = FindQuiverInInventory();
        if (quiverItem == null)
        {
            Debug.LogWarning("🏹 [YAY] Envanterde Sadak (Quiver) yok!");
            return;
        }

        QuiverData qData = GetQuiverData(quiverItem);
        if (qData.IsEmpty)
        {
            Debug.LogWarning("🏹 [YAY] Sadak boş! Ok doldurmalısınız.");
            return;
        }

        // Stamina Kontrolü
        if (PlayerStamina.Instance != null && !PlayerStamina.Instance.ConsumeStamina(bowItem.itemData.staminaCost))
        {
            return;
        }

        ItemSO arrowType = qData.ConsumeArrow();
        if (arrowType == null)
        {
            Debug.LogWarning("🏹 Sadak boş! Ok atılamadı.");
            return;
        }

        Vector2 origin = GetAttackOrigin();
        Vector2 mouseWorld = AimTrajectoryUI.Instance != null ? AimTrajectoryUI.Instance.GetMouseWorldPosition() : origin + Vector2.right;
        Vector2 dir = (mouseWorld - origin).normalized;

        bool isFire = arrowType.itemName.Contains("Ateşli");

        GameObject arrowObj = new GameObject($"ArrowProjectile_{arrowType.itemName}");
        arrowObj.transform.position = origin;

        // Görsel Sprite
        SpriteRenderer sr = arrowObj.AddComponent<SpriteRenderer>();
        sr.sprite = arrowType.icon;
        sr.sortingOrder = 10;

        BoxCollider2D col = arrowObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.2f);

        ArrowProjectile arrowProj = arrowObj.AddComponent<ArrowProjectile>();
        arrowProj.Launch(dir, 16f, 15f, isFire);

        // Yay dayanıklılığı düşür
        if (bowItem.itemData.HasDurability)
        {
            bowItem.Use();
            if (bowItem.IsBroken)
            {
                Debug.LogWarning($"💥 {bowItem.itemData.itemName} kırıldı!");
                InventoryManager.Instance.RemoveItemCompletely(bowItem);
            }
            else
            {
                InventoryManager.Instance.NotifyInventoryChanged();
            }
        }
    }

    /// <summary>Can İksiri iç (+HP) ve ardından boş şişeyi fırlat.</summary>
    private void DrinkHealthPotion(InventoryItem item)
    {
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.Heal(50f);
        }

        // İksir adetini 1 eksilt
        item.Use();

        // Elinde kalan boş şişeyi fırlat
        Vector2 origin = GetAttackOrigin();
        Vector2 mouseWorld = AimTrajectoryUI.Instance != null ? AimTrajectoryUI.Instance.GetMouseWorldPosition() : origin + Vector2.right;
        Vector2 dir = (mouseWorld - origin).normalized;

        GameObject bottleObj = new GameObject("ThrowableEmptyBottle");
        bottleObj.transform.position = origin;

        SpriteRenderer sr = bottleObj.AddComponent<SpriteRenderer>();
        sr.sprite = item.itemData.icon;
        sr.sortingOrder = 10;

        CircleCollider2D col = bottleObj.AddComponent<CircleCollider2D>();
        col.radius = 0.2f;

        ThrowableBottle bottle = bottleObj.AddComponent<ThrowableBottle>();
        bottle.Launch(dir, 10f, item.itemData);

        if (item.IsEmpty)
        {
            InventoryManager.Instance.RemoveItemCompletely(item);
        }
        else
        {
            InventoryManager.Instance.NotifyInventoryChanged();
        }
    }

    /// <summary>Patlayıcı İksir / Şişe fırlat.</summary>
    private void ThrowFlask(InventoryItem item)
    {
        Vector2 origin = GetAttackOrigin();
        Vector2 mouseWorld = AimTrajectoryUI.Instance != null ? AimTrajectoryUI.Instance.GetMouseWorldPosition() : origin + Vector2.right;
        Vector2 dir = (mouseWorld - origin).normalized;

        GameObject flaskObj = new GameObject($"Flask_{item.itemData.itemName}");
        flaskObj.transform.position = origin;

        SpriteRenderer sr = flaskObj.AddComponent<SpriteRenderer>();
        sr.sprite = item.itemData.icon;
        sr.sortingOrder = 5;

        CircleCollider2D col = flaskObj.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;

        ThrowableBottle bottle = flaskObj.AddComponent<ThrowableBottle>();
        bottle.Launch(dir, item.itemData.throwForce > 0 ? item.itemData.throwForce : 12f, item.itemData);

        item.Use();
        if (item.IsEmpty)
        {
            InventoryManager.Instance.RemoveItemCompletely(item);
        }
        else
        {
            InventoryManager.Instance.NotifyInventoryChanged();
        }
    }

    /// <summary>Büyü Parşömeni (Fireball / Lightning) oku ve büyü fırlat.</summary>
    private void CastSpell(InventoryItem item)
    {
        // Stamina Kontrolü
        if (PlayerStamina.Instance != null && !PlayerStamina.Instance.ConsumeStamina(item.itemData.staminaCost))
            return;
        ItemSO data = item.itemData;
        Vector2 origin = GetAttackOrigin();
        Vector2 mouseWorld = AimTrajectoryUI.Instance != null ? AimTrajectoryUI.Instance.GetMouseWorldPosition() : origin + Vector2.right;
        Vector2 dir = (mouseWorld - origin).normalized;

        SpellProjectile.SpellType st = data.itemName.Contains("Lightning")
            ? SpellProjectile.SpellType.Lightning
            : SpellProjectile.SpellType.Fireball;

        GameObject spellObj = new GameObject($"Spell_{st}");
        spellObj.transform.position = origin;

        CircleCollider2D col = spellObj.AddComponent<CircleCollider2D>();
        col.radius = 0.25f;

        SpellProjectile spell = spellObj.AddComponent<SpellProjectile>();
        spell.Launch(dir, st, data.spellDamage > 0 ? data.spellDamage : 50f, data.spellAoeRadius > 0 ? data.spellAoeRadius : 1.8f);

        item.Use();
        if (item.IsEmpty)
        {
            InventoryManager.Instance.RemoveItemCompletely(item);
        }
        else
        {
            InventoryManager.Instance.NotifyInventoryChanged();
        }
    }

    // ═══════════════════════════════════════════
    //  SAĞ TIK NİŞAN ALMA & FIRLATMA (Throwing)
    // ═══════════════════════════════════════════

    private void HandleSagTikThrowAiming()
    {
        bool rightHold = false;
        bool rightReleased = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            rightHold = Mouse.current.rightButton.isPressed;
            rightReleased = Mouse.current.rightButton.wasReleasedThisFrame;
        }
#else
        rightHold = Input.GetMouseButton(1);
        rightReleased = Input.GetMouseButtonUp(1);
#endif

        InventoryItem activeItem = InventoryManager.Instance != null ? InventoryManager.Instance.GetActiveHotbarItem() : null;

        if (rightHold && IsAimableItem(activeItem))
        {
            if (!isAimingToThrow)
            {
                isAimingToThrow = true;
                aimingItem = activeItem;
                if (AimTrajectoryUI.Instance != null)
                {
                    ThrowStyle style = GetEffectiveThrowStyle(activeItem.itemData);
                    float force = activeItem.itemData.throwForce > 0 ? activeItem.itemData.throwForce : 10f;
                    AimTrajectoryUI.Instance.BeginAiming(style, force, GetAttackOrigin());
                }
            }

            if (isAimingToThrow && AimTrajectoryUI.Instance != null)
            {
                AimTrajectoryUI.Instance.UpdateAiming(GetAttackOrigin());
            }
        }

        if (rightReleased && isAimingToThrow)
        {
            ExecuteThrow(aimingItem);
            CancelAiming();
        }
    }

    private bool IsAimableItem(InventoryItem item)
    {
        if (item == null || item.itemData == null) return false;
        ItemSO d = item.itemData;

        if (d.itemName.Contains("Yay") || d.itemType == ItemType.WeaponTool) return true;
        if (d.isThrowable) return true;
        if (d.itemType == ItemType.ThrowableFlask) return true;
        if (d.itemType == ItemType.SpellScroll) return true;
        if (d.itemType == ItemType.Consumable) return true;
        if (d.itemType == ItemType.QuestItem) return true;

        return false;
    }

    private ThrowStyle GetEffectiveThrowStyle(ItemSO data)
    {
        if (data.itemName.Contains("Yay") || data.itemName.Contains("Hançer") || data.itemType == ItemType.SpellScroll)
            return ThrowStyle.StraightLine;

        if (data.throwStyle != ThrowStyle.None) return data.throwStyle;

        return ThrowStyle.Arc;
    }

    private void ExecuteThrow(InventoryItem item)
    {
        if (item == null || item.itemData == null) return;
        ItemSO data = item.itemData;

        // Stamina Kontrolü
        if (PlayerStamina.Instance != null && !PlayerStamina.Instance.ConsumeStamina(data.staminaCost))
            return;

        Vector2 origin = GetAttackOrigin();

        // 0) Yay Atışı (Sadak'tan ok ateşler)
        if (data.itemName.Contains("Yay"))
        {
            TryShootBow(item);
            return;
        }

        Vector2 dir = AimTrajectoryUI.Instance != null ? AimTrajectoryUI.Instance.GetAimDirection(origin) : Vector2.right;

        // 1) Silah Fırlatma (Hançer, Kılıç, Küçük Sopa)
        if (data.itemType == ItemType.WeaponTool)
        {
            int remainingDurability = item.currentDurability;
            if (item.currentDurability != -1) // Sonsuz değilse
            {
                remainingDurability -= data.throwDurabilityCost;
                if (remainingDurability <= 0)
                {
                    Debug.LogWarning($"💥 {data.itemName} fırlatılırken parçalandı!");
                    item.currentStack--;
                    if (item.currentStack <= 0) InventoryManager.Instance.RemoveItemCompletely(item);
                    else InventoryManager.Instance.NotifyInventoryChanged();
                    return;
                }
            }

            GameObject thrownObj = new GameObject($"Thrown_{data.itemName}");
            thrownObj.transform.position = origin;

            SpriteRenderer sr = thrownObj.AddComponent<SpriteRenderer>();
            sr.sprite = data.icon;
            sr.sortingOrder = 10;

            BoxCollider2D col = thrownObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.5f, 0.5f);

            ThrownWeapon weapon = thrownObj.AddComponent<ThrownWeapon>();
            weapon.Launch(dir, data.throwForce, data, remainingDurability, data.throwStyle);

            // Envanterden 1 adet eksilt (Stackli fırlatma bıçakları vb. için)
            item.currentStack--;
            if (item.currentStack <= 0)
            {
                InventoryManager.Instance.RemoveItemCompletely(item);
            }
            else
            {
                InventoryManager.Instance.NotifyInventoryChanged();
            }
        }
        // 2) Şişe / İksir Fırlatma
        else if (data.itemType == ItemType.ThrowableFlask || data.itemType == ItemType.Consumable)
        {
            ThrowFlask(item);
        }
        // 3) Mum / Taş (Görev Eşyaları)
        else if (data.itemType == ItemType.QuestItem)
        {
            GameObject questObj = new GameObject($"Thrown_{data.itemName}");
            questObj.transform.position = origin;

            SpriteRenderer sr = questObj.AddComponent<SpriteRenderer>();
            sr.sprite = data.icon;
            sr.sortingOrder = 10;

            CircleCollider2D col = questObj.AddComponent<CircleCollider2D>();
            col.radius = 0.2f;

            ThrownWeapon weapon = questObj.AddComponent<ThrownWeapon>();
            weapon.Launch(dir, data.throwForce, data, 1, data.throwStyle);

            item.Use();
            if (item.IsEmpty)
            {
                InventoryManager.Instance.RemoveItemCompletely(item);
            }
            else
            {
                InventoryManager.Instance.NotifyInventoryChanged();
            }
        }
    }

    private void CancelAiming()
    {
        isAimingToThrow = false;
        aimingItem = null;
        if (AimTrajectoryUI.Instance != null)
        {
            AimTrajectoryUI.Instance.StopAiming();
        }
    }

    // ═══════════════════════════════════════════
    //  QUIVER & ARROW HELPERS
    // ═══════════════════════════════════════════

    public QuiverData GetQuiverData(InventoryItem item)
    {
        if (item == null) return null;
        if (!quiverDataMap.TryGetValue(item, out var qData))
        {
            qData = new QuiverData();
            quiverDataMap[item] = qData;
        }
        return qData;
    }

    private InventoryItem FindQuiverInInventory()
    {
        if (InventoryManager.Instance == null) return null;

        foreach (var item in InventoryManager.Instance.HotbarGrid.GetAllItems())
        {
            if (item.itemData != null && item.itemData.itemType == ItemType.Quiver)
                return item;
        }

        foreach (var item in InventoryManager.Instance.InventoryGrid.GetAllItems())
        {
            if (item.itemData != null && item.itemData.itemType == ItemType.Quiver)
                return item;
        }

        return null;
    }
}
