using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MemoScripts
{
    /// <summary>
    /// 2D Side-scroller oyuncu hareket kontrolcüsü (Zıplamasız, Yürüme ve Koşma).
    /// Yeni Input System ve Eski Input Manager ile tam uyumludur.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterController : MonoBehaviour
    {
        [Header("Hareket Ayarları")]
        [Tooltip("Normal yürüme hızı")]
        [SerializeField] private float walkSpeed = 5.0f;

        [Tooltip("Koşma hızı")]
        [SerializeField] private float runSpeed = 9.0f;

        [Tooltip("Eski Input Manager kullanılıyorsa koşma tuşu")]
        [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

        [Header("Görsel & Bileşenler")]
        [Tooltip("Karakterin sprite renderer bileşeni (Yön çevirmek için). Boşsa otomatik aranır.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Eğer SpriteRenderer kullanmıyorsanız Transform scale ile yön çevir (Sağ için +1, Sol için -1).")]
        [SerializeField] private bool useTransformScaleFlip = false;

        [Header("Animatör Parametreleri (Opsiyonel)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParamName = "Speed";
        [SerializeField] private string isRunningParamName = "IsRunning";

        private Rigidbody2D rb;
        private float horizontalInput;

        [Header("Dash (Atılma) Ayarları")]
        [SerializeField] private float baseDashSpeed = 16.0f;
        [SerializeField] private float baseDashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 0.8f;
        [SerializeField] private float baseDashStaminaCost = 20.0f;

        private bool isDashing = false;
        private float dashTimer = 0f;
        private float nextDashTime = 0f;
        private float currentDashSpeed = 16.0f;
        private Vector2 dashDirection = Vector2.right;

        [Header("Hurtbox / Collider Ayarları")]
        [SerializeField] private Collider2D hurtboxCollider;

        private Vector2 originalBoxSize;
        private Vector2 originalBoxOffset;
        private Vector2 originalCapsuleSize;
        private Vector2 originalCapsuleOffset;
        private float originalCircleRadius;
        private Vector2 originalCircleOffset;
        private bool hasRecordedColliderDefaults = false;

        private bool isRunning;
        private bool isFacingRight = true;

        public bool IsDashing => isDashing;
        public bool IsRunning => isRunning;
        public float CurrentSpeed => isDashing ? currentDashSpeed : Mathf.Abs(horizontalInput) * (isRunning ? runSpeed : walkSpeed);

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            // Girdi okuma (Yeni ve Eski Input System destekli)
            ReadInput();

            // Yön çevirme kontrolü (Nişan alma esnasında fare konumuna göre, normalde hareket yönüne göre)
            if (PlayerEquipmentController.Instance != null && PlayerEquipmentController.Instance.IsAttackingOrAiming)
            {
                if (AimTrajectoryUI.Instance != null)
                {
                    Vector2 mouseWorld = AimTrajectoryUI.Instance.GetMouseWorldPosition();
                    if (mouseWorld.x > transform.position.x + 0.1f && !isFacingRight)
                    {
                        Flip();
                    }
                    else if (mouseWorld.x < transform.position.x - 0.1f && isFacingRight)
                    {
                        Flip();
                    }
                }
            }
            else if (horizontalInput > 0.01f && !isFacingRight)
            {
                Flip();
            }
            else if (horizontalInput < -0.01f && isFacingRight)
            {
                Flip();
            }

            // Animatör güncellemesi
            UpdateAnimator();
        }

        [Header("Envanter Etkileşimi")]
        [Tooltip("Envanter açıkken karakterin hareket etmesini engelle")]
        [SerializeField] private bool blockMovementWhenInventoryIsOpen = true;

        private void ReadInput()
        {
            horizontalInput = 0f;
            isRunning = false;

            // Envanter açıkken, açılmak için tuşa basılı tutulurken, Not okunurken veya Saldırılırken hareketi durdur
            if (blockMovementWhenInventoryIsOpen)
            {
                if ((InventoryManager.Instance != null && (InventoryManager.Instance.IsInventoryOpen || InventoryManager.Instance.IsHoldingToOpen)) ||
                    (NoteUI.Instance != null && NoteUI.Instance.IsReadingNote) ||
                    (PlayerEquipmentController.Instance != null && PlayerEquipmentController.Instance.IsAttackingOrAiming))
                {
                    return;
                }
            }

            // Dash Kontrolü (Space tuşu)
            bool dashPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                dashPressed = true;
#else
            if (Input.GetKeyDown(KeyCode.Space))
                dashPressed = true;
#endif

            if (dashPressed && !isDashing && Time.time >= nextDashTime)
            {
                TryPerformDash();
            }

#if ENABLE_INPUT_SYSTEM
            // Yeni Input System (Keyboard & Gamepad)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    horizontalInput -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    horizontalInput += 1f;

                if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                    isRunning = true;
            }

            if (Gamepad.current != null)
            {
                float stickX = Gamepad.current.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.15f)
                    horizontalInput = stickX;

                if (Gamepad.current.buttonSouth.isPressed || Gamepad.current.leftShoulder.isPressed || Gamepad.current.rightShoulder.isPressed)
                    isRunning = true;
            }
#else
            // Eski Input Manager Fallback (veya try-catch güvencesi)
            try
            {
                horizontalInput = Input.GetAxisRaw("Horizontal");
                isRunning = Input.GetKey(runKey);
            }
            catch (System.InvalidOperationException)
            {
                // Eğer Input System paket durum ayarları çakışırsa güvenli geçiş
            }
#endif

            // Koşmanın geçerli olması için hareket ediliyor olması gerekir
            isRunning = isRunning && Mathf.Abs(horizontalInput) > 0.01f;

            // Stamina kontrolü (20 saniyelik koşu vb.)
            if (isRunning && PlayerStamina.Instance != null)
            {
                if (!PlayerStamina.Instance.DrainStamina(PlayerStamina.Instance.runCostPerSecond))
                {
                    isRunning = false;
                }
            }
        }

        private void TryPerformDash()
        {
            // Zırh ağırlık oranı hesapla
            float weightPenalty = 0f;
            if (PlayerArmorSystem.Instance != null)
            {
                weightPenalty = Mathf.Max(0f, PlayerArmorSystem.Instance.CurrentAccelTime - 0.05f);
            }

            // Stamina maliyeti ağırlıkla artar (20 base + weightPenalty * 40)
            float dynamicStaminaCost = baseDashStaminaCost + (weightPenalty * 40f);

            // Stamina kontrolü
            if (PlayerStamina.Instance != null && !PlayerStamina.Instance.ConsumeStamina(dynamicStaminaCost))
            {
                Debug.LogWarning("🔋 [DASH] Yetersiz Stamina! Dash atılamadı.");
                return;
            }

            // Ağırlıkla hızı hafifçe azalt
            currentDashSpeed = Mathf.Max(8.0f, baseDashSpeed - (weightPenalty * 15f));

            // Dash yönü: Giriş yapılan yön veya bakılan yön
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                dashDirection = horizontalInput > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                dashDirection = isFacingRight ? Vector2.right : Vector2.left;
            }

            isDashing = true;
            dashTimer = baseDashDuration;
            nextDashTime = Time.time + dashCooldown;

            // i-Frames (Geçici dokunulmazlık)
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.IsInvincible = true;
            }

            // Nişan almayı iptal et
            if (PlayerEquipmentController.Instance != null)
            {
                PlayerEquipmentController.Instance.CancelAiming();
            }

            // Hurtbox'ı %75 küçült (%25 boyuta indir)
            SetHurtboxShrinkState(true);

            Debug.Log($"💨 [DASH] Dash atıldı! Hız: {currentDashSpeed:F1}, Harcanan Stamina: {dynamicStaminaCost:F1}");
        }

        private float smoothedVelocityX = 0f;

        private void FixedUpdate()
        {
            if (isDashing)
            {
                dashTimer -= Time.fixedDeltaTime;
                Vector2 vel = rb.linearVelocity;
                vel.x = dashDirection.x * currentDashSpeed;
                rb.linearVelocity = vel;

                if (dashTimer <= 0f)
                {
                    isDashing = false;
                    smoothedVelocityX = vel.x;

                    // Hurtbox'ı orijinal boyutuna döndür
                    SetHurtboxShrinkState(false);

                    if (PlayerHealth.Instance != null)
                    {
                        PlayerHealth.Instance.IsInvincible = false;
                    }
                }
                return;
            }

            // Hedef hız hesapla
            float targetSpeed = isRunning ? runSpeed : walkSpeed;
            float targetVelocityX = horizontalInput * targetSpeed;

            // Zırh ağırlık eğrisi uygula (PlayerArmorSystem varsa)
            float accelTime = 0.05f;
            float decelTime = 0.05f;

            if (PlayerArmorSystem.Instance != null)
            {
                accelTime = PlayerArmorSystem.Instance.CurrentAccelTime;
                decelTime = PlayerArmorSystem.Instance.CurrentDecelTime;
            }

            // Hızlanma mı yavaşlama mı?
            float smoothTime;
            if (Mathf.Abs(targetVelocityX) > Mathf.Abs(smoothedVelocityX))
                smoothTime = Mathf.Max(accelTime, 0.01f); // Hızlanma
            else
                smoothTime = Mathf.Max(decelTime, 0.01f); // Yavaşlama

            // Yumuşak geçiş (Lerp)
            smoothedVelocityX = Mathf.Lerp(smoothedVelocityX, targetVelocityX, Time.fixedDeltaTime / smoothTime);

            // Çok küçük değerleri sıfırla (titreşimi önle)
            if (Mathf.Abs(smoothedVelocityX) < 0.01f && Mathf.Abs(targetVelocityX) < 0.01f)
                smoothedVelocityX = 0f;

            Vector2 velocity = rb.linearVelocity;
            velocity.x = smoothedVelocityX;
            rb.linearVelocity = velocity;
        }

        private void Flip()
        {
            isFacingRight = !isFacingRight;

            if (useTransformScaleFlip)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (isFacingRight ? 1f : -1f);
                transform.localScale = scale;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !isFacingRight;
            }

            if (PlayerEquipmentController.Instance != null)
            {
                PlayerEquipmentController.Instance.UpdateFlipDirection(isFacingRight);
            }
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;

            if (!string.IsNullOrEmpty(speedParamName))
            {
                animator.SetFloat(speedParamName, Mathf.Abs(horizontalInput));
            }

            if (!string.IsNullOrEmpty(isRunningParamName))
            {
                animator.SetBool(isRunningParamName, isRunning);
            }
        }

        private void RecordColliderDefaults()
        {
            if (hasRecordedColliderDefaults) return;
            if (hurtboxCollider == null) hurtboxCollider = GetComponent<Collider2D>();
            if (hurtboxCollider == null) hurtboxCollider = GetComponentInChildren<Collider2D>();

            if (hurtboxCollider != null)
            {
                if (hurtboxCollider is BoxCollider2D box)
                {
                    originalBoxSize = box.size;
                    originalBoxOffset = box.offset;
                }
                else if (hurtboxCollider is CapsuleCollider2D cap)
                {
                    originalCapsuleSize = cap.size;
                    originalCapsuleOffset = cap.offset;
                }
                else if (hurtboxCollider is CircleCollider2D circ)
                {
                    originalCircleRadius = circ.radius;
                    originalCircleOffset = circ.offset;
                }
                hasRecordedColliderDefaults = true;
            }
        }

        private void SetHurtboxShrinkState(bool isShrunk)
        {
            RecordColliderDefaults();
            if (hurtboxCollider == null) return;

            float factor = isShrunk ? 0.25f : 1.0f; // %75 küçültme -> %25 boyuta düşürme

            if (hurtboxCollider is BoxCollider2D box)
            {
                box.size = new Vector2(originalBoxSize.x, originalBoxSize.y * factor);
                box.offset = new Vector2(originalBoxOffset.x, originalBoxOffset.y * factor);
            }
            else if (hurtboxCollider is CapsuleCollider2D cap)
            {
                cap.size = new Vector2(originalCapsuleSize.x, originalCapsuleSize.y * factor);
                cap.offset = new Vector2(originalCapsuleOffset.x, originalCapsuleOffset.y * factor);
            }
            else if (hurtboxCollider is CircleCollider2D circ)
            {
                circ.radius = originalCircleRadius * factor;
                circ.offset = new Vector2(originalCircleOffset.x, originalCircleOffset.y * factor);
            }
        }
    }
}


