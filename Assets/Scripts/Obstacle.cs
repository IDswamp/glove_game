using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class Obstacle : MonoBehaviour
{
    public float speed = 200f;

    [Header("Коллизия")]
    [Tooltip("Множитель хитбокса: 1.0 = полный размер, 0.5 = половина")]
    [Range(0.1f, 2f)]
    public float hitboxScale = 0.75f;

    [Header("Анимация — покачивание (НЛО, тучи)")]
    public float bobAmplitude = 0f;   // амплитуда вверх-вниз
    public float bobFrequency = 1f;   // скорость покачивания

    [Header("Анимация — перекаты (НЛО)")]
    public float rollAmplitude = 0f;  // макс. угол поворота в градусах
    public float rollFrequency = 1f;

    [Header("Анимация — тряска (птица)")]
    public float shakeAmount = 0f;    // сила случайного дрожания
    public float shakeSpeed = 10f;

    private RectTransform _rt;
    private RectTransform _playerRt;
    private float _animTime;
    private Vector2 _basePosition;
    private bool _baseSet;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _animTime = Random.Range(0f, 100f); // случайная фаза
    }

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerRt = player.GetComponent<RectTransform>();
    }

    private void Update()
    {
        _animTime += Time.deltaTime;

        // Горизонтальное движение
        if (!_baseSet)
        {
            _basePosition = _rt.anchoredPosition;
            _baseSet = true;
        }
        _basePosition.x -= speed * Time.deltaTime;

        // Покачивание (синус по Y)
        float bobOffset = bobAmplitude * Mathf.Sin(_animTime * bobFrequency * Mathf.PI * 2f);

        // Перекаты (синус по Z-вращению)
        float rollAngle = rollAmplitude * Mathf.Sin(_animTime * rollFrequency * Mathf.PI * 2f);

        // Тряска (случайное смещение по X/Y каждый кадр)
        float shakeX = shakeAmount * (Mathf.PerlinNoise(_animTime * shakeSpeed, 0f) - 0.5f) * 2f;
        float shakeY = shakeAmount * (Mathf.PerlinNoise(0f, _animTime * shakeSpeed) - 0.5f) * 2f;

        _rt.anchoredPosition = new Vector2(_basePosition.x + shakeX, _basePosition.y + bobOffset + shakeY);
        _rt.localRotation = Quaternion.Euler(0f, 0f, rollAngle);

        // Ручная проверка столкновения
        if (_playerRt != null && RectOverlap(_rt, _playerRt, hitboxScale))
        {
            GameManager.Instance?.OnPlayerHit();
        }

        if (_basePosition.x < -240f)
            Destroy(gameObject);
    }

    /// <summary>
    /// Проверяет пересечение двух RectTransform с учётом pivot и масштаба хитбокса.
    /// </summary>
    private static bool RectOverlap(RectTransform a, RectTransform b, float scale = 1f)
    {
        // Получаем мировые углы rect'ов
        Vector3[] aCorners = new Vector3[4];
        Vector3[] bCorners = new Vector3[4];
        a.GetWorldCorners(aCorners);
        b.GetWorldCorners(bCorners);

        // Вычисляем центры и размеры в мировых координатах
        Vector2 aCenter = (aCorners[0] + aCorners[2]) / 2f;
        Vector2 bCenter = (bCorners[0] + bCorners[2]) / 2f;

        Vector2 aSize = new Vector2(
            Mathf.Abs(aCorners[2].x - aCorners[0].x),
            Mathf.Abs(aCorners[2].y - aCorners[0].y)
        ) * scale;

        Vector2 bSize = new Vector2(
            Mathf.Abs(bCorners[2].x - bCorners[0].x),
            Mathf.Abs(bCorners[2].y - bCorners[0].y)
        ) * scale;

        return Mathf.Abs(aCenter.x - bCenter.x) < (aSize.x + bSize.x) / 2f
            && Mathf.Abs(aCenter.y - bCenter.y) < (aSize.y + bSize.y) / 2f;
    }
}
