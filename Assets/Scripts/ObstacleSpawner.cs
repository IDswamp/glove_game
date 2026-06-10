using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Запись: префаб + зона спавна по вертикали (0 = низ экрана, 1 = верх).
/// </summary>
[Serializable]
public class ObstacleEntry
{
    [Tooltip("Префаб препятствия (должен иметь компонент Obstacle и тег Obstacle)")]
    public GameObject prefab;

    [Header("Зона по Y (0 = низ, 1 = верх)")]
    [Range(0f, 1f)]
    public float minY = 0.1f;
    [Range(0f, 1f)]
    public float maxY = 0.9f;

    [Tooltip("Вес: чем больше, тем чаще спавнится относительно других")]
    [Range(0.1f, 10f)]
    public float weight = 1f;
}

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Препятствия")]
    [Tooltip("Настрой зону и вес для каждого. Дом — низ, шторм — верх, птицы — везде.")]
    public List<ObstacleEntry> obstacles;

    [Header("Спавн")]
    public float spawnInterval = 1.5f;
    [Tooltip("Первый UI-элемент (счёт или жизни). Враги будут вставляться ПЕРЕД ним, чтобы не перекрывать интерфейс.")]
    public RectTransform uiSeparator;

    private float _timer;
    private bool _spawning = true;
    private RectTransform _spawnParent;
    private float _refW;
    private float _refH;
    private float _totalWeight;
    private List<GameObject> _spawned = new List<GameObject>();

    private void Start()
    {
        // Спавним туда же, где лежит сам спавнер (PlaneGamePanel), а не в корень Canvas
        _spawnParent = (RectTransform)transform;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                _refW = scaler.referenceResolution.x;
                _refH = scaler.referenceResolution.y;
            }
            else
            {
                _refW = _spawnParent.rect.width;
                _refH = _spawnParent.rect.height;
            }
        }

        RecalcWeights();
    }

    private void OnValidate()
    {
        RecalcWeights();
    }

    private void RecalcWeights()
    {
        _totalWeight = 0f;
        if (obstacles != null)
            foreach (var o in obstacles)
                if (o.prefab != null)
                    _totalWeight += o.weight;
    }

    private float VisibleRefH()
    {
        return Screen.height * _refW / (float)Screen.width;
    }

    public void StartSpawning() => _spawning = true;
    public void StopSpawning() => _spawning = false;

    public void ClearAll()
    {
        foreach (var obj in _spawned)
        {
            if (obj != null)
                Destroy(obj);
        }
        _spawned.Clear();
    }

    private void Update()
    {
        if (!_spawning) return;
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval) { _timer = 0f; SpawnRandom(); }
    }

    private void SpawnRandom()
    {
        if (obstacles == null || obstacles.Count == 0 || _totalWeight <= 0f) return;
        if (_spawnParent == null) return;

        // Выбираем префаб с учётом веса
        ObstacleEntry entry = PickWeighted();
        if (entry == null || entry.prefab == null) return;

        float visH = VisibleRefH();
        float y = visH * Mathf.Lerp(entry.minY, entry.maxY, UnityEngine.Random.value);
        float x = _refW + 80f;

        GameObject obj = Instantiate(entry.prefab, _spawnParent);
        // Вставляем перед UI, но после фонов
        if (uiSeparator != null)
            obj.transform.SetSiblingIndex(uiSeparator.GetSiblingIndex());
        _spawned.Add(obj);
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(x, y);
        }
    }

    private ObstacleEntry PickWeighted()
    {
        float roll = UnityEngine.Random.Range(0f, _totalWeight);
        float sum = 0f;
        foreach (var entry in obstacles)
        {
            if (entry.prefab == null) continue;
            sum += entry.weight;
            if (roll <= sum) return entry;
        }
        return null;
    }
}
