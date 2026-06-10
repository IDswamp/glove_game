using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Индикатор тайминга как в osu! — круг сужается к точке ловли.
/// </summary>
public class TimingIndicator : MonoBehaviour
{
    private RectTransform _rt;
    private Image _img;
    private float _catchX;
    private float _catchY;
    private float _maxRadius;

    public void Init(float catchX, float catchY, float maxRadius)
    {
        _catchX = catchX;
        _catchY = catchY;
        _maxRadius = maxRadius;

        _rt = GetComponent<RectTransform>();
        _rt.anchorMin = _rt.anchorMax = _rt.pivot = Vector2.zero;
        _rt.anchoredPosition = new Vector2(catchX, catchY);
        _rt.sizeDelta = Vector2.one * maxRadius * 2f;

        _img = GetComponent<Image>();
        _img.sprite = null; // используем заливку цветом
        _img.color = new Color(1f, 1f, 1f, 0.5f);
    }

    public void UpdateIndicator(float distance, float speed)
    {
        if (_rt == null) return;

        // Размер круга зависит от расстояния мяча до зоны ловли
        float maxDist = 200f;
        float t = Mathf.Clamp01(1f - Mathf.Abs(distance) / maxDist);

        // Круг увеличивается по мере приближения мяча
        float size = Mathf.Lerp(10f, _maxRadius * 2f, t);
        _rt.sizeDelta = new Vector2(size, size);

        // Цвет: зелёный когда близко, красный когда далеко
        float quality = Mathf.Clamp01(1f - Mathf.Abs(distance) / 80f);
        _img.color = new Color(1f - quality, quality, 0.3f, 0.7f);
    }
}
