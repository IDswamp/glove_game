using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скроллинг фонового RawImage. Текстура должна быть с Wrap Mode = Repeat.
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("Фон")]
    public RawImage backgroundImage;

    [Header("Скорость скролла")]
    public float scrollSpeed = 20f;

    [Header("Тайлинг")]
    [Tooltip("Во сколько раз текстура повторяется по горизонтали")]
    public float tileX = 2f;
    [Tooltip("Во сколько раз текстура повторяется по вертикали")]
    public float tileY = 2f;

    private void Start()
    {
        if (backgroundImage != null && backgroundImage.texture != null)
        {
            // Настраиваем тайлинг UV
            Rect uv = backgroundImage.uvRect;
            uv.width = tileX;
            uv.height = tileY;
            backgroundImage.uvRect = uv;
        }
    }

    private void Update()
    {
        if (backgroundImage != null)
        {
            Rect uv = backgroundImage.uvRect;
            uv.x += scrollSpeed * Time.deltaTime * 0.01f;
            backgroundImage.uvRect = uv;
        }
    }
}
