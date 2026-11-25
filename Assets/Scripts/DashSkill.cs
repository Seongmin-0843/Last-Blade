using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 대쉬 버튼으로 이동 방향(정지 중이면 조준 방향)으로 돌진. 획득 전에는 컴포넌트와 버튼이 꺼져 있다. (기획서 §6)
/// </summary>
public class DashSkill : MonoBehaviour
{
    [Tooltip("돌진 거리(units).")]
    [SerializeField] float distance = 3f;
    [Tooltip("돌진에 걸리는 시간(초).")]
    [SerializeField] float duration = 0.12f;
    [Tooltip("화면의 대쉬 버튼(OnScreenButton). 획득하면 켠다.")]
    [SerializeField] GameObject touchButton;

    PlayerController controller;
    SwordAttack sword;
    InputAction dashAction;
    float cooldown;
    float nextDashTime;
    float dashLeft;
    Vector2 dashVelocity;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        sword = GetComponent<SwordAttack>();
        dashAction = InputSystem.actions.FindAction("Player/Dash", true);
    }

    void OnEnable() => dashAction.Enable();
    void OnDisable() => dashAction.Disable();

    /// <summary>쿨타임 진행도. 0이면 방금 썼고 1이면 쓸 수 있다. 획득 전에도 1.</summary>
    public float CooldownProgress => cooldown > 0f ? 1f - Mathf.Clamp01((nextDashTime - Time.time) / cooldown) : 1f;

    public void Upgrade(float newCooldown)
    {
        cooldown = newCooldown;
        enabled = true;
        touchButton.SetActive(true);
    }

    void Update()
    {
        if (GameManager.Instance.InputBlocked) return;

        if (dashLeft > 0f)
        {
            float step = Mathf.Min(Time.deltaTime, dashLeft);
            controller.Move(dashVelocity * step);
            dashLeft -= step;
            controller.MovementLocked = dashLeft > 0f;
            return;
        }

        if (!dashAction.WasPressedThisFrame() || Time.time < nextDashTime) return;

        Vector2 direction = controller.MoveInput != Vector2.zero ? controller.MoveInput.normalized : sword.Aim;
        dashVelocity = direction * (distance / duration);
        dashLeft = duration;
        nextDashTime = Time.time + cooldown;
        controller.MovementLocked = true;
    }
}
