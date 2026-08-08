using UnityEngine;

namespace MemoScripts
{
    public enum CameraZoneType
    {
        AutoDetect,     // BoxCollider2D boyutuna göre otomatik karar ver
        FixedSmallZone, // Her zaman küçük bölge gibi davran (Kamera nesneye/bölge merkezine kilitlenir)
        LargeFollowZone // Her zaman büyük bölge gibi davran (Kamera oyuncuyu sınırlar içinde takip eder)
    }

    /// <summary>
    /// 2D Side-scroller için BoxCollider2D alanlarını analiz eden betik.
    /// Bölge küçükse kamerayı bu nesneye/bölgeye odaklar, büyükse oyuncuyu kutu sınırları içinde takip ettirir.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CameraZone : MonoBehaviour
    {
        [Header("Bölge Ayarları")]
        [Tooltip("Bölge tipi (Otomatik algılama veya sabit mod).")]
        [SerializeField] private CameraZoneType zoneType = CameraZoneType.AutoDetect;

        [Tooltip("Bölgenin 'küçük' sayılması için maksimum taban alanı (Genişlik * Yükseklik). Bu değerden küçükse kamera nesneye kilitlenir.")]
        [SerializeField] private float smallZoneAreaThreshold = 30.0f;

        [Tooltip("Büyük bölgede oyuncu takibi yapılırken kenarlardan bırakılacak pay/ofset.")]
        [SerializeField] private float padding = 0.5f;

        [Header("Özel Odaklama (Opsiyonel)")]
        [Tooltip("Küçük bölgedeyken kameranın odaklanacağı özel Transform. Boş bırakılırsa bu BoxCollider2D'nin merkezi kullanılır.")]
        [SerializeField] private Transform customFocusTarget;

        private BoxCollider2D boxCollider2D;

        private void Awake()
        {
            boxCollider2D = GetComponent<BoxCollider2D>();
            boxCollider2D.isTrigger = true;
        }

        /// <summary>
        /// Bu bölgenin sabit (küçük) bir bölge olup olmadığını söyler.
        /// </summary>
        public bool IsSmallZone(Camera cam = null)
        {
            if (zoneType == CameraZoneType.FixedSmallZone) return true;
            if (zoneType == CameraZoneType.LargeFollowZone) return false;

            if (boxCollider2D == null) boxCollider2D = GetComponent<BoxCollider2D>();

            Bounds bounds = boxCollider2D.bounds;

            // Kamera görüş alanı ile karşılaştırma:
            // Eğer kutu en az bir boyutta (genişlik veya yükseklik) kameradan büyükse oyuncuyu takip etmelidir.
            if (cam != null && cam.orthographic)
            {
                float camHeight = cam.orthographicSize * 2f;
                float camWidth = camHeight * cam.aspect;

                if (bounds.size.x > camWidth + 0.1f || bounds.size.y > camHeight + 0.1f)
                {
                    return false; // Takip yapılmalı (Büyük bölge)
                }

                return true; // Hem X hem Y kameradan küçük/eşitse kilitlen (Küçük bölge)
            }

            float area = bounds.size.x * bounds.size.y;
            return area <= smallZoneAreaThreshold;
        }

        /// <summary>
        /// Verilen oyuncu pozisyonu ve kameraya göre hedef pozisyonu hesaplar.
        /// </summary>
        public Vector3 GetTargetCameraPosition(Vector3 playerPosition, Camera cam, float cameraZ)
        {
            if (boxCollider2D == null) boxCollider2D = GetComponent<BoxCollider2D>();
            Bounds bounds = boxCollider2D.bounds;

            if (IsSmallZone(cam))
            {
                // Küçük bölge: Kamera tam nesnenin/kutunun merkezine kilitlenir
                Vector3 centerPos = customFocusTarget != null ? customFocusTarget.position : bounds.center;
                return new Vector3(centerPos.x, centerPos.y, cameraZ);
            }
            else
            {
                // Büyük bölge: Oyuncuyu takip eder ancak kamera görüş alanını BoxCollider2D sınırlarında tutar (Clamp)
                float vertExtent = (cam != null && cam.orthographic) ? cam.orthographicSize : 5f;
                float horizExtent = vertExtent * ((cam != null) ? cam.aspect : (16f / 9f));

                float minX = bounds.min.x + horizExtent + padding;
                float maxX = bounds.max.x - horizExtent - padding;
                float minY = bounds.min.y + vertExtent + padding;
                float maxY = bounds.max.y - vertExtent - padding;

                // Eğer kutu kameradan küçükse ortada tut
                float clampedX = (minX > maxX) ? bounds.center.x : Mathf.Clamp(playerPosition.x, minX, maxX);
                float clampedY = (minY > maxY) ? bounds.center.y : Mathf.Clamp(playerPosition.y, minY, maxY);

                return new Vector3(clampedX, clampedY, cameraZ);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
            {
                CameraController mainCam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : FindFirstObjectByType<CameraController>();
                if (mainCam != null)
                {
                    mainCam.SetCurrentZone(this);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
            {
                CameraController mainCam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : FindFirstObjectByType<CameraController>();
                if (mainCam != null)
                {
                    mainCam.RemoveZone(this);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (boxCollider2D == null) boxCollider2D = GetComponent<BoxCollider2D>();
            if (boxCollider2D == null) return;

            Gizmos.color = IsSmallZone() ? new Color(1f, 0.3f, 0.3f, 0.4f) : new Color(0.3f, 1f, 0.3f, 0.4f);
            Gizmos.DrawCube(boxCollider2D.bounds.center, boxCollider2D.bounds.size);
        }
    }
}

