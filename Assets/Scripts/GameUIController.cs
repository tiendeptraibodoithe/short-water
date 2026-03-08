using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class GameUIController : MonoBehaviour
{
    [Header("Result Panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Panel Animation")]
    public float panelAnimDuration = 0.4f;

    // ── Settings Button Slide Animation ─────────────────────────
    [Header("Settings Button Slide")]
    [Tooltip("GameObject cha chứa các button settings (sẽ được bật/tắt theo panel).")]
    public GameObject settingsPanel;

    [Tooltip("RectTransform của nút Settings (nút bấm toggle).")]
    public RectTransform settingsToggleButton;

    [Tooltip("Các button sẽ trượt ra từ vị trí của nút Settings (kéo đúng thứ tự bạn muốn stagger).")]
    public RectTransform[] settingsButtons;

    [Tooltip("Thời gian mỗi button trượt ra/vào.")]
    public float slideDuration = 0.25f;

    [Tooltip("Độ trễ stagger giữa các button (giây).")]
    public float staggerDelay = 0.05f;

    // Lưu anchoredPosition gốc (đã set trong Inspector) của từng button
    private Vector2[] _originalPositions;
    private bool _settingsOpen = false;

    // ── Toggle Button Sprites ────────────────────────────────────
    [Header("Toggle Button Sprites")]
    public Sprite greenSprite;
    public Sprite greySprite;

    [Header("Settings Toggle Buttons")]
    public Image soundButtonImage;
    public Image musicButtonImage;
    public Image hapticButtonImage;

    // Trạng thái toggle
    private bool isSoundOn;
    private bool isMusicOn;
    private bool isHapticOn;

    // ── Lifecycle ────────────────────────────────────────────────

    void OnEnable()
    {
        GameController.OnWin  += ShowWinPanel;
        GameController.OnLose += ShowLosePanel;
    }

    void OnDisable()
    {
        GameController.OnWin  -= ShowWinPanel;
        GameController.OnLose -= ShowLosePanel;
    }

    void Start()
    {
        // Ẩn win/lose panels
        if (winPanel  != null) { winPanel.SetActive(false);  winPanel.transform.localScale  = Vector3.zero; }
        if (losePanel != null) { losePanel.SetActive(false); losePanel.transform.localScale = Vector3.zero; }

        // Lưu anchoredPosition gốc của mỗi settings button và ẩn chúng về vị trí nút toggle
        if (settingsButtons != null && settingsToggleButton != null)
        {
            _originalPositions = new Vector2[settingsButtons.Length];
            for (int i = 0; i < settingsButtons.Length; i++)
            {
                if (settingsButtons[i] == null) continue;
                _originalPositions[i] = settingsButtons[i].anchoredPosition;
                // Đặt về vị trí nút settings và ẩn đi
                settingsButtons[i].anchoredPosition = settingsToggleButton.anchoredPosition;
                settingsButtons[i].gameObject.SetActive(false);
            }
        }

        // Đọc trạng thái toggle
        isSoundOn  = PlayerPrefs.GetInt("SoundOn",     1) == 1;
        isMusicOn  = PlayerPrefs.GetInt("MusicOn",     1) == 1;
        isHapticOn = PlayerPrefs.GetInt("VibrationOn", 1) == 1;

        UpdateButtonSprite(soundButtonImage,  isSoundOn);
        UpdateButtonSprite(musicButtonImage,  isMusicOn);
        UpdateButtonSprite(hapticButtonImage, isHapticOn);
    }

    // ── Win / Lose ───────────────────────────────────────────────

    void ShowWinPanel()
    {
        SoundManager.Instance?.PlaySFX(SoundType.Win);
        PanelAnimator.Show(winPanel, panelAnimDuration);
    }

    void ShowLosePanel()
    {
        SoundManager.Instance?.PlaySFX(SoundType.Lose);
        PanelAnimator.Show(losePanel, panelAnimDuration);
    }

    // ── Settings Slide Toggle ────────────────────────────────────

    public void ToggleSettingsPanel()
    {
        SoundManager.Instance?.PlaySFX(SoundType.BtnClick);
        Debug.Log($"[GameUI] ToggleSettingsPanel called. _settingsOpen={_settingsOpen}");
        if (_settingsOpen)
            CloseSettingsButtons();
        else
            OpenSettingsButtons();
    }

    private void OpenSettingsButtons()
    {
        _settingsOpen = true;
        if (settingsToggleButton == null)
        {
            Debug.LogWarning("[GameUI] settingsToggleButton chưa được gán trong Inspector!");
            return;
        }
        if (settingsButtons == null || settingsButtons.Length == 0)
        {
            Debug.LogWarning("[GameUI] settingsButtons chưa được gán hoặc rỗng trong Inspector!");
            return;
        }

        // Bật panel cha trước để các button con có thể hiện ra
        if (settingsPanel != null) settingsPanel.SetActive(true);

        for (int i = 0; i < settingsButtons.Length; i++)
        {
            if (settingsButtons[i] == null) continue;
            var btn = settingsButtons[i];
            var targetPos = _originalPositions[i];
            float delay = i * staggerDelay;

            btn.anchoredPosition = settingsToggleButton.anchoredPosition;
            btn.gameObject.SetActive(true);
            btn.DOAnchorPos(targetPos, slideDuration)
               .SetDelay(delay)
               .SetEase(Ease.OutBack);
        }
    }

    private void CloseSettingsButtons()
    {
        _settingsOpen = false;
        if (settingsButtons == null || settingsToggleButton == null) return;

        // Đóng theo thứ tự ngược lại (button đầu tiên trong mảng đóng sau cùng)
        for (int i = settingsButtons.Length - 1; i >= 0; i--)
        {
            if (settingsButtons[i] == null) continue;
            var btn = settingsButtons[i];
            float delay = (settingsButtons.Length - 1 - i) * staggerDelay;
            bool isLastToFinish = (i == 0);

            var tween = btn.DOAnchorPos(settingsToggleButton.anchoredPosition, slideDuration * 0.8f)
               .SetDelay(delay)
               .SetEase(Ease.InBack)
               .OnComplete(() => btn.gameObject.SetActive(false));

            // Button cuối cùng hoàn tất → tắt panel cha
            if (isLastToFinish && settingsPanel != null)
            {
                tween.OnComplete(() =>
                {
                    btn.gameObject.SetActive(false);
                    settingsPanel.SetActive(false);
                });
            }
        }
    }

    // ── Settings Toggles ─────────────────────────────────────────

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0);
        UpdateButtonSprite(soundButtonImage, isSoundOn);
        SoundManager.Instance?.RefreshSettings();
    }

    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        PlayerPrefs.SetInt("MusicOn", isMusicOn ? 1 : 0);
        UpdateButtonSprite(musicButtonImage, isMusicOn);
        SoundManager.Instance?.RefreshSettings();
    }

    public void ToggleHaptic()
    {
        isHapticOn = !isHapticOn;
        PlayerPrefs.SetInt("VibrationOn", isHapticOn ? 1 : 0);
        UpdateButtonSprite(hapticButtonImage, isHapticOn);
    }

    private void UpdateButtonSprite(Image btnImage, bool isOn)
    {
        if (btnImage == null) return;
        btnImage.sprite = isOn ? greenSprite : greySprite;
    }

    // ── Navigation ───────────────────────────────────────────────

    public void OnHomeButtonClicked()
    {
        SoundManager.Instance?.PlaySFX(SoundType.BtnClick);
        SceneManager.LoadScene("MenuScene");
    }

    public void OnRestartButtonClicked()
    {
        SoundManager.Instance?.PlaySFX(SoundType.BtnClick);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnNextLevelButtonClicked()
    {
        SoundManager.Instance?.PlaySFX(SoundType.BtnClick);
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);
        else
            SceneManager.LoadScene("MenuScene");
    }
}
