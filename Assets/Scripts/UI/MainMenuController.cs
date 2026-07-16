using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private string playSceneName = "play";
    [SerializeField] private string deckEditorSceneName = "DeckEditor";

    [Header("Scene UI")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button editDeckButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button closeSettingsButton;

    private const string VolumeKey = "loveca.masterVolume";

    private void Awake()
    {
        playButton.onClick.AddListener(Play);
        editDeckButton.onClick.AddListener(EditDeck);
        settingsButton.onClick.AddListener(OpenSettings);
        exitButton.onClick.AddListener(ExitGame);
        closeSettingsButton.onClick.AddListener(CloseSettings);
        volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        volumeSlider.SetValueWithoutNotify(savedVolume);
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        AudioListener.volume = savedVolume;
        settingsPanel.SetActive(false);
    }

    public void Play() => SceneManager.LoadScene(playSceneName);
    public void EditDeck() => SceneManager.LoadScene(deckEditorSceneName);
    public void OpenSettings() => settingsPanel.SetActive(true);
    public void CloseSettings() => settingsPanel.SetActive(false);

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void SetFullscreen(bool value) => Screen.fullScreen = value;

#if UNITY_EDITOR
    public void ConfigureEditorReferences(
        Button play, Button editDeck, Button settings, Button exit,
        GameObject settingsRoot, Slider volume, Toggle fullscreen, Button close)
    {
        playButton = play;
        editDeckButton = editDeck;
        settingsButton = settings;
        exitButton = exit;
        settingsPanel = settingsRoot;
        volumeSlider = volume;
        fullscreenToggle = fullscreen;
        closeSettingsButton = close;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
