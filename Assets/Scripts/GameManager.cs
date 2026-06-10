using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum StateEnum { Calibrating, MainMenu, PlayingPlane, PlayingBall, GameOver, Victory }
    public StateEnum State { get; private set; } = StateEnum.Calibrating;

    [Header("Панели")]
    public GameObject calibrationPanel;
    public Text calibrationText;
    public GameObject mainMenuPanel;
    public GameObject planeGamePanel;
    public GameObject ballGamePanel;
    public GameObject planeGameOverPanel;
    public GameObject planeVictoryPanel;
    public GameObject ballGameOverPanel;
    public GameObject ballVictoryPanel;
    public Text scoreText;
    public Text planeGameOverScoreText;
    public Text planeVictoryScoreText;
    public Text ballGameOverScoreText;
    public Text ballVictoryScoreText;

    [Header("Самолёт")]
    public ObstacleSpawner spawner;
    public PlayerController player;
    public int winScore = 1000;

    [Header("Жизни")]
    public int maxLives = 3;
    public float invulnDuration = 1.5f;
    public Image livesImage;
    public Sprite[] livesSprites;   // [0] = 0 ламп, [1] = 1 лампа, [2] = 2, [3] = 3

    [Header("Мяч")]
    public BallGameManager ballGame;

    public int Score { get; private set; }
    private float _scoreTimer;
    private SerialGloveReader _glove;
    private MainMenuController _menu;
    private int _currentLives;
    private float _invulnTimer;

    private void Awake()
    {
        Instance = this;
        _glove = FindAnyObjectByType<SerialGloveReader>();
        _menu = FindAnyObjectByType<MainMenuController>();
    }

    private void Start()
    {
        HideAll();
        calibrationPanel?.SetActive(true);

        if (_glove != null && _glove.Connect())
            StartCoroutine(CalibrationRoutine());
        else
            ShowMainMenu();
    }

    private System.Collections.IEnumerator CalibrationRoutine()
    {
        State = StateEnum.Calibrating;
        yield return StartCoroutine(_glove.CalibrateRoutine(msg =>
        {
            if (calibrationText) calibrationText.text = msg;
        }));
        calibrationPanel?.SetActive(false);
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        HideAll();
        State = StateEnum.MainMenu;
        mainMenuPanel?.SetActive(true);
        if (livesImage) livesImage.gameObject.SetActive(false);
        _menu?.Activate();
    }

    public void StartPlaneGame()
    {
        HideAll();
        State = StateEnum.PlayingPlane;
        planeGamePanel?.SetActive(true);
        Score = 0; _scoreTimer = 0f;
        _currentLives = 0;
        _invulnTimer = 0f;
        UpdateLivesSprite();
        if (livesImage) livesImage.gameObject.SetActive(true);
        if (player != null) player.gameObject.SetActive(true);
        spawner?.ClearAll();
        spawner?.StartSpawning();
    }

    public void StartBallGame()
    {
        HideAll();
        State = StateEnum.PlayingBall;
        ballGamePanel?.SetActive(true);
        Score = 0; _scoreTimer = 0f;
        _currentLives = 0;
        _invulnTimer = 0f;
        UpdateLivesSprite();
        if (livesImage) livesImage.gameObject.SetActive(true);
        ballGame?.StartGame();
    }

    public void OnPlayerHit()
    {
        if (State != StateEnum.PlayingPlane) return;

        // Неуязвимость после получения урона
        if (_invulnTimer > 0f) return;

        _currentLives++;
        _invulnTimer = invulnDuration;
        UpdateLivesSprite();

        if (_currentLives >= maxLives)
        {
            State = StateEnum.GameOver;
            spawner?.StopSpawning();
            spawner?.ClearAll();
            if (player != null) player.gameObject.SetActive(false);
            ShowPlaneGameOver();
        }
    }

    private void UpdateLivesSprite()
    {
        if (livesImage != null && livesSprites != null && _currentLives < livesSprites.Length)
            livesImage.sprite = livesSprites[_currentLives];
    }

    public void OnBallGameOver(bool won)
    {
        if (State != StateEnum.PlayingBall) return;
        State = won ? StateEnum.Victory : StateEnum.GameOver;
        if (won)
            ShowBallVictory();
        else
            ShowBallGameOver();
    }

    /// <summary>
    /// Промах в игре с мячом — теряем жизнь.
    /// </summary>
    public void OnBallMiss()
    {
        if (State != StateEnum.PlayingBall) return;

        _currentLives++;
        _invulnTimer = invulnDuration;
        UpdateLivesSprite();

        if (_currentLives >= maxLives)
        {
            State = StateEnum.GameOver;
            ShowBallGameOver();
        }
    }

    private void ShowPlaneGameOver()
    {
        planeGameOverPanel?.SetActive(true);
        if (planeGameOverScoreText) planeGameOverScoreText.text = $"Счёт: {Score}";
    }

    private void ShowPlaneVictory()
    {
        State = StateEnum.Victory;
        _invulnTimer = 0f;
        spawner?.StopSpawning();
        spawner?.ClearAll();
        if (player != null) player.gameObject.SetActive(false);
        planeVictoryPanel?.SetActive(true);
        if (planeVictoryScoreText) planeVictoryScoreText.text = $"Победа! Счёт: {Score}";
    }

    private void ShowBallGameOver()
    {
        ballGameOverPanel?.SetActive(true);
        if (ballGameOverScoreText) ballGameOverScoreText.text = $"Счёт: {(ballGame != null ? ballGame.Score : Score)}";
    }

    private void ShowBallVictory()
    {
        ballVictoryPanel?.SetActive(true);
        if (ballVictoryScoreText) ballVictoryScoreText.text = $"Победа! Счёт: {(ballGame != null ? ballGame.Score : Score)}";
    }

    public void AddScore(int pts)
    {
        Score += pts;
    }

    private void Update()
    {
        // Таймер неуязвимости + мигание
        if (_invulnTimer > 0f)
        {
            _invulnTimer -= Time.deltaTime;
            // Мигание игрока: каждый 0.1 сек переключаем видимость
            if (player != null)
            {
                bool visible = Mathf.FloorToInt(_invulnTimer * 10f) % 2 == 0;
                player.gameObject.SetActive(visible);
            }
        }
        else if (player != null && !player.gameObject.activeSelf && State == StateEnum.PlayingPlane)
        {
            player.gameObject.SetActive(true);
        }

        // Счёт только в plane-игре (ball управляет сам)
        if (State == StateEnum.PlayingPlane)
        {
            _scoreTimer += Time.deltaTime;
            int newScore = Mathf.FloorToInt(_scoreTimer * 10f);
            if (newScore != Score) AddScore(newScore - Score);
            if (scoreText) scoreText.text = $"Счёт: {Score}";

            // Проверка победы
            if (Score >= winScore)
                ShowPlaneVictory();
        }

        // Возврат в меню после Game Over / Victory
        if ((State == StateEnum.GameOver || State == StateEnum.Victory) && EnterPressed())
            ShowMainMenu();
    }

    private static bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current.enterKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Return);
#endif
    }

    private void HideAll()
    {
        if (calibrationPanel) calibrationPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (planeGamePanel) planeGamePanel.SetActive(false);
        if (ballGamePanel) ballGamePanel.SetActive(false);
        if (planeGameOverPanel) planeGameOverPanel.SetActive(false);
        if (planeVictoryPanel) planeVictoryPanel.SetActive(false);
        if (ballGameOverPanel) ballGameOverPanel.SetActive(false);
        if (ballVictoryPanel) ballVictoryPanel.SetActive(false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
