using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  Đăng ký SFX mới: chỉ cần thêm một entry vào enum này,
//  rồi gán AudioClip tương ứng trong Inspector (mảng sfxEntries).
// ─────────────────────────────────────────────────────────────────────────────
public enum SoundType
{
    BtnClick,
    BottleClose,
    BottleFull,
    WaterPour,
    Win,
    Lose
}

[System.Serializable]
public struct SFXEntry
{
    public SoundType soundType;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("AudioSource dành riêng cho nhạc nền (loop).")]
    public AudioSource musicSource;

    [Tooltip("AudioSource dành riêng cho âm thanh hiệu ứng ngắn (one-shot).")]
    public AudioSource sfxSource;

    [Tooltip("AudioSource dành riêng cho âm thanh phát liên tục (loop SFX, ví dụ: WaterPour).")]
    public AudioSource loopSource;

    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    [Header("Sound Effects")]
    [Tooltip("Đăng ký từng SoundType với AudioClip tương ứng.")]
    public SFXEntry[] sfxEntries;


    private Dictionary<SoundType, AudioClip> sfxMap;
    private bool isMusicOn;
    private bool isSoundOn;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSFXMap();
        RefreshSettings();
    }

    private void Start()
    {
        PlayBackgroundMusic();
    }

    // Phát âm thanh ngắn (one-shot) – không loop
    public void PlaySFX(SoundType type)
    {
        if (!isSoundOn) return;
        if (sfxSource == null) return;

        if (sfxMap.TryGetValue(type, out AudioClip clip) && clip != null)
            sfxSource.PlayOneShot(clip);
        else
            Debug.LogWarning($"[SoundManager] Không tìm thấy clip cho SoundType: {type}");
    }

    // Trả về độ dài (giây) của clip – dùng để delay phát SFX tiếp theo
    public float GetClipLength(SoundType type)
    {
        if (sfxMap.TryGetValue(type, out AudioClip clip) && clip != null)
            return clip.length;
        return 0f;
    }

    // Phát âm thanh liên tục (loop) – dùng cho WaterPour
    public void PlaySFXLoop(SoundType type)
    {
        if (!isSoundOn) return;
        if (loopSource == null) return;

        if (!sfxMap.TryGetValue(type, out AudioClip clip) || clip == null)
        {
            Debug.LogWarning($"[SoundManager] Không tìm thấy clip cho SoundType: {type}");
            return;
        }

        // Không restart nếu đang phát đúng clip này rồi
        if (loopSource.isPlaying && loopSource.clip == clip) return;

        loopSource.clip = clip;
        loopSource.loop = true;
        loopSource.Play();
    }

    // Dừng âm thanh loop
    public void StopSFXLoop()
    {
        if (loopSource != null && loopSource.isPlaying)
            loopSource.Stop();
    }

    public void PlayBackgroundMusic()
    {
        if (musicSource == null || backgroundMusic == null) return;

        musicSource.clip   = backgroundMusic;
        musicSource.loop   = true;
        musicSource.volume = musicVolume;
        musicSource.mute   = !isMusicOn;
        musicSource.Play();
    }


    public void RefreshSettings()
    {
        isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        if (musicSource != null) musicSource.mute = !isMusicOn;
        if (sfxSource   != null) sfxSource.mute   = !isSoundOn;
        if (loopSource  != null) loopSource.mute  = !isSoundOn;
    }

    private void BuildSFXMap()
    {
        sfxMap = new Dictionary<SoundType, AudioClip>();

        if (sfxEntries == null) return;

        foreach (var entry in sfxEntries)
        {
            if (!sfxMap.ContainsKey(entry.soundType))
                sfxMap.Add(entry.soundType, entry.clip);
            else
                Debug.LogWarning($"[SoundManager] SoundType '{entry.soundType}' bị đăng ký trùng. Chỉ dùng entry đầu tiên.");
        }
    }
}
