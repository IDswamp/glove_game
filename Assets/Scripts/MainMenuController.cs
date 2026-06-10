using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Панели")]
    public GameObject planePanel;
    public GameObject ballPanel;

    [Header("Курсор")]
    public RectTransform cursor;
    public float cursorSpeed = 400f;

    [Header("Логотипы")]
    public Image planeLogo;
    public Sprite planeOnSprite;
    public Sprite planeOffSprite;

    public Image ballLogo;
    public Sprite ballOnSprite;
    public Sprite ballOffSprite;

    [Header("Свечение (вкл/выкл при выборе)")]
    public GameObject planeGlow;
    public GameObject ballGlow;

    private SerialGloveReader _glove;
    private bool _active;
    private int _selected;
    private float _minX = -350f, _maxX = 350f;
    private int _frameCount;
    private bool _wasSqueezed; // фронт сжатия (расслабился→сжал)

    private void Start()
    {
        _glove = FindAnyObjectByType<SerialGloveReader>();
        // Авто-активация если панель включена
        if (gameObject.activeInHierarchy)
            Activate();
    }

    private void OnEnable()
    {
        // Активируемся когда панель включается
        Activate();
    }

    public void Activate()
    {
        _active = true;
        _wasSqueezed = true; // ждём что рука сначала расслабится
        if (_glove == null) _glove = FindAnyObjectByType<SerialGloveReader>();
        if (cursor != null) cursor.anchoredPosition = Vector2.zero;
        _selected = 0;
        UpdateHighlights();
        Debug.Log("[Menu] Активирован. Курсор: " + (cursor != null ? "OK" : "NULL!") + " Glove: " + (_glove != null && _glove.IsCalibrated ? "OK" : "нет"));
    }

    private void Update()
    {
        if (!_active) return;
        if (cursor == null) { Debug.LogWarning("[Menu] cursor не назначен в инспекторе!"); return; }

        float inputX = 0f;
        if (_glove != null && _glove.IsCalibrated)
        {
            inputX = _glove.SmoothedAy;
        }
        else
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) inputX = -1f;
            else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) inputX = 1f;
#else
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) inputX = -1f;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) inputX = 1f;
#endif
        }

        Vector2 cp = cursor.anchoredPosition;
        cp.x += inputX * cursorSpeed * Time.deltaTime;
        cp.x = Mathf.Clamp(cp.x, _minX, _maxX);
        cursor.anchoredPosition = cp;

        _selected = cp.x < 0f ? 0 : 1;
        UpdateHighlights();

        // Отладка: все пальцы каждые 60 кадров
        if (_glove != null && _glove.IsCalibrated && ++_frameCount % 60 == 0)
            Debug.Log($"[Menu] v0={_glove.Finger0:F2} v1={_glove.Finger1:F2} v2={_glove.Finger2:F2} v3={_glove.Finger3:F2} v4={_glove.Finger4:F2}");

        // Выбор: сжатие по фронту (расслабился → сжал)
        bool squeezed = _glove != null && _glove.IsCalibrated && _glove.V0Pressure > 0.3f;
        bool select = squeezed && !_wasSqueezed;
        _wasSqueezed = squeezed;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current.enterKey.wasPressedThisFrame)
            select = true;
#else
        if (Input.GetKeyDown(KeyCode.Return))
            select = true;
#endif

        if (select)
        {
            _active = false;
            if (_selected == 0)
                GameManager.Instance?.StartPlaneGame();
            else
                GameManager.Instance?.StartBallGame();
        }
    }

    private void UpdateHighlights()
    {
        if (planeLogo != null)
            planeLogo.sprite = _selected == 0 ? planeOnSprite : planeOffSprite;
        if (ballLogo != null)
            ballLogo.sprite = _selected == 1 ? ballOnSprite : ballOffSprite;

        if (planeGlow != null)
            planeGlow.SetActive(_selected == 0);
        if (ballGlow != null)
            ballGlow.SetActive(_selected == 1);
    }
}
