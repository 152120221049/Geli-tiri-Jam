using UnityEngine;

/// <summary>
/// Nişan alma ve yörünge çizgisi sistemi.
/// Oyuncu aktif Hotbar eşyasını kullanırken:
/// - Fırlatılabilir eşyalar için parabolik kavis (Arc) çizer
/// - Oklar ve büyüler için düz çizgi (Straight) çizer
/// - Crosshair fare konumunda gösterilir
/// </summary>
public class AimTrajectoryUI : MonoBehaviour
{
    private static AimTrajectoryUI _instance;
    public static AimTrajectoryUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AimTrajectoryUI>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AimTrajectoryUI");
                    _instance = go.AddComponent<AimTrajectoryUI>();
                }
            }
            return _instance;
        }
    }

    [Header("Ayarlar")]
    [SerializeField] private int trajectoryPointCount = 30;
    [SerializeField] private float trajectoryTimeStep = 0.05f;
    [SerializeField] private float straightLineMaxRange = 10f;

    [Header("Renk")]
    [SerializeField] private Color arcColor = new Color(1f, 0.9f, 0.3f, 0.7f);
    [SerializeField] private Color straightColor = new Color(0.3f, 0.9f, 1f, 0.7f);

    [Header("Crosshair")]
    [SerializeField] private Sprite crosshairSprite;
    [SerializeField] private Color crosshairColor = Color.white;
    [SerializeField] private float crosshairSize = 0.3f;

    private LineRenderer lineRenderer;
    private GameObject crosshairObj;
    private SpriteRenderer crosshairSR;
    private Camera mainCam;

    private bool isAiming = false;
    private ThrowStyle currentStyle = ThrowStyle.None;
    private float currentThrowForce = 10f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        mainCam = Camera.main;
        SetupLineRenderer();
        SetupCrosshair();
        HideAll();
    }

    private void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.08f;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingOrder = 500;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null) shader = Shader.Find("UI/Default");

        lineRenderer.material = new Material(shader);
        lineRenderer.startColor = arcColor;
        lineRenderer.endColor = new Color(arcColor.r, arcColor.g, arcColor.b, 0.3f);
    }

    private void SetupCrosshair()
    {
        crosshairObj = new GameObject("AimCrosshair");
        crosshairSR = crosshairObj.AddComponent<SpriteRenderer>();
        crosshairSR.sortingOrder = 501;
        crosshairSR.color = crosshairColor;

        if (crosshairSprite != null)
        {
            crosshairSR.sprite = crosshairSprite;
        }
        else
        {
            // Varsayılan artı işareti
            crosshairSR.sprite = CreateCrosshairSprite();
        }

        crosshairObj.transform.localScale = Vector3.one * crosshairSize;
        crosshairObj.SetActive(false);
    }

    private Sprite CreateCrosshairSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        int center = size / 2;
        int thickness = 1;
        int armLength = 10;

        // Yatay çizgi
        for (int x = center - armLength; x <= center + armLength; x++)
        {
            for (int t = -thickness; t <= thickness; t++)
            {
                int py = Mathf.Clamp(center + t, 0, size - 1);
                int px = Mathf.Clamp(x, 0, size - 1);
                pixels[py * size + px] = Color.white;
            }
        }

        // Dikey çizgi
        for (int y = center - armLength; y <= center + armLength; y++)
        {
            for (int t = -thickness; t <= thickness; t++)
            {
                int px = Mathf.Clamp(center + t, 0, size - 1);
                int py = Mathf.Clamp(y, 0, size - 1);
                pixels[py * size + px] = Color.white;
            }
        }

        // Merkez boşluk (daha iyi görünüm)
        for (int x = center - 2; x <= center + 2; x++)
        {
            for (int y = center - 2; y <= center + 2; y++)
            {
                pixels[y * size + x] = Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ═══════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════

    /// <summary>Nişan almayı başlat (sağ tık basılı).</summary>
    public void BeginAiming(ThrowStyle style, float throwForce, Vector2 origin)
    {
        isAiming = true;
        currentStyle = style;
        currentThrowForce = throwForce;

        crosshairObj.SetActive(true);
        lineRenderer.enabled = true;

        // Renk ayarla
        Color c = (style == ThrowStyle.Arc) ? arcColor : straightColor;
        lineRenderer.startColor = c;
        lineRenderer.endColor = new Color(c.r, c.g, c.b, 0.1f);
    }

    /// <summary>Nişan alma sırasında her frame güncelle.</summary>
    public void UpdateAiming(Vector2 origin)
    {
        if (!isAiming) return;

        Vector2 mouseWorld = GetMouseWorldPosition();
        crosshairObj.transform.position = new Vector3(mouseWorld.x, mouseWorld.y, -1f);

        Vector2 direction = (mouseWorld - origin).normalized;
        float distance = Vector2.Distance(mouseWorld, origin);

        switch (currentStyle)
        {
            case ThrowStyle.Arc:
                DrawArcTrajectory(origin, direction, distance);
                break;
            case ThrowStyle.StraightLine:
                DrawStraightTrajectory(origin, direction);
                break;
        }
    }

    /// <summary>Nişan almayı durdur.</summary>
    public void StopAiming()
    {
        isAiming = false;
        HideAll();
    }

    /// <summary>Fare dünya koordinatını al.</summary>
    public Vector2 GetMouseWorldPosition()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        Vector2 screenPos = UnityEngine.InputSystem.Mouse.current != null
            ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
            : Vector2.zero;
#else
        Vector2 screenPos = Input.mousePosition;
#endif

        Vector3 wPos = mainCam.ScreenToWorldPoint(screenPos);
        return new Vector2(wPos.x, wPos.y);
    }

    /// <summary>Fare yönünü al (oyuncudan).</summary>
    public Vector2 GetAimDirection(Vector2 origin)
    {
        return ((Vector2)GetMouseWorldPosition() - origin).normalized;
    }

    // ═══════════════════════════════════════════
    //  TRAJECTORY DRAWING
    // ═══════════════════════════════════════════

    private void DrawArcTrajectory(Vector2 origin, Vector2 direction, float distance)
    {
        lineRenderer.positionCount = trajectoryPointCount;

        float angle = Mathf.Atan2(direction.y, direction.x);
        float speed = Mathf.Clamp(currentThrowForce, 5f, 20f);
        float gravity = Physics2D.gravity.y;

        Vector2 velocity = new Vector2(
            Mathf.Cos(angle) * speed,
            Mathf.Sin(angle) * speed
        );

        for (int i = 0; i < trajectoryPointCount; i++)
        {
            float t = i * trajectoryTimeStep;
            float x = origin.x + velocity.x * t;
            float y = origin.y + velocity.y * t + 0.5f * gravity * t * t;
            lineRenderer.SetPosition(i, new Vector3(x, y, -1f));
        }
    }

    private void DrawStraightTrajectory(Vector2 origin, Vector2 direction)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(origin.x, origin.y, -1f));
        lineRenderer.SetPosition(1, new Vector3(origin.x + direction.x * straightLineMaxRange, origin.y + direction.y * straightLineMaxRange, -1f));
    }

    private void HideAll()
    {
        if (crosshairObj != null)
            crosshairObj.SetActive(false);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
    }
}
