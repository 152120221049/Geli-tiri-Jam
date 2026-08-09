using UnityEngine;

/// <summary>
/// Tüm oyun seslerini (SFX) tek bir merkezden ve tek bir Inspector panelinden yöneten Ses Yöneticisi.
/// - Rastgele pitch frekansı (0.9f - 1.1f) ile doğal ses dağılımı sağlar.
/// - SettingsManager (Master & SFX Volume) ile tam entegredir.
/// - Tüm mekanik kodlarından otomatik olarak çağrılır.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("⚔️ Dövüş & Silah Sesleri")]
    public AudioClip swordSwingClip;
    public AudioClip daggerSwingClip;
    public AudioClip greatswordSwingClip;
    public AudioClip bowShootClip;
    public AudioClip arrowHitClip;
    public AudioClip quiverReloadClip;

    [Header("🧪 Fırlatma & İksir / Büyü Sesleri")]
    public AudioClip potionDrinkClip;
    public AudioClip bottleBreakClip;
    public AudioClip fireballExplosionClip;
    public AudioClip lightningSpellClip;
    public AudioClip itemThrowClip;

    [Header("💨 Hareket & Can Sesleri")]
    public AudioClip footstepClip;
    public AudioClip dashWindClip;
    public AudioClip playerHurtClip;
    public AudioClip playerDeathClip;

    [Header("📦 Envanter & Arayüz (UI) Sesleri")]
    public AudioClip itemPickupClip;
    public AudioClip inventoryOpenClip;
    public AudioClip inventoryCloseClip;
    public AudioClip uiButtonClickClip;

    private AudioSource sfxAudioSource;
    private AudioSource bgmAudioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;

        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.loop = true;
    }

    /// <summary>
    /// Belirtilen ses klibini çalar. Otomatik pitch varyasyonu içerir.
    /// </summary>
    public void PlaySound(AudioClip clip, float volumeScale = 1.0f, bool randomizePitch = true)
    {
        if (clip == null) return;

        float masterVol = SettingsManager.Instance != null ? SettingsManager.Instance.MasterVolume : 1.0f;
        float sfxVol = SettingsManager.Instance != null ? SettingsManager.Instance.SFXVolume : 1.0f;
        float finalVolume = volumeScale * masterVol * sfxVol;

        if (randomizePitch && sfxAudioSource != null)
        {
            sfxAudioSource.pitch = Random.Range(0.92f, 1.08f);
            sfxAudioSource.PlayOneShot(clip, finalVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : transform.position, finalVolume);
        }
    }

    // ═══════════════════════════════════════════
    //  KODLARDAN OTOMATİK ÇAĞRILAN METODLAR
    // ═══════════════════════════════════════════

    public void PlaySwordSwing() => PlaySound(swordSwingClip);
    public void PlayDaggerSwing() => PlaySound(daggerSwingClip != null ? daggerSwingClip : swordSwingClip, 0.8f);
    public void PlayGreatswordSwing() => PlaySound(greatswordSwingClip != null ? greatswordSwingClip : swordSwingClip, 1.2f);
    public void PlayBowShoot() => PlaySound(bowShootClip);
    public void PlayArrowHit() => PlaySound(arrowHitClip);
    public void PlayQuiverReload() => PlaySound(quiverReloadClip);

    public void PlayPotionDrink() => PlaySound(potionDrinkClip);
    public void PlayBottleBreak() => PlaySound(bottleBreakClip);
    public void PlayFireballExplosion() => PlaySound(fireballExplosionClip != null ? fireballExplosionClip : bottleBreakClip, 1.2f);
    public void PlayLightningSpell() => PlaySound(lightningSpellClip);
    public void PlayItemThrow() => PlaySound(itemThrowClip);

    public void PlayFootstep() => PlaySound(footstepClip, 0.4f, true);
    public void PlayDash() => PlaySound(dashWindClip, 0.9f);
    public void PlayPlayerHurt() => PlaySound(playerHurtClip);
    public void PlayPlayerDeath() => PlaySound(playerDeathClip);

    public void PlayItemPickup() => PlaySound(itemPickupClip);
    public void PlayInventoryOpen() => PlaySound(inventoryOpenClip, 0.8f, false);
    public void PlayInventoryClose() => PlaySound(inventoryCloseClip, 0.8f, false);
    public void PlayButtonClick() => PlaySound(uiButtonClickClip, 0.8f, false);
}
