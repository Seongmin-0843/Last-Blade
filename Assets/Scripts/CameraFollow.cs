using UnityEngine;

/// <summary>
/// 플레이어를 부드럽게 따라가되, 맵 밖이 보이지 않도록 경계에서 멈춘다. (기획서 §3)
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] MapBounds bounds;

    [Tooltip("작을수록 빠르게 따라붙는다.")]
    [SerializeField] float smoothTime = 0.15f;

    Camera cam;
    Vector2 velocity;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (target == null || bounds == null) return;

        // 화면 절반만큼 안쪽으로 가두면 맵 밖이 안 보인다
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector2 smoothed = Vector2.SmoothDamp(transform.position, target.position, ref velocity, smoothTime);
        Vector2 clamped = bounds.Clamp(smoothed, new Vector2(halfW, halfH));

        transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
    }
}
