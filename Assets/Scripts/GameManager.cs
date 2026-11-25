using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 상태, 생존 시간, 처치 수. Time.timeScale은 여기서만 바꾼다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// 터치(트윈스틱) 조작인지. 폰과 에디터의 Device Simulator에서는 true,
    /// Game 뷰와 PC 빌드에서는 false라 원래 PC 조작(WASD·커서·클릭·Space)으로 동작한다.
    /// </summary>
    public static bool TouchControls => Application.isMobilePlatform;

    [SerializeField] PlayerHealth player;
    [SerializeField] ExperienceManager experience;
    [Tooltip("화면 스틱·대쉬 버튼의 부모. PC 조작일 때는 끈다.")]
    [SerializeField] GameObject touchControls;
    [Tooltip("화면이 세로일 때 덮는 \"가로로 돌려주세요\" 안내. 웹에서는 방향을 고정할 수 없어서 필요하다.")]
    [SerializeField] GameObject portraitNotice;
    [Tooltip("임시 시작 화면. 안의 버튼을 눌러야 게임이 시작된다. M6에서 타이틀 씬으로 교체.")]
    [SerializeField] GameObject startPanel;
    [Tooltip("임시 HUD(OnGUI) 글꼴.")]
    [SerializeField] Font font;
    [Tooltip("모바일 앱 기본값은 30fps라 직접 올린다. 웹 빌드에서는 무시하고 브라우저에 맡긴다.")]
    [SerializeField] int targetFrameRate = 60;

    public float SurvivalTime { get; private set; }
    public int Kills { get; private set; }
    public bool IsGameOver { get; private set; }

    /// <summary>게임 오버, 레벨업 팝업, 세로 화면 안내 중에는 플레이어 입력을 받지 않는다. 히트스톱 중에는 받는다.</summary>
    public bool InputBlocked => !started || IsGameOver || levelUpOpen || portrait;

    bool started;
    bool levelUpOpen;
    bool portrait;
    float hitStopLeft;

    void Awake()
    {
        Instance = this;
        // 시작 버튼을 누를 때까지 멈춰 있다(timeScale 0이라 생존 시간·스폰도 흐르지 않는다).
        startPanel.SetActive(true);
        startPanel.GetComponentInChildren<Button>().onClick.AddListener(StartGame);
        RefreshTimeScale();
        // 웹은 브라우저 화면 갱신(requestAnimationFrame)에 맡겨야 부드럽다. 숫자를 지정하면 타이머로 돌아서 끊긴다.
        Application.targetFrameRate = Application.platform == RuntimePlatform.WebGLPlayer ? -1 : targetFrameRate;
        touchControls.SetActive(TouchControls);
        Screen.sleepTimeout = SleepTimeout.NeverSleep; // 플레이 중 화면이 꺼지지 않게
    }

    void Update()
    {
        // 세로면 안내를 덮고 멈춘다. 세로 화면은 맵(18 units)보다 위아래가 더 보여서 맵 밖이 드러난다.
        bool isPortrait = Screen.height > Screen.width;
        if (isPortrait != portrait)
        {
            portrait = isPortrait;
            portraitNotice.SetActive(portrait);
            RefreshTimeScale();
        }

        if (IsGameOver)
        {
            if ((Pointer.current != null && Pointer.current.press.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        SurvivalTime += Time.deltaTime;

        if (hitStopLeft > 0f)
        {
            hitStopLeft -= Time.unscaledDeltaTime;
            RefreshTimeScale();
        }
    }

    void RefreshTimeScale() => Time.timeScale = InputBlocked || hitStopLeft > 0f ? 0f : 1f;

    void StartGame()
    {
        started = true;
        startPanel.SetActive(false);
        RefreshTimeScale();
    }

    public void AddKill() => Kills++;

    public void HitStop(float duration)
    {
        hitStopLeft = duration;
        RefreshTimeScale();
    }

    public void SetLevelUpOpen(bool open)
    {
        levelUpOpen = open;
        RefreshTimeScale();
    }

    public void GameOver()
    {
        IsGameOver = true;
        RefreshTimeScale();
    }

    // ponytail: M6에서 UI로 교체할 임시 화면. 게임 오버 화면은 탭·클릭이나 R 키로 다시 한다.
    void OnGUI()
    {
        GUI.skin.font = font;
        GUI.skin.label.fontSize = 28;
        string time = $"{(int)SurvivalTime / 60:00}:{(int)SurvivalTime % 60:00}";
        string level = experience.IsMaxLevel ? "Lv MAX" : $"Lv {experience.Level} ({experience.Exp}/{experience.RequiredExp})";
        GUI.Label(new Rect(20, 10, 1200, 45), $"HP {player.Health:0} / {player.MaxHealth:0}    {level}    {time}    Kills {Kills}");

        if (IsGameOver)
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "GAME OVER\nTap to retry", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
    }
}
