using System;
using System.Reflection;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SerialGloveReader : MonoBehaviour
{
    [Header("COM")]
    public string portName = "COM11";
    public int baudRate = 9600;
    public int readTimeout = 10;

    [Header("Калибровка")]
    public int calibrationSamples = 60;
    public int rangeAx = 1500;
    public int rangeAy = 1500;

    [Header("Сглаживание")]
    public float smoothFactor = 0.85f;

    public float SmoothedAx { get; private set; }
    public float SmoothedAy { get; private set; }
    public float V0Pressure { get; private set; }

    // Пальцы: сырые + нормализованные (0..1)
    public float Finger0 { get; private set; }
    public float Finger1 { get; private set; }
    public float Finger2 { get; private set; }
    public float Finger3 { get; private set; }
    public float Finger4 { get; private set; }

    // Сырые значения для отладки
    public float Raw0 { get; private set; }
    public float Raw1 { get; private set; }
    public float Raw2 { get; private set; }
    public float Raw3 { get; private set; }
    public float Raw4 { get; private set; }

    private static Type _spType, _sbType, _pType;
    private object _serial;
    private int _axN, _ayN;
    private bool _calibrated;
    private float _targetAx, _targetAy;
    private float _targetF0, _targetF1, _targetF2, _targetF3, _targetF4;

    // Калибровка пальцев: покой / сжатие (средние)
    private float[] _fRest = { 1000, 2725, 0, 2700, 2690 };
    private float[] _fSqueeze = { 760, 2690, 0, 2600, 2580 };
    private bool _fingersCalibrated = true; // дефолты уже заданы

    public bool IsConnected { get; private set; }
    public bool IsCalibrated => _calibrated;

    private static uint Crc32(byte[] d, int len)
    {
        uint crc = 0xFFFFFFFF, poly = 0x04C11DB7;
        for (int i = 0; i < len; i += 4)
        {
            uint w = (uint)(d[i] | (d[i + 1] << 8) | (d[i + 2] << 16) | (d[i + 3] << 24));
            crc ^= w;
            for (int b = 0; b < 32; b++)
                crc = (crc & 0x80000000) != 0 ? ((crc << 1) ^ poly) & 0xFFFFFFFF : (crc << 1) & 0xFFFFFFFF;
        }
        return crc;
    }

    private void Awake()
    {
        if (_spType != null) return;
        try { var a = Assembly.Load("System"); _spType = a.GetType("System.IO.Ports.SerialPort"); _sbType = a.GetType("System.IO.Ports.StopBits"); _pType = a.GetType("System.IO.Ports.Parity"); }
        catch { try { var a = Assembly.Load("System.IO.Ports"); _spType = a.GetType("System.IO.Ports.SerialPort"); _sbType = a.GetType("System.IO.Ports.StopBits"); _pType = a.GetType("System.IO.Ports.Parity"); } catch { } }
    }

    public bool Connect()
    {
        Awake();
        if (_spType == null) { Debug.Log("[Glove] DLL нет."); return false; }
        try
        {
            _serial = Activator.CreateInstance(_spType, portName, baudRate, Enum.ToObject(_pType, 0), 8, Enum.ToObject(_sbType, 1));
            _spType.GetProperty("ReadTimeout")?.SetValue(_serial, readTimeout);
            _spType.GetMethod("Open")?.Invoke(_serial, null);
            IsConnected = true;
            Debug.Log($"[Glove] {portName} открыт");
            return true;
        }
        catch (Exception e) { Debug.LogWarning($"[Glove] {e.Message}"); return false; }
    }

    private void OnDestroy()
    {
        if (_serial == null) return;
        try { if ((bool)(_spType.GetProperty("IsOpen")?.GetValue(_serial) ?? false)) _spType.GetMethod("Close")?.Invoke(_serial, null); } catch { }
    }

    private (short ax, short ay, uint v0, uint v1, uint v2, uint v3, uint v4)? ReadPacket()
    {
        if (_serial == null) return null;
        try { if (!(bool)(_spType.GetProperty("IsOpen")?.GetValue(_serial) ?? false)) return null; } catch { return null; }
        try
        {
            int avail = (int)(_spType.GetProperty("BytesToRead")?.GetValue(_serial) ?? 0);
            if (avail < 32) return null;
            var rm = _spType.GetMethod("Read", new[] { typeof(byte[]), typeof(int), typeof(int) });
            byte[] d = new byte[32];
            if ((int)(rm?.Invoke(_serial, new object[] { d, 0, 32 }) ?? 0) != 32) return null;
            if (avail > 64) _spType.GetMethod("DiscardInBuffer")?.Invoke(_serial, null);
            short ax = BitConverter.ToInt16(d, 0);
            short ay = BitConverter.ToInt16(d, 2);
            uint v0 = BitConverter.ToUInt32(d, 8);
            uint v1 = BitConverter.ToUInt32(d, 12);
            uint v2 = BitConverter.ToUInt32(d, 16);
            uint v3 = BitConverter.ToUInt32(d, 20);
            uint v4 = BitConverter.ToUInt32(d, 24);
            if (Crc32(d, 28) == BitConverter.ToUInt32(d, 28)) return (ax, ay, v0, v1, v2, v3, v4);
        }
        catch { }
        return null;
    }

    public System.Collections.IEnumerator CalibrateRoutine(System.Action<string> cb)
    {
        cb?.Invoke("Держите руку прямо.\nENTER = калибровать\nSPACE = пропустить");
        while (true)
        {
            if (EnterKey()) break;
            if (SpaceKey()) { _calibrated = true; cb?.Invoke("Пропущено."); yield break; }
            yield return null;
        }
        cb?.Invoke("Сбор данных...");
        long sa = 0, sy = 0; int n = 0;
        float timeout = 5f;
        while (n < calibrationSamples && timeout > 0)
        {
            var p = ReadPacket();
            if (p.HasValue) { sa += p.Value.ax; sy += p.Value.ay; n++; timeout = 5f; }
            else { timeout -= Time.deltaTime; }
            yield return null;
        }
        if (n == 0) { Debug.LogWarning("[Glove] Нет данных! Пропуск калибровки."); _calibrated = true; cb?.Invoke("Нет данных. ENTER для продолжения..."); while (!EnterKey()) yield return null; }
        if (n > 0) { _axN = (int)(sa / n); _ayN = (int)(sy / n); }
        _calibrated = true;
        cb?.Invoke($"OK! ax={_axN} ay={_ayN}");
        Debug.Log($"[Glove] axN={_axN} ayN={_ayN}");
    }

    /// Калибровка пальцев: запоминаем среднее в покое и при сжатии
    public System.Collections.IEnumerator CalibrateFingersRoutine(System.Action<string> cb)
    {
        // Пропустить калибровку пальцев: SPACE
        cb?.Invoke("Калибровка пальцев.\nENTER = калибровать\nSPACE = пропустить");
        while (true)
        {
            if (EnterKey()) break;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.Space))
#endif
            {
                Debug.Log("[Glove] Калибровка пальцев пропущена");
                yield break;
            }
            yield return null;
        }

        cb?.Invoke("РАССЛАБЬТЕ руку.\nENTER...");
        while (!EnterKey()) yield return null;

        cb?.Invoke("Сбор...");
        float sum0 = 0, sum1 = 0, sum2 = 0, sum3 = 0, sum4 = 0;
        int n = 0;
        for (int i = 0; i < calibrationSamples * 2; i++)
        {
            var p = ReadPacket();
            if (p.HasValue) { sum0 += p.Value.v0; sum1 += p.Value.v1; sum2 += p.Value.v2; sum3 += p.Value.v3; sum4 += p.Value.v4; n++; }
            yield return null;
        }
        float r0 = sum0 / Mathf.Max(1, n), r1 = sum1 / Mathf.Max(1, n), r2 = sum2 / Mathf.Max(1, n), r3 = sum3 / Mathf.Max(1, n), r4 = sum4 / Mathf.Max(1, n);

        cb?.Invoke("СОЖМИТЕ руку.\nENTER...");
        while (!EnterKey()) yield return null;

        cb?.Invoke("Сбор...");
        sum0 = sum1 = sum2 = sum3 = sum4 = 0; n = 0;
        for (int i = 0; i < calibrationSamples * 2; i++)
        {
            var p = ReadPacket();
            if (p.HasValue) { sum0 += p.Value.v0; sum1 += p.Value.v1; sum2 += p.Value.v2; sum3 += p.Value.v3; sum4 += p.Value.v4; n++; }
            yield return null;
        }
        float s0 = sum0 / Mathf.Max(1, n), s1 = sum1 / Mathf.Max(1, n), s2 = sum2 / Mathf.Max(1, n), s3 = sum3 / Mathf.Max(1, n), s4 = sum4 / Mathf.Max(1, n);

        _fRest[0] = r0; _fSqueeze[0] = s0;
        _fRest[1] = r1; _fSqueeze[1] = s1;
        _fRest[2] = r2; _fSqueeze[2] = s2;
        _fRest[3] = r3; _fSqueeze[3] = s3;
        _fRest[4] = r4; _fSqueeze[4] = s4;
        _fingersCalibrated = true;

        var msg = $"0: {r0:F0}→{s0:F0}\n1: {r1:F0}→{s1:F0}\n2: {r2:F0}→{s2:F0}\n3: {r3:F0}→{s3:F0}\n4: {r4:F0}→{s4:F0}";
        cb?.Invoke($"Готово!\n{msg}");
        Debug.Log($"[Glove] Fingers:\n{msg}");
    }

    private static float Avg(float[] arr) { return 0; } // unused now

    private static bool EnterKey()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current.enterKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Return);
#endif
    }

    private static bool SpaceKey()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private void Update()
    {
        if (!_calibrated) return;
        var p = ReadPacket();
        if (p.HasValue)
        {
            _targetAx = Mathf.Clamp((p.Value.ax - _axN) / (float)rangeAx, -1f, 1f);
            _targetAy = Mathf.Clamp((p.Value.ay - _ayN) / (float)rangeAy, -1f, 1f);

            Raw0 = p.Value.v0; Raw1 = p.Value.v1; Raw2 = p.Value.v2; Raw3 = p.Value.v3; Raw4 = p.Value.v4;

            if (_fingersCalibrated)
            {
                _targetF0 = p.Value.v0 > 0 ? Norm(p.Value.v0, 0) : -1f;
                _targetF1 = p.Value.v1 > 0 ? Norm(p.Value.v1, 1) : -1f;
                _targetF2 = p.Value.v2 > 0 ? Norm(p.Value.v2, 2) : -1f;
                _targetF3 = p.Value.v3 > 0 ? Norm(p.Value.v3, 3) : -1f;
                _targetF4 = p.Value.v4 > 0 ? Norm(p.Value.v4, 4) : -1f;

                // Медиана рабочих пальцев для неработающих (как в ganto-client)
                float median = MedianValid(_targetF0, _targetF1, _targetF2, _targetF3, _targetF4);
                if (_targetF0 < 0) _targetF0 = median;
                if (_targetF1 < 0) _targetF1 = median;
                if (_targetF2 < 0) _targetF2 = median;
                if (_targetF3 < 0) _targetF3 = median;
                if (_targetF4 < 0) _targetF4 = median;
            }
        }
        float s = 1f - smoothFactor;
        SmoothedAx = Mathf.Lerp(SmoothedAx, _targetAx, s);
        SmoothedAy = Mathf.Lerp(SmoothedAy, _targetAy, s);
        Finger0 = Mathf.Lerp(Finger0, _targetF0, s);
        Finger1 = Mathf.Lerp(Finger1, _targetF1, s);
        Finger2 = Mathf.Lerp(Finger2, _targetF2, s);
        Finger3 = Mathf.Lerp(Finger3, _targetF3, s);
        Finger4 = Mathf.Lerp(Finger4, _targetF4, s);
        V0Pressure = Finger0;
    }

    private float Norm(float raw, int idx)
    {
        float range = _fRest[idx] - _fSqueeze[idx];
        if (range < 10f) return 0f;
        return Mathf.Clamp01((_fRest[idx] - raw) / range);
    }

    private static float MedianValid(float a, float b, float c, float d, float e)
    {
        var vals = new System.Collections.Generic.List<float>();
        if (a >= 0) vals.Add(a);
        if (b >= 0) vals.Add(b);
        if (c >= 0) vals.Add(c);
        if (d >= 0) vals.Add(d);
        if (e >= 0) vals.Add(e);
        if (vals.Count == 0) return 0f;
        vals.Sort();
        return vals[vals.Count / 2];
    }
}

