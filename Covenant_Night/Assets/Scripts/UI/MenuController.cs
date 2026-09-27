using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Title screen and in-game pause menu (Resume / Restart Zone / Controls / Settings / Main Menu / Quit).
// The menu runs on unscaled time; pausing freezes the simulation through GameManager.SetUserPaused.
// The editor builder creates the UI and assigns the references below.
public class MenuController : MonoBehaviour
{
    public static MenuController Instance { get; private set; }

    // Set before reloading the Persistent scene to go straight into play ("play again" after the credits).
    public static bool SkipTitleOnce;

    enum Page { None, Title, Difficulty, Mixed, Pause, Controls, Settings }

    [Header("Root")]
    public GameObject canvasRoot;
    public CanvasGroup group;
    public GameObject titlePage, difficultyPage, pausePage, controlsPage, settingsPage;

    [Header("Title")]
    public Button playBtn, titleControlsBtn, titleSettingsBtn, titleQuitBtn;

    [Header("Difficulty")]
    public Button easyBtn, mediumBtn, hardBtn, hardcoreBtn, difficultyBackBtn;

    [Header("Mixed Difficulty (New Game+)")]
    public Button mixedBtn;                    // on the Difficulty page; shown only once New Game+ is unlocked
    public GameObject mixedPage;
    public Button[] zoneLevelBtns = new Button[5];
    public Button mixedStartBtn, mixedBackBtn;

    [Header("Pause")]
    public Button resumeBtn, restartBtn, pauseControlsBtn, pauseSettingsBtn, mainMenuBtn, pauseQuitBtn;

    [Header("Sub pages")]
    public Button controlsBackBtn, settingsBackBtn;
    public Slider volumeSlider, lookSlider;
    public TextMeshProUGUI volumeLabel, lookLabel;
    public Button invertBtn, fullscreenBtn;

    Page _page = Page.None;
    Page _parent = Page.Pause;
    bool _playClicked;
    Button _pending;
    string _pendingText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        GameSettings.Apply();
        EnsureEventSystem();

        playBtn.onClick.AddListener(() => Show(Page.Difficulty));
        easyBtn.onClick.AddListener(() => StartRun(DifficultyLevel.Easy));
        mediumBtn.onClick.AddListener(() => StartRun(DifficultyLevel.Medium));
        hardBtn.onClick.AddListener(() => StartRun(DifficultyLevel.Hard));
        hardcoreBtn.onClick.AddListener(() => StartRun(DifficultyLevel.Hardcore));
        difficultyBackBtn.onClick.AddListener(() => Show(Page.Title));
        titleControlsBtn.onClick.AddListener(() => OpenSub(Page.Controls, Page.Title));
        titleSettingsBtn.onClick.AddListener(() => OpenSub(Page.Settings, Page.Title));
        titleQuitBtn.onClick.AddListener(Quit);

        mixedBtn.onClick.AddListener(() => Show(Page.Mixed));
        mixedBtn.gameObject.SetActive(GameDifficulty.NewGamePlusUnlocked);
        for (int i = 0; i < zoneLevelBtns.Length; i++)
        {
            int idx = i;
            zoneLevelBtns[i].onClick.AddListener(() => CycleZoneLevel(idx));
        }
        mixedStartBtn.onClick.AddListener(StartMixedRun);
        mixedBackBtn.onClick.AddListener(() => Show(Page.Difficulty));
        RefreshMixedLabels();

        resumeBtn.onClick.AddListener(Resume);
        pauseControlsBtn.onClick.AddListener(() => OpenSub(Page.Controls, Page.Pause));
        pauseSettingsBtn.onClick.AddListener(() => OpenSub(Page.Settings, Page.Pause));
        Confirmable(restartBtn, RestartZone);
        Confirmable(mainMenuBtn, MainMenu);
        Confirmable(pauseQuitBtn, Quit);

        controlsBackBtn.onClick.AddListener(() => Show(_parent));
        settingsBackBtn.onClick.AddListener(() => Show(_parent));

        volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
        lookSlider.minValue = GameSettings.MinLookScale;
        lookSlider.maxValue = GameSettings.MaxLookScale;
        lookSlider.SetValueWithoutNotify(GameSettings.LookScale);
        volumeSlider.onValueChanged.AddListener(v => { GameSettings.Volume = v; RefreshSettingsLabels(); });
        lookSlider.onValueChanged.AddListener(v => { GameSettings.LookScale = v; RefreshSettingsLabels(); });
        invertBtn.onClick.AddListener(() => { GameSettings.InvertY = !GameSettings.InvertY; RefreshSettingsLabels(); });
        fullscreenBtn.onClick.AddListener(() => { GameSettings.Fullscreen = !GameSettings.Fullscreen; RefreshSettingsLabels(); });
        fullscreenBtn.gameObject.SetActive(GameSettings.CanChangeFullscreen);

        bool canQuit = !(Application.isConsolePlatform || Application.platform == RuntimePlatform.WebGLPlayer ||
                         Application.platform == RuntimePlatform.IPhonePlayer);
        titleQuitBtn.gameObject.SetActive(canQuit);
        pauseQuitBtn.gameObject.SetActive(canQuit);

        RefreshSettingsLabels();
        HideAll();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        DontDestroyOnLoad(go);
    }

    // ── Title ───────────────────────────────────────────────────────────────

    // Shown once when the game starts; ZoneManager waits for it before loading the first zone.
    public IEnumerator RunTitle()
    {
        if (SkipTitleOnce) { SkipTitleOnce = false; yield break; }

        _playClicked = false;
        group.alpha = 1f;
        Show(Page.Title);
        while (!_playClicked) yield return null;

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / 0.4f);
            yield return null;
        }
        Show(Page.None);
        group.alpha = 1f;
    }

    // ── Update ──────────────────────────────────────────────────────────────

    void Update()
    {
        var input = InputReader.Instance;
        var gm = GameManager.Instance;
        bool pausePressed = input != null && input.PausePressed;
        bool back = pausePressed || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        switch (_page)
        {
            case Page.None:
                bool transitioning = ZoneManager.Instance != null && ZoneManager.Instance.IsTransitioning;
                if (pausePressed && gm != null && gm.CanUserPause && !transitioning) OpenPause();
                break;
            case Page.Difficulty:
                if (back) Show(Page.Title);
                break;
            case Page.Mixed:
                if (back) Show(Page.Difficulty);
                break;
            case Page.Pause:
                if (back) Resume();
                break;
            case Page.Controls:
            case Page.Settings:
                if (back) Show(_parent);
                break;
        }

        if (_page != Page.None && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            SelectFirst();
    }

    void LateUpdate()
    {
        if (_page == Page.None) return;
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
    }

#if !UNITY_EDITOR
    void OnApplicationFocus(bool focus)
    {
        var gm = GameManager.Instance;
        if (!focus && _page == Page.None && gm != null && gm.CanUserPause) OpenPause();
    }
#endif

    // ── Pages ───────────────────────────────────────────────────────────────

    void StartRun(DifficultyLevel level)
    {
        GameDifficulty.MixedModeActive = false;
        GameDifficulty.Level = level;
        _playClicked = true;
    }

    void CycleZoneLevel(int index)
    {
        GameDifficulty.ZoneLevels[index] = GameDifficulty.ZoneLevels[index] switch
        {
            DifficultyLevel.Easy   => DifficultyLevel.Medium,
            DifficultyLevel.Medium => DifficultyLevel.Hard,
            _                      => DifficultyLevel.Easy,
        };
        RefreshMixedLabels();
    }

    void RefreshMixedLabels()
    {
        for (int i = 0; i < zoneLevelBtns.Length; i++)
            zoneLevelBtns[i].GetComponentInChildren<TMP_Text>().text = GameDifficulty.ZoneLevels[i].ToString();
    }

    void StartMixedRun()
    {
        GameDifficulty.MixedModeActive = true;
        _playClicked = true;
    }

    void OpenPause()
    {
        // Hardcore has one life, so restarting means restarting the whole run.
        restartBtn.GetComponentInChildren<TMP_Text>().text = GameDifficulty.Hardcore ? "Restart Run" : "Restart Zone";
        GameManager.Instance.SetUserPaused(true);
        Show(Page.Pause);
    }

    void Resume()
    {
        Show(Page.None);
        GameManager.Instance?.SetUserPaused(false);
    }

    void OpenSub(Page page, Page parent)
    {
        _parent = parent;
        Show(page);
    }

    void HideAll()
    {
        titlePage.SetActive(false);
        difficultyPage.SetActive(false);
        mixedPage.SetActive(false);
        pausePage.SetActive(false);
        controlsPage.SetActive(false);
        settingsPage.SetActive(false);
        canvasRoot.SetActive(false);
    }

    void Show(Page page)
    {
        if (_page == Page.Settings && page != Page.Settings) GameSettings.Save();
        ClearPending();
        _page = page;

        canvasRoot.SetActive(page != Page.None);
        titlePage.SetActive(page == Page.Title);
        difficultyPage.SetActive(page == Page.Difficulty);
        mixedPage.SetActive(page == Page.Mixed);
        pausePage.SetActive(page == Page.Pause);
        controlsPage.SetActive(page == Page.Controls);
        settingsPage.SetActive(page == Page.Settings);

        if (page == Page.None)
        {
            if (!Application.isConsolePlatform) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            SelectFirst();
        }
    }

    void SelectFirst()
    {
        if (EventSystem.current == null) return;
        Selectable first = null;
        switch (_page)
        {
            case Page.Title:    first = playBtn; break;
            case Page.Difficulty:
                switch (GameDifficulty.Level)
                {
                    case DifficultyLevel.Easy:     first = easyBtn; break;
                    case DifficultyLevel.Hard:     first = hardBtn; break;
                    case DifficultyLevel.Hardcore: first = hardcoreBtn; break;
                    default:                       first = mediumBtn; break;
                }
                break;
            case Page.Mixed:    first = zoneLevelBtns[0]; break;
            case Page.Pause:    first = resumeBtn; break;
            case Page.Controls: first = controlsBackBtn; break;
            case Page.Settings: first = volumeSlider; break;
        }
        if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    // ── Actions ─────────────────────────────────────────────────────────────

    void RestartZone()
    {
        Resume();
        if (GameDifficulty.Hardcore) ZoneManager.Instance?.RestartRun();
        else ZoneManager.Instance?.RestartCurrentZone();
    }

    void MainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("Persistent");    // Single mode: rebuilds every manager and shows the title again
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // A destructive button needs a second click; the label asks for it.
    void Confirmable(Button b, Action action)
    {
        b.onClick.AddListener(() =>
        {
            if (_pending == b) { ClearPending(); action(); return; }
            ClearPending();
            _pending = b;
            var label = b.GetComponentInChildren<TMP_Text>();
            _pendingText = label.text;
            label.text = "Click Again to Confirm";
        });
    }

    void ClearPending()
    {
        if (_pending == null) return;
        _pending.GetComponentInChildren<TMP_Text>().text = _pendingText;
        _pending = null;
    }

    void RefreshSettingsLabels()
    {
        volumeLabel.text = $"Volume: {Mathf.RoundToInt(GameSettings.Volume * 100f)}%";
        lookLabel.text = $"Look Speed: {GameSettings.LookScale:0.0}x";
        invertBtn.GetComponentInChildren<TMP_Text>().text = $"Invert Look Y: {(GameSettings.InvertY ? "On" : "Off")}";
        fullscreenBtn.GetComponentInChildren<TMP_Text>().text = $"Fullscreen: {(GameSettings.Fullscreen ? "On" : "Off")}";
    }
}
