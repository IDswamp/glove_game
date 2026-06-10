using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Параллакс-слой: бесконечный скролл RawImage.
/// Вешается на каждый слой фона (город, облака и т.д.).
/// Текстура должна иметь Wrap Mode = Repeat.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [Header("Скорость")]
    [Tooltip("Горизонтальная скорость скролла (0 = стоит на месте)")]
    public float scrollSpeedX = 30f;

    [Tooltip("Вертикальная скорость (0 для города)")]
    public float scrollSpeedY = 0f;

    [Header("Тайлинг")]
    [Tooltip("Сколько раз текстура повторяется по X")]
    public float tileX = 1f;

    [Tooltip("Сколько раз текстура повторяется по Y")]
    public float tileY = 1f;

    [Header("Случайный сдвиг")]
    [Tooltip("Случайно сдвинуть UV при старте, чтобы облака не были синхронны")]
    public bool randomOffset = true;

    private RawImage _image;
    private Rect _uv;

    private void Awake()
    {
        _image = GetComponent<RawImage>();
        if (_image == null)
        {
            Debug.LogWarning($"[ParallaxLayer] Нет RawImage на {name}", this);
            return;
        }

        _uv = _image.uvRect;
        _uv.width = tileX;
        _uv.height = tileY;

        if (randomOffset)
            _uv.x = Random.Range(0f, 1f);

        _image.uvRect = _uv;
    }

    private void Update()
    {
        if (_image == null) return;

        _uv.x += scrollSpeedX * Time.deltaTime * 0.01f;
        _uv.y += scrollSpeedY * Time.deltaTime * 0.01f;
        _image.uvRect = _uv;
    }

    /// <summary>
    /// Позволяет менять скорость извне (например, ускорять фон со временем).
    /// </summary>
    public void SetSpeed(float x, float y)
    {
        scrollSpeedX = x;
        scrollSpeedY = y;
    }
}
