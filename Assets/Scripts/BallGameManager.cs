using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Мяч летит к кольцу. Цвет мяча меняется: красный → зелёный → красный.
/// Сжать руку когда мяч зелёный = поймал.
/// Промах = -1 жизнь. 10 попаданий = победа.
/// </summary>
public class BallGameManager : MonoBehaviour
{
    [Header("Объекты")]
    public GameObject ballPrefab;
    public RectTransform hoop;              // кольцо-цель (статичное)

    [Header("UI")]
    public Text scoreText;
    public Text resultText;

    [Header("Параметры")]
    public float spawnInterval = 1.5f;      // пауза между мячами
    public float flightDuration = 2f;       // время полёта мяча до кольца
    public float catchWindow = 0.25f;       // ширина окна попадания (0..1, чем больше тем легче)
    public int winScore = 10;               // очков для победы
    public float perfectTiming = 0.65f;     // момент идеального тайминга (0..1)

    public int Score { get; private set; }

    private SerialGloveReader _glove;
    private RectTransform _area;
    private RectTransform _ballRt;
    private Image _ballImage;
    private float _spawnTimer;
    private float _flightTimer;
    private bool _running;
    private bool _wasSqueezed;
    private Vector2 _ballStartPos;
    private Vector2 _ballTargetPos;

    private void Start()
    {
        _area = GetComponent<RectTransform>();
        _glove = FindAnyObjectByType<SerialGloveReader>();
    }

    public void StartGame()
    {
        _running = true;
        Score = 0;
        _spawnTimer = 0f;
        _wasSqueezed = true;
        if (resultText) resultText.text = "";
        UpdateScore();
        ClearBall();
    }

    public void StopGame()
    {
        _running = false;
        ClearBall();
    }

    private void OnDisable()
    {
        StopGame();
    }

    private void Update()
    {
        if (!_running) return;

        // Остановка если игра закончилась (извне)
        if (GameManager.Instance != null &&
            GameManager.Instance.State != GameManager.StateEnum.PlayingBall)
        {
            StopGame();
            return;
        }

        // Ждём спавна
        if (_ballRt == null)
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= spawnInterval) { _spawnTimer = 0f; SpawnBall(); }
            return;
        }

        // Мяч летит
        _flightTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_flightTimer / flightDuration);
        _ballRt.anchoredPosition = Vector2.Lerp(_ballStartPos, _ballTargetPos, t);

        // Цвет мяча: красный → зелёный (у perfectTiming) → красный
        if (_ballImage != null)
        {
            float distFromPerfect = Mathf.Abs(t - perfectTiming) / Mathf.Max(catchWindow, 0.01f);
            float greenness = 1f - Mathf.Clamp01(distFromPerfect);
            _ballImage.color = Color.Lerp(Color.red, Color.green, greenness);
        }

        // Мяч долетел — промах (опоздал)
        if (t >= 1f)
        {
            OnMiss(closeness: 0f, tooEarly: false);
            return;
        }

        // Сжатие = попытка поймать
        bool squeezed = _glove != null && _glove.IsCalibrated && _glove.V0Pressure > 0.3f;
        bool shoot = squeezed && !_wasSqueezed;
        _wasSqueezed = squeezed;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current.spaceKey.wasPressedThisFrame) shoot = true;
#else
        if (Input.GetKeyDown(KeyCode.Space)) shoot = true;
#endif

        if (shoot) OnCatchAttempt(t);
    }

    private void SpawnBall()
    {
        if (ballPrefab == null || _area == null || hoop == null) return;

        // Случайная точка на границе экрана
        float w = _area.rect.width;
        float h = _area.rect.height;
        float margin = 20f; // небольшой отступ от края
        int edge = Random.Range(0, 4);
        Vector2 startPos;
        switch (edge)
        {
            case 0: startPos = new Vector2(Random.Range(0f, w), h + margin); break;
            case 1: startPos = new Vector2(w + margin, Random.Range(0f, h)); break;
            case 2: startPos = new Vector2(Random.Range(0f, w), -margin); break;
            default: startPos = new Vector2(-margin, Random.Range(0f, h)); break;
        }

        _ballStartPos = startPos;
        // Позиция корзины в локальных координатах _area
        _ballTargetPos = _area.InverseTransformPoint(hoop.position);
        _flightTimer = 0f;

        GameObject obj = Instantiate(ballPrefab, _area);
        _ballRt = obj.GetComponent<RectTransform>();
        _ballImage = obj.GetComponent<Image>();
        _ballRt.anchorMin = _ballRt.anchorMax = _ballRt.pivot = new Vector2(0.5f, 0.5f);
        _ballRt.anchoredPosition = startPos;
        _ballRt.sizeDelta = hoop.sizeDelta * 0.8f; // мяч чуть меньше кольца
    }

    private void OnCatchAttempt(float t)
    {
        float dist = Mathf.Abs(t - perfectTiming);
        float quality = 1f - Mathf.Clamp01(dist / catchWindow); // 1 = идеально, 0 = край окна

        if (quality > 0f) // попал в окно
        {
            Score++;
            UpdateScore();

            string grade;
            Color gradeColor;
            if (quality > 0.9f)      { grade = "Perfect!";   gradeColor = new Color(0f, 1f, 0.3f); }
            else if (quality > 0.7f) { grade = "Great!";     gradeColor = new Color(0.4f, 1f, 0.2f); }
            else if (quality > 0.4f) { grade = "Good";       gradeColor = new Color(0.8f, 1f, 0.2f); }
            else                     { grade = "Ok";         gradeColor = new Color(1f, 0.9f, 0.3f); }

            ShowResult(grade, gradeColor, $" ({Score}/{winScore})");

            ClearBall();

            if (Score >= winScore)
                GameManager.Instance?.OnBallGameOver(true);
        }
        else // слишком рано или поздно
        {
            float closeness = 1f - Mathf.Clamp01(dist / (catchWindow * 2f)); // насколько близко к окну
            OnMiss(closeness, t < perfectTiming);
        }
    }

    private void OnMiss(float closeness = 0f, bool tooEarly = false)
    {
        ClearBall();

        string msg;
        Color msgColor;
        if (closeness > 0.7f)
        {
            msg = tooEarly ? "So close! A bit later..." : "Almost! A bit earlier...";
            msgColor = new Color(1f, 0.6f, 0.1f); // оранжевый
        }
        else if (closeness > 0.3f)
        {
            msg = tooEarly ? "Too early!" : "Too late!";
            msgColor = new Color(1f, 0.4f, 0.2f);
        }
        else
        {
            msg = "Too bad!";
            msgColor = new Color(0.9f, 0.2f, 0.2f); // красный
        }

        ShowResult(msg, msgColor);
        GameManager.Instance?.OnBallMiss();
    }

    private void ShowResult(string msg, Color color, string suffix = "")
    {
        if (resultText != null)
        {
            resultText.text = msg + suffix;
            resultText.color = color;
        }
    }

    private void ClearBall()
    {
        if (_ballRt != null) Destroy(_ballRt.gameObject);
        _ballRt = null;
    }

    private void UpdateScore()
    {
        if (scoreText) scoreText.text = $"Счёт: {Score}";
    }
}

