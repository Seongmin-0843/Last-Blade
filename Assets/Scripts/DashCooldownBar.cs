using UnityEngine;

/// <summary>
/// 플레이어 발밑의 대쉬 쿨타임 바. 쿨타임 동안 차오르고, 다 차면 한 번 반짝이고 사라진다.
/// </summary>
public class DashCooldownBar : MonoBehaviour
{
    [SerializeField] DashSkill dash;
    [SerializeField] SpriteRenderer background;
    [SerializeField] SpriteRenderer fill;
    [Tooltip("다 찼을 때 반짝이는 색.")]
    [SerializeField] Color flashColor = Color.white;
    [Tooltip("다 찼을 때 반짝이는 시간(초).")]
    [SerializeField] float flashDuration = 0.15f;

    Color fillColor;
    Vector3 fullScale;
    bool wasCooling;
    float flashLeft;

    void Awake()
    {
        // 단색 막대라 그림 파일 없이 흰색 텍스처로 만든다. 왼쪽 기준이라 X 크기만 바꾸면 오른쪽으로 차오른다.
        Texture2D white = Texture2D.whiteTexture;
        Sprite sprite = Sprite.Create(white, new Rect(0f, 0f, white.width, white.height), new Vector2(0f, 0.5f), white.width);
        background.sprite = sprite;
        fill.sprite = sprite;

        fillColor = fill.color;
        fullScale = fill.transform.localScale;
    }

    void Update()
    {
        float progress = dash.CooldownProgress;
        bool cooling = progress < 1f;

        if (wasCooling && !cooling) flashLeft = flashDuration;
        wasCooling = cooling;
        if (flashLeft > 0f) flashLeft -= Time.deltaTime;

        bool flashing = flashLeft > 0f;
        background.enabled = fill.enabled = cooling || flashing;

        fill.color = flashing ? flashColor : fillColor;
        fill.transform.localScale = new Vector3(fullScale.x * progress, fullScale.y, fullScale.z);
    }
}
