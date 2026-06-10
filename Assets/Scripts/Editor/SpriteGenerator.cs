using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Генератор спрайтов для игры.
/// Запуск: Tools → Generate Game Sprites
/// </summary>
public class SpriteGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Game Sprites")]
    public static void Generate()
    {
        string path = "Assets/Sprites/";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        CreatePlane(path);
        CreateCloud(path);
        CreateBird(path);
        CreateMissile(path);
        CreateGradientBg(path);

        AssetDatabase.Refresh();
        Debug.Log("[SpriteGen] Спрайты созданы в Assets/Sprites/");
    }

    private static void CreatePlane(string path)
    {
        int w = 64, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color transparent = Color.clear;
        Color planeColor = new Color(1f, 0.9f, 0.2f, 1f);
        Color engineColor = new Color(1f, 0.4f, 0f, 1f);

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, transparent);

        int cx = w / 2, cy = h / 2;
        for (int x = cx - 20; x <= cx + 20; x++)
        {
            for (int y = cy - 12; y <= cy + 12; y++)
            {
                float fx = x - cx;
                float fy = y - cy;
                if (IsPointInTriangle(fx, fy, 20, 0, -20, -12, -20, 12))
                    tex.SetPixel(x, y, planeColor);
            }
        }

        for (int x = 6; x < 14; x++)
            for (int y = cy - 4; y <= cy + 4; y++)
                tex.SetPixel(x, y, engineColor);

        tex.Apply();
        SavePng(tex, path + "plane.png");
    }

    private static void CreateCloud(string path)
    {
        int s = 48;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color transparent = Color.clear;
        Color white = Color.white;

        for (int x = 0; x < s; x++)
            for (int y = 0; y < s; y++)
                tex.SetPixel(x, y, transparent);

        int cx = s / 2, cy = s / 2;
        int r = s / 3;
        DrawCircle(tex, cx, cy, r, white);
        DrawCircle(tex, cx - r, cy + r / 2, r, white);
        DrawCircle(tex, cx + r, cy + r / 2, r, white);
        DrawCircle(tex, cx - r / 2, cy - r / 2, r, white);
        DrawCircle(tex, cx + r / 2, cy - r / 2, r, white);

        tex.Apply();
        SavePng(tex, path + "cloud.png");
    }

    private static void CreateBird(string path)
    {
        int s = 48;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color transparent = Color.clear;
        Color bodyColor = new Color(0.3f, 0.1f, 0.1f, 1f);
        Color wingColor = new Color(0.8f, 0.2f, 0.2f, 1f);

        for (int x = 0; x < s; x++)
            for (int y = 0; y < s; y++)
                tex.SetPixel(x, y, transparent);

        int cx = s / 2, cy = s / 2;
        for (int x = cx - 6; x <= cx + 6; x++)
            for (int y = cy - 4; y <= cy + 4; y++)
                if ((x - cx) * (x - cx) * 4 + (y - cy) * (y - cy) * 9 <= 144)
                    tex.SetPixel(x, y, bodyColor);

        FillTriangle(tex, cx - 2, cy, cx - 16, cy - 10, cx - 16, cy + 10, wingColor);
        FillTriangle(tex, cx + 2, cy, cx + 16, cy - 10, cx + 16, cy + 10, wingColor);

        tex.Apply();
        SavePng(tex, path + "bird.png");
    }

    private static void CreateMissile(string path)
    {
        int w = 16, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color transparent = Color.clear;
        Color missileColor = new Color(1f, 0.4f, 0f, 1f);
        Color flameColor = new Color(1f, 1f, 0f, 1f);

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, transparent);

        for (int x = 3; x < 10; x++)
            for (int y = 8; y < h - 4; y++)
                tex.SetPixel(x, y, missileColor);

        FillTriangle(tex, 3, 8, 6, 3, 10, 8, missileColor);

        for (int x = 10; x < 14; x++)
            for (int y = 16; y < h - 12; y++)
                tex.SetPixel(x, y, flameColor);

        tex.Apply();
        SavePng(tex, path + "missile.png");
    }

    private static void CreateGradientBg(string path)
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color topColor = new Color(0.25f, 0.5f, 1f);
        Color bottomColor = new Color(0.7f, 0.85f, 1f);

        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            Color c = Color.Lerp(topColor, bottomColor, t);
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, c);
        }

        tex.Apply();
        SavePng(tex, path + "sky.png");
    }

    // ── Helpers ──

    private static void SavePng(Texture2D tex, string filepath)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(filepath, bytes);
        Debug.Log($"  Created: {filepath}");
    }

    private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color color)
    {
        int w = tex.width, h = tex.height;
        for (int x = cx - r; x <= cx + r; x++)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (x < 0 || x >= w || y < 0 || y >= h) continue;
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    tex.SetPixel(x, y, color);
            }
        }
    }

    private static void FillTriangle(Texture2D tex, int x1, int y1, int x2, int y2, int x3, int y3, Color color)
    {
        int minX = Mathf.Max(0, Mathf.Min(x1, x2, x3));
        int maxX = Mathf.Min(tex.width - 1, Mathf.Max(x1, x2, x3));
        int minY = Mathf.Max(0, Mathf.Min(y1, y2, y3));
        int maxY = Mathf.Min(tex.height - 1, Mathf.Max(y1, y2, y3));

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (IsPointInTriangle(x, y, x1, y1, x2, y2, x3, y3))
                    tex.SetPixel(x, y, color);
            }
        }
    }

    private static bool IsPointInTriangle(float px, float py,
        float x1, float y1, float x2, float y2, float x3, float y3)
    {
        float d1 = Sign(px, py, x1, y1, x2, y2);
        float d2 = Sign(px, py, x2, y2, x3, y3);
        float d3 = Sign(px, py, x3, y3, x1, y1);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Sign(float px, float py, float x1, float y1, float x2, float y2)
    {
        return (px - x2) * (y1 - y2) - (x1 - x2) * (py - y2);
    }
}
