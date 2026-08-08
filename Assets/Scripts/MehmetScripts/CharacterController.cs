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
        private bool isRunning;
        private bool isFacingRight = true;

        public bool IsRunning => isRunning;
        public float CurrentSpeed => Mathf.Abs(horizontalInput) * (isRunning ? runSpeed : walkSpeed);

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

            // Yön çevirme kontrolü
            if (horizontalInput > 0.01f && !isFacingRight)
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

            // Envanter açıkken, açılmak için tuşa basılı tutulurken veya Not okunurken hareketi durdur
            if (blockMovementWhenInventoryIsOpen)
            {
                if ((InventoryManager.Instance != null && (InventoryManager.Instance.IsInventoryOpen || InventoryManager.Instance.IsHoldingToOpen)) ||
                    (NoteUI.Instance != null && NoteUI.Instance.IsReadingNote))
                {
                    return;
                }
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
        }

        private float smoothedVelocityX = 0f;

        private void FixedUpdate()
        {
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
                scale.x *= -1;
                transform.localScale = scale;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !isFacingRight;
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
    }
}


