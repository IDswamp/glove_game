using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Выводит на экран степень сжатия всех пальцев.
/// </summary>
public class FingerDebugDisplay : MonoBehaviour
{
    public Text debugText;
    private SerialGloveReader _glove;

    private void Start()
    {
        _glove = FindAnyObjectByType<SerialGloveReader>();
    }

    private void Update()
    {
        if (_glove == null || debugText == null) return;

        debugText.text =
            $"v0: {_glove.Finger0:F2}  " +
            $"v1: {_glove.Finger1:F2}  " +
            $"v2: {_glove.Finger2:F2}  " +
            $"v3: {_glove.Finger3:F2}  " +
            $"v4: {_glove.Finger4:F2}";
    }
}
