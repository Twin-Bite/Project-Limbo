using System.Collections.Generic;
using UnityEngine;

public enum OrganizeCategory
{
    Player,
    Building,
    Decoration,
    Misc,
    HorrorEvent
}

[System.Serializable]
public class OrganizeEntry
{
    [Tooltip("Game object yang ada di hierarchy")]
    public GameObject target;

    [Tooltip("Kategori objek")]
    public OrganizeCategory category = OrganizeCategory.Misc;

    [Tooltip("Warna yang mau lu tampilin di hierarchy")]
    public Color color = Color.white;
}

[CreateAssetMenu(fileName = "Organize", menuName = "Organize")]
public class Organize : ScriptableObject
{
    [Tooltip("Daftar GameObject yang ingin diwarnai di Hierarchy")]
    public List<OrganizeEntry> entries = new List<OrganizeEntry>();

    public static Color GetCategoryColor(OrganizeCategory category)
    {
        switch (category)
        {
            case OrganizeCategory.Player:      return new Color(0.30f, 0.65f, 1.00f); // Biru
            case OrganizeCategory.Building:    return new Color(1.00f, 0.60f, 0.20f); // Oranye
            case OrganizeCategory.Decoration:  return new Color(0.30f, 0.90f, 0.50f); // Hijau
            case OrganizeCategory.Misc:        return new Color(0.70f, 0.70f, 0.70f); // Abu-abu
            case OrganizeCategory.HorrorEvent: return new Color(1.00f, 0.20f, 0.20f); // Merah
            default:                           return Color.white;
        }
    }
}
