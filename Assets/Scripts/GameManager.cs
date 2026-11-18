using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 상태, 생존 시간, 처치 수. Time.timeScale은 여기서만 바꾼다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] PlayerHealth player;
    [SerializeField] ExperienceManager experience;

    public float SurvivalTime { get; private set; }
    public int Kills { get; private set; }
    public bool IsGameOver { get; private set; }

    /// <summary>게임 오버나 레벨업 팝업 중에는 플레이어 입력을 받지 않는다. 히트스톱 중에는 받는다.</summary>
    public bool InputBlocked => IsGameOver || levelUpOpen;

    bool levelUpOpen;
    float hitStopLeft;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (IsGameOver)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
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

    // ponytail: M6에서 UI로 교체할 임시 화면. 게임 오버 화면은 다시 하기를 R 키로 받는다.
    void OnGUI()
    {
        GUI.skin.label.fontSize = 28;
        string time = $"{(int)SurvivalTime / 60:00}:{(int)SurvivalTime % 60:00}";
        string level = experience.IsMaxLevel ? "Lv MAX" : $"Lv {experience.Level} ({experience.Exp}/{experience.RequiredExp})";
        GUI.Label(new Rect(20, 10, 1200, 45), $"HP {player.Health:0} / {player.MaxHealth:0}    {level}    {time}    Kills {Kills}");

        if (IsGameOver)
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "GAME OVER\nPress R to retry", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
    }
}
