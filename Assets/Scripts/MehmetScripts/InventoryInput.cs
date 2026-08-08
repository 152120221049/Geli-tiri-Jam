using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Giriş (Input) sistem uyumluluk katmanı.
/// Projede hem Yeni Input System hem de Eski Input Manager seçili olsa veya
/// yalnızca Yeni Input System aktif olsa dahi hatasız girdi okur.
/// </summary>
public static class InventoryInput
{
    /// <summary>
    /// Envanter açma/kapama için 'I' veya 'E' tuşuna basılı tutulup tutulmadığını döner.
    /// </summary>
    public static bool IsInventoryKeyHeld(KeyCode legacy1, KeyCode legacy2)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            bool iPressed = Keyboard.current.iKey.isPressed;
            bool ePressed = Keyboard.current.eKey.isPressed;
            if (iPressed || ePressed) return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            return Input.GetKey(legacy1) || Input.GetKey(legacy2);
        }
        catch { }
#endif

        return false;
    }

    /// <summary>
    /// Hotbar kısayol tuşuna (1–4) basılıp basılmadığını döner (digitIndex: 0 → '1', 1 → '2', vb.).
    /// </summary>
    public static bool IsDigitKeyPressed(int digitIndex)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            switch (digitIndex)
            {
                case 0: if (Keyboard.current.digit1Key.wasPressedThisFrame) return true; break;
                case 1: if (Keyboard.current.digit2Key.wasPressedThisFrame) return true; break;
                case 2: if (Keyboard.current.digit3Key.wasPressedThisFrame) return true; break;
                case 3: if (Keyboard.current.digit4Key.wasPressedThisFrame) return true; break;
                case 4: if (Keyboard.current.digit5Key.wasPressedThisFrame) return true; break;
                case 5: if (Keyboard.current.digit6Key.wasPressedThisFrame) return true; break;
                case 6: if (Keyboard.current.digit7Key.wasPressedThisFrame) return true; break;
                case 7: if (Keyboard.current.digit8Key.wasPressedThisFrame) return true; break;
                case 8: if (Keyboard.current.digit9Key.wasPressedThisFrame) return true; break;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            return Input.GetKeyDown(KeyCode.Alpha1 + digitIndex);
        }
        catch { }
#endif

        return false;
    }

    /// <summary>
    /// Eşya döndürme için 'R' tuşuna bu karede basılıp basılmadığını döner.
    /// </summary>
    public static bool IsRotateKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            return Input.GetKeyDown(KeyCode.R);
        }
        catch { }
#endif

        return false;
    }

    /// <summary>
    /// Sol tık (mouse 0) tuşuna bu karede basılıp basılmadığını döner.
    /// </summary>
    public static bool IsLeftClickDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            return Input.GetMouseButtonDown(0);
        }
        catch { }
#endif

        return false;
    }

    /// <summary>
    /// Fare tekerlek kaydırma miktarını döner (pozitif/negatif).
    /// </summary>
    public static float GetScrollDeltaY()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            float scrollY = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.01f)
                return scrollY;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            return Input.GetAxis("Mouse ScrollWheel");
        }
        catch { }
#endif

        return 0f;
    }

    /// <summary>
    /// Mevcut fare konumunu (ekran koordinatı) döner.
    /// </summary>
    public static Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            return Input.mousePosition;
        }
        catch { }
#endif

        return Vector2.zero;
    }
}
