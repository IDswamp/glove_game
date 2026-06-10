using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    public float verticalSpeed = 300f;
    public float horizontalSpeed = 250f;

    [Header("Наклон")]
    [Tooltip("Максимальный угол наклона носа в градусах")]
    public float maxTilt = 12f;
    [Tooltip("Скорость возврата в исходное положение")]
    public float tiltSmooth = 8f;

    private SerialGloveReader _glove;
    private RectTransform _rt;
    private RectTransform _parentRt;
    private float _halfW, _halfH;
    private bool _useGlove;
    private float _currentTilt;

    public RectTransform RectTransform => _rt;

    private void Start()
    {
        _rt = GetComponent<RectTransform>();
        _parentRt = (RectTransform)_rt.parent;
        _glove = FindAnyObjectByType<SerialGloveReader>();
        _useGlove = _glove != null && _glove.IsConnected;
        _halfW = _rt.rect.width / 2f;
        _halfH = _rt.rect.height / 2f;

        Debug.Log(_useGlove ? "[Player] Перчатка" : "[Player] WASD ↑↓←→");
    }

    private static bool UpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed;
#else
        return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
#endif
    }

    private static bool DownPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed;
#else
        return Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
#endif
    }

    private static bool LeftPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
#endif
    }

    private static bool RightPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed;
#else
        return Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
#endif
    }

    private void Update()
    {
        float inputY = 0f, inputX = 0f;

        if (_useGlove && _glove != null && _glove.IsCalibrated)
        {
            inputY = _glove.SmoothedAx;
            inputX = _glove.SmoothedAy;
        }
        else
        {
            if (UpPressed()) inputY = 1f;
            else if (DownPressed()) inputY = -1f;
            if (LeftPressed()) inputX = -1f;
            else if (RightPressed()) inputX = 1f;
        }

        float parentW = _parentRt.rect.width;
        float parentH = _parentRt.rect.height;
        float leftBound = _halfW + 10f;
        float rightBound = parentW - _halfW - 10f;
        float topBound = parentH - _halfH - 10f;
        float bottomBound = _halfH + 10f;

        Vector2 ap = _rt.anchoredPosition;
        ap.x += inputX * horizontalSpeed * Time.deltaTime;
        ap.y += inputY * verticalSpeed * Time.deltaTime;
        ap.x = Mathf.Clamp(ap.x, leftBound, rightBound);
        ap.y = Mathf.Clamp(ap.y, bottomBound, topBound);
        _rt.anchoredPosition = ap;

        // Наклон носа по вертикальному движению
        float targetTilt = -inputY * maxTilt; // вверх = нос вверх (положительный Z-поворот)
        _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, tiltSmooth * Time.deltaTime);
        _rt.localRotation = Quaternion.Euler(0f, 0f, _currentTilt);
    }
}
