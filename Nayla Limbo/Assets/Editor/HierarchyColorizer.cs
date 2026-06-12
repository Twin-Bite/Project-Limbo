using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Secara otomatis mewarnai item di Hierarchy Window
/// berdasarkan data dari semua ScriptableObject Organize di project.
/// WAJIB ada di dalam folder bernama "Editor".
/// [InitializeOnLoad] membuat script ini berjalan otomatis saat Unity dibuka.
/// </summary>
[InitializeOnLoad]
public static class HierarchyColorizer
{
    private static List<Organize> _cachedAssets   = new List<Organize>();
    private static double         _lastRefreshTime = 0;
    private const  double         REFRESH_INTERVAL = 2.0; // detik

    static HierarchyColorizer()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
        EditorApplication.projectChanged           += OnProjectChanged;
    }

    // ── Dipanggil setiap kali Unity menggambar satu item hierarchy ──
    static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
    {
        if (EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            RefreshCache();

        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;

        foreach (Organize asset in _cachedAssets)
        {
            if (asset == null || asset.entries == null) continue;

            foreach (OrganizeEntry entry in asset.entries)
            {
                if (entry.target != go) continue;

                DrawColoredRow(selectionRect, go.name, entry.color);
                return; 
            }
        }
    }

    // ── Gambar background berwarna + label ulang ──────────────
    static void DrawColoredRow(Rect rowRect, string objectName, Color itemColor)
    {
        Color bgColor = itemColor;
        bgColor.a = 0.25f;
        EditorGUI.DrawRect(rowRect, bgColor);

        Rect accentRect = new Rect(rowRect.x, rowRect.y, 3f, rowRect.height);
        Color accentColor = itemColor;
        accentColor.a = 1f;
        EditorGUI.DrawRect(accentRect, accentColor);

        float brightness = itemColor.r * 0.299f + itemColor.g * 0.587f + itemColor.b * 0.114f;
        Color textColor  = brightness > 0.6f
            ? new Color(0.1f, 0.1f, 0.1f)   // Gelap untuk background terang
            : new Color(0.95f, 0.95f, 0.95f); // Terang untuk background gelap

        GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            normal    = { textColor = textColor }
        };

        Rect labelRect = new Rect(rowRect.x + 18, rowRect.y, rowRect.width - 18, rowRect.height);
        EditorGUI.LabelField(labelRect, objectName, labelStyle);
    }

    // ── Refresh daftar Organize assets ───────────────────────
    static void RefreshCache()
    {
        _cachedAssets.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Organize");
        foreach (string guid in guids)
        {
            string   path  = AssetDatabase.GUIDToAssetPath(guid);
            Organize asset = AssetDatabase.LoadAssetAtPath<Organize>(path);
            if (asset != null) _cachedAssets.Add(asset);
        }

        _lastRefreshTime = EditorApplication.timeSinceStartup;
    }

    // ── Refresh otomatis kalau ada perubahan di project ───────
    static void OnProjectChanged()
    {
        _lastRefreshTime = 0;
    }
}