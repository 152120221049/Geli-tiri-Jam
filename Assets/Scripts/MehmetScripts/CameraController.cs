using System.Collections.Generic;
using UnityEngine;

namespace MemoScripts
{
    /// <summary>
    /// 2D Side-scroller Akıllı Kamera Kontrolcüsü.
    /// Aktif CameraZone (BoxCollider2D) duruma göre oyuncuyu takip eder veya nesneye kilitlenir.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Hedef & Takip Ayarları")]
        [Tooltip("Kameranın takip edeceği oyuncu. Boş bırakılırsa CharacterController veya 'Player' tag'li obje bulunur.")]
        [SerializeField] private Transform playerTarget;

        [Tooltip("Kamera yumuşatma süresi (Daha küçük değer = Daha hızlı takip).")]
        [SerializeField] private float smoothTime = 0.25f;

        [Tooltip("Oyuncu bir bölgede değilken kameranın ofseti.")]
        [SerializeField] private Vector2 defaultOffset = Vector2.zero;

        [Header("Aktif Bölge (Zone) Bilgisi")]
        [SerializeField] private CameraZone currentZone;

        private Camera cam;
        private Vector3 velocity = Vector3.zero;
        private List<CameraZone> activeZones = new List<CameraZone>();

        public CameraZone CurrentZone => currentZone;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            FindPlayerIfNull();
        }

        private void LateUpdate()
        {
            if (playerTarget == null)
            {
                FindPlayerIfNull();
                if (playerTarget == null) return;
            }

            Vector3 targetPosition;

            if (currentZone != null)
            {
                // Bölgenin hesapladığı hedef kamera pozisyonunu al (Küçük bölgeyse kilitlenir, büyükse sınırlar içinde takip eder)
                targetPosition = currentZone.GetTargetCameraPosition(playerTarget.position, cam, transform.position.z);
            }
            else
            {
                // Herhangi bir bölgede değilse direkt oyuncuyu takip et
                targetPosition = new Vector3(playerTarget.position.x + defaultOffset.x, playerTarget.position.y + defaultOffset.y, transform.position.z);
            }

            // Pozisyonu yumuşakça (SmoothDamp) hedefe kaydır
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }

        /// <summary>
        /// Karakter bir kamera bölgesine girdiğinde çağrılır.
        /// </summary>
        public void SetCurrentZone(CameraZone zone)
        {
            if (!activeZones.Contains(zone))
            {
                activeZones.Add(zone);
            }
            currentZone = zone;
        }

        /// <summary>
        /// Karakter bir kamera bölgesinden çıktığında çağrılır.
        /// </summary>
        public void RemoveZone(CameraZone zone)
        {
            if (activeZones.Contains(zone))
            {
                activeZones.Remove(zone);
            }

            // En son girilen aktif bölgeye geç, bölge kalmadıysa null yap
            if (activeZones.Count > 0)
            {
                currentZone = activeZones[activeZones.Count - 1];
            }
            else if (currentZone == zone)
            {
                currentZone = null;
            }
        }

        private void FindPlayerIfNull()
        {
            if (playerTarget != null) return;

            CharacterController playerCC = FindFirstObjectByType<CharacterController>();
            if (playerCC != null)
            {
                playerTarget = playerCC.transform;
                return;
            }

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }
    }
}
