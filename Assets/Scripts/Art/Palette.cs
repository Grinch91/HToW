using UnityEngine;

/// <summary>
/// The game's colour language.
///
/// Every value is a pigment that was actually available to a scribe or carver working in
/// Ireland between roughly the seventh and twelfth centuries. Constraining the palette to
/// real materials keeps the game out of generic fantasy and gives every screen a reason
/// to look the way it does.
///
/// The discipline matters more than the hues: <see cref="Madder"/> is only ever loss —
/// morale, damage, death — and <see cref="Orpiment"/> is only ever resource. A player who
/// plays for an hour absorbs that without being told.
/// </summary>
public static class Palette
{
    /// <summary>Calfskin. Card faces and light ground.</summary>
    public static readonly Color Vellum = Hex(0xE9DDC2);

    /// <summary>Slightly deeper vellum, for insets and windows.</summary>
    public static readonly Color VellumDeep = Hex(0xDFD0AF);

    /// <summary>Bog oak. Dark ground and the board surround.</summary>
    public static readonly Color BogOak = Hex(0x16110C);

    /// <summary>Arsenic-yellow, the manuscript stand-in for gold. Resource, and only resource.</summary>
    public static readonly Color Orpiment = Hex(0xB8862A);

    /// <summary>Root-dye red. Loss, and only loss.</summary>
    public static readonly Color Madder = Hex(0x93362B);

    /// <summary>Copper green. The Gaelic faction.</summary>
    public static readonly Color Verdigris = Hex(0x4C7A57);

    /// <summary>Woad blue. The Norse faction.</summary>
    public static readonly Color Woad = Hex(0x33608F);

    /// <summary>Bare iron. Mercenaries, who belong to neither tradition.</summary>
    public static readonly Color Iron = Hex(0x6B6357);

    /// <summary>Iron-gall ink. Text on vellum.</summary>
    public static readonly Color Ink = Hex(0x221A12);

    /// <summary>Faded ink, for secondary text.</summary>
    public static readonly Color InkSoft = Hex(0x5E4E39);

    /// <summary>The colour that marks a unit as still having an action available.</summary>
    public static readonly Color Ready = Hex(0xD8A63E);

    /// <summary>The frame colour for a faction.</summary>
    public static Color For(Faction faction)
    {
        switch (faction)
        {
            case Faction.Celtic: return Verdigris;
            case Faction.Norse: return Woad;
            default: return Iron;
        }
    }

    private static Color Hex(int rgb)
    {
        return new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f,
            1f);
    }
}
