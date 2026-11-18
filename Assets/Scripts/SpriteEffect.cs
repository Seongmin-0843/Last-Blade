using System;
using UnityEngine;

/// <summary>
/// 스프라이트 프레임을 한 번 재생하고 꺼지는 일회성 이펙트. 슬래시 궤적, 처치 이펙트에 쓴다.
/// </summary>
public class SpriteEffect : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Sprite[] frames;
    [Tooltip("초당 프레임 수.")]
    [SerializeField] float fps = 20f;

    public Action<SpriteEffect> Finished;

    float elapsed;

    /// <summary>전체 프레임이 duration초에 맞춰 재생되도록 속도를 정하고 재생한다.</summary>
    public void Play(float duration)
    {
        fps = frames.Length / duration;
        Play();
    }

    public void Play()
    {
        elapsed = 0f;
        sprite.sprite = frames[0];
        gameObject.SetActive(true);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        int frame = (int)(elapsed * fps);

        if (frame < frames.Length)
        {
            sprite.sprite = frames[frame];
            return;
        }

        gameObject.SetActive(false);
        Finished?.Invoke(this);
    }
}
