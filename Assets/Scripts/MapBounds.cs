using UnityEngine;

/// <summary>
/// 맵의 사각 경계. 플레이어와 카메라가 같은 값을 공유한다.
/// 이 오브젝트의 위치가 맵 중심.
/// </summary>
public class MapBounds : MonoBehaviour
{
    [Tooltip("맵 전체 크기(units). 화면이 20 x 11.25 이므로 기획서의 1.5배 규칙 기준 30 x 18.")]
    [SerializeField] Vector2 size = new Vector2(30f, 18f);

    public Vector2 Size => size;

    /// <summary>pos를 맵 안으로 가둔다. inset은 안쪽으로 더 밀어넣을 여유(콜라이더 반지름, 화면 절반 등).</summary>
    public Vector2 Clamp(Vector2 pos, Vector2 inset)
    {
        Vector2 center = transform.position;

        // inset이 맵 절반보다 크면(= 가둘 공간이 없으면) 중심에 고정. 안 그러면 min > max가 되어 값이 튄다.
        float halfX = Mathf.Max(0f, size.x * 0.5f - inset.x);
        float halfY = Mathf.Max(0f, size.y * 0.5f - inset.y);

        return new Vector2(
            Mathf.Clamp(pos.x, center.x - halfX, center.x + halfX),
            Mathf.Clamp(pos.y, center.y - halfY, center.y + halfY));
    }

    // 씬 뷰에 경계를 그려준다. 타일을 어디까지 칠해야 하는지 이걸 보고 맞추면 된다.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, size);
    }
}
