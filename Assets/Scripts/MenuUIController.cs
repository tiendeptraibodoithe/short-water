using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject settingsPanel;
    public GameObject levelsPanel;

    [Header("Toggle Button Sprites")]
    public Sprite greenSprite;
    public Sprite greySprite;

    [Header("Toggle Buttons")]
    public Image soundButtonImage;
    public Image musicButtonImage;
    public Image vibrationButtonImage;

    [Header("Panel Animation")]
    public float panelAnimDuration = 0.3f;

    // Trạng thái toggle
    private bool isSoundOn;
    private bool isMusicOn;
    private bool isVibrationOn;

    private void Start()
    {
        isSoundOn     = PlayerPrefs.GetInt("SoundOn",     1) == 1;
        isMusicOn     = PlayerPrefs.GetInt("MusicOn",     1) == 1;
        isVibrationOn = PlayerPrefs.GetInt("VibrationOn", 1) == 1;

        UpdateButtonSprite(soundButtonImage,     isSoundOn);
        UpdateButtonSprite(musicButtonImage,     isMusicOn);
        UpdateButtonSprite(vibrationButtonImage, isVibrationOn);

        if (settingsPanel != null) { settingsPanel.SetActive(false); settingsPanel.transform.localScale = UnityEngine.Vector3.zero; }
        if (levelsPanel   != null) { levelsPanel.SetActive(false);   levelsPanel.transform.localScale   = UnityEngine.Vector3.zero; }
    }

    // ── Toggle ───────────────────────────────────────────────────

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

    public void ToggleVibration()
    {
        isVibrationOn = !isVibrationOn;
        PlayerPrefs.SetInt("VibrationOn", isVibrationOn ? 1 : 0);
        UpdateButtonSprite(vibrationButtonImage, isVibrationOn);
    }

    private void UpdateButtonSprite(Image btnImage, bool isOn)
    {
        if (btnImage == null) return;
        btnImage.sprite = isOn ? greenSprite : greySprite;
    }

    // ── Scene / Panel ────────────────────────────────────────────

    public void OnStartButtonClicked()
    {
        SoundManager.Instance?.PlaySFX(SoundType.BtnClick);
        SceneManager.LoadScene("SampleScene1");
    }

    public void OpenSettingsPanel()
    {
        SoundManager.Instance?.PlaySFX(SoundType.BtnClick);
        PanelAnimator.Show(settingsPanel, panelAnimDuration);
    }

    public void CloseSettingsPanel()
    {
        SoundManager.Instance?.PlaySFX(SoundType.BtnClick);
        PanelAnimator.Hide(settingsPanel, panelAnimDuration * 0.8f);
    }

    public void OpenLevelsPanel()
    {
        SoundManager.Instance?.PlaySFX(SoundType.BtnClick);
        PanelAnimator.Show(levelsPanel, panelAnimDuration);
    }

    public void CloseLevelsPanel()
    {
        SoundManager.Instance?.PlaySFX(SoundType.BtnClick);
        PanelAnimator.Hide(levelsPanel, panelAnimDuration * 0.8f);
    }

    public void LoadLevel(int level)
    {
        SoundManager.Instance?.PlaySFX(SoundType.BtnClick);
        string sceneName = "SampleScene" + level;
        SceneManager.LoadScene(sceneName);
    }
}
