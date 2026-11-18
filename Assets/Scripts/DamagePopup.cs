using System;
using UnityEngine;

/// <summary>
/// 떠오르면서 사라지는 피해 숫자.
/// </summary>
[RequireComponent(typeof(TextMesh))]
public class DamagePopup : MonoBehaviour
{
    [Tooltip("떠오르는 속도(units/s).")]
    [SerializeField] float riseSpeed = 1f;
    [Tooltip("표시 시간(초).")]
    [SerializeField] float lifetime = 0.5f;
    [SerializeField] int sortingOrder = 20;

    public Action<DamagePopup> Finished;

    TextMesh text;
    Color baseColor;
    float elapsed;

    void Awake()
    {
        text = GetComponent<TextMesh>();
        baseColor = text.color;

        // M6에서 TMP로 UI를 정리할 때 함께 바꿔도 된다. 숫자만 쓰니 지금은 내장 폰트로 충분.
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.font = font;

        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = font.material;
        meshRenderer.sortingOrder = sortingOrder;
    }

    public void Show(Vector2 position, float damage)
    {
        transform.position = position;
        text.text = Mathf.RoundToInt(damage).ToString();
        text.color = baseColor;
        elapsed = 0f;
        gameObject.SetActive(true);
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        if (elapsed >= lifetime)
        {
            gameObject.SetActive(false);
            Finished?.Invoke(this);
            return;
        }

        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

        Color color = baseColor;
        color.a *= 1f - elapsed / lifetime;
        text.color = color;
    }
}
