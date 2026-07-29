using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates the game's frames, plates and badges in code rather than loading them as
/// artwork.
///
/// This is the production route chosen in the art direction: the frame system is what
/// carries the visual identity, and generating it means it costs nothing, recolours
/// instantly, and stays perfectly consistent across every card. Painted portraits can be
/// dropped in later without touching any of this.
///
/// The two factions get genuinely different geometry rather than a palette swap. Gaelic
/// borders are woven and rounded — a line that enters the frame never simply stops.
/// Norse borders are cut and squared, with a visible grain. Mercenaries get bare iron
/// with no ornament at all, which is both cheap to produce and exactly right for troops
/// who belong to neither tradition.
///
/// Everything is cached: a texture is built once per distinct request and reused.
/// </summary>
public static class ProceduralArt
{
    public const int CardWidth = 512;
    public const int CardHeight = 744;
    public const float PixelsPerUnit = 100f;

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    /// <summary>Drops every cached sprite. Used by tests.</summary>
    public static void ClearCache()
    {
        cache.Clear();
    }

    // ── Public sprite builders ──────────────────────────────────────────────────

    /// <summary>
    /// A card frame with a transparent portrait window, so the illustration shows
    /// through, plus an opaque name plate and stat footer.
    /// </summary>
    public static Sprite CardFrame(Faction faction)
    {
        string key = "frame:" + faction;
        if (cache.ContainsKey(key)) return cache[key];

        Color32[] pixels = Fill(CardWidth, CardHeight, new Color(0, 0, 0, 0));
        Color trim = Palette.For(faction);

        // Outer border band, then the ornament inside it.
        DrawBorder(pixels, CardWidth, CardHeight, 0, 30, trim);
        DrawOrnament(pixels, CardWidth, CardHeight, faction, trim);

        // Name plate and the footer the stats sit on.
        DrawRect(pixels, CardWidth, 40, 150, CardWidth - 40, 208, trim);
        DrawRect(pixels, CardWidth, 30, 30, CardWidth - 30, 148, Palette.Vellum);

        // A hairline around the portrait window keeps the illustration from bleeding
        // into the frame.
        DrawOutline(pixels, CardWidth, CardHeight, 40, 216, CardWidth - 40, CardHeight - 40, 3, trim);

        return Store(key, pixels, CardWidth, CardHeight);
    }

    /// <summary>
    /// The supply badge. Gaelic rounds it, Norse squares it — same position, different
    /// geometry, so the faction reads even from the corner of the eye.
    /// </summary>
    public static Sprite CostBadge(Faction faction)
    {
        string key = "cost:" + faction;
        if (cache.ContainsKey(key)) return cache[key];

        const int size = 108;
        Color32[] pixels = Fill(size, size, new Color(0, 0, 0, 0));

        if (faction == Faction.Celtic)
        {
            DrawDisc(pixels, size, size / 2f, size / 2f, (size / 2f) - 4f, Palette.Orpiment);
            DrawDiscOutline(pixels, size, size / 2f, size / 2f, (size / 2f) - 4f, 4f, Palette.Ink);
        }
        else
        {
            DrawRect(pixels, size, 4, 4, size - 4, size - 4, Palette.Orpiment);
            DrawOutline(pixels, size, size, 4, 4, size - 4, size - 4, 4, Palette.Ink);
        }

        return Store(key, pixels, size, size);
    }

    /// <summary>
    /// The morale banner — the only stat with a silhouette of its own, because it is the
    /// game's signature rule and was previously the least visible thing on screen.
    /// </summary>
    public static Sprite MoraleBanner()
    {
        const string key = "morale";
        if (cache.ContainsKey(key)) return cache[key];

        const int w = 96;
        const int h = 132;
        Color32[] pixels = Fill(w, h, new Color(0, 0, 0, 0));

        for (int y = 0; y < h; y++)
        {
            // The bottom third tapers to a swallow-tail notch.
            int notch = 0;
            if (y < 34)
            {
                notch = Mathf.RoundToInt(Mathf.Abs((w / 2f) - 0f) * (1f - (y / 34f)));
                notch = Mathf.Min(notch, w / 2);
            }

            for (int x = 0; x < w; x++)
            {
                bool insideNotch = y < 34 && Mathf.Abs(x - (w / 2f)) < (w / 2f) - notch;
                if (insideNotch) continue;

                bool edge = x < 5 || x > w - 6 || y > h - 6;
                Set(pixels, w, x, y, edge ? Palette.Ink : Palette.Madder);
            }
        }

        return Store(key, pixels, w, h);
    }

    /// <summary>A plain stat plate for Strike and Stand.</summary>
    public static Sprite StatPlate(Faction faction)
    {
        string key = "stat:" + faction;
        if (cache.ContainsKey(key)) return cache[key];

        const int size = 92;
        Color32[] pixels = Fill(size, size, new Color(0, 0, 0, 0));

        if (faction == Faction.Celtic)
        {
            DrawDisc(pixels, size, size / 2f, size / 2f, (size / 2f) - 3f, Palette.Vellum);
            DrawDiscOutline(pixels, size, size / 2f, size / 2f, (size / 2f) - 3f, 3f, Palette.Ink);
        }
        else
        {
            DrawRect(pixels, size, 3, 3, size - 3, size - 3, Palette.Vellum);
            DrawOutline(pixels, size, size, 3, 3, size - 3, size - 3, 3, Palette.Ink);
        }

        return Store(key, pixels, size, size);
    }

    /// <summary>
    /// The ring that marks a unit as still having an action available.
    ///
    /// This is the single most useful thing in the whole art pass: dimming a spent card
    /// only tells you after the fact, whereas a ring tells you what you can still do.
    /// </summary>
    public static Sprite ReadyRing()
    {
        const string key = "ready";
        if (cache.ContainsKey(key)) return cache[key];

        const int w = CardWidth + 150;
        const int h = CardHeight + 150;
        Color32[] pixels = Fill(w, h, new Color(0, 0, 0, 0));

        // A solid inner band so the ring reads at a glance, then a soft falloff. The
        // first version was 28px of pure gradient and was effectively invisible once the
        // card was scaled down to board size.
        Color glow = Palette.Ready;
        for (int band = 0; band < 75; band++)
        {
            glow.a = band < 26 ? 1f : 1f - ((band - 26) / 49f);
            DrawOutline(pixels, w, h, band, band, w - band, h - band, 1, glow);
        }

        return Store(key, pixels, w, h);
    }

    /// <summary>
    /// Crops a card illustration down to just its figure.
    ///
    /// The 2014 artwork is not portrait art — each file is a complete card design with
    /// its own border, its own name banner and its own printed stats. Dropped inside the
    /// new frame it reads as a card within a card, with every label duplicated.
    ///
    /// Cutting the outer sixth away leaves the figure, which is the only part the new
    /// frame actually wants. Replacing these with real portraits later is a drop-in.
    /// </summary>
    public static Sprite Portrait(Sprite source)
    {
        if (source == null)
        {
            return null;
        }

        string key = "portrait:" + source.name;
        if (cache.ContainsKey(key)) return cache[key];

        Rect r = source.rect;

        // Asymmetric because the source cards carry a name banner at the top and a
        // deeper stat block at the foot.
        float sideCut = r.width * 0.13f;
        float topCut = r.height * 0.17f;
        float footCut = r.height * 0.24f;

        Rect crop = new Rect(
            r.x + sideCut,
            r.y + footCut,
            r.width - (sideCut * 2f),
            r.height - topCut - footCut);

        Sprite cropped = Sprite.Create(
            source.texture, crop, new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
        cropped.name = source.name + "-portrait";

        cache[key] = cropped;
        return cropped;
    }

    /// <summary>
    /// A faction card back. The 2014 build used an ornate playing-card back, which reads
    /// as a poker deck rather than anything Irish.
    /// </summary>
    public static Sprite CardBack(Faction faction)
    {
        string key = "back:" + faction;
        if (cache.ContainsKey(key)) return cache[key];

        Color trim = Palette.For(faction);
        Color32[] pixels = Fill(CardWidth, CardHeight, Palette.BogOak);

        DrawBorder(pixels, CardWidth, CardHeight, 0, 30, trim);
        DrawOrnament(pixels, CardWidth, CardHeight, faction, trim);

        // A single centred boss, so the back reads as an object rather than a pattern.
        float cx = CardWidth / 2f;
        float cy = CardHeight / 2f;
        DrawDisc(pixels, CardWidth, cx, cy, 120f, trim);
        DrawDisc(pixels, CardWidth, cx, cy, 96f, Palette.BogOak);
        DrawDisc(pixels, CardWidth, cx, cy, 54f, Palette.Orpiment);

        return Store(key, pixels, CardWidth, CardHeight);
    }

    /// <summary>A flat plate, used for HUD strips and backdrops.</summary>
    public static Sprite Plate(Color fill, Color edge, int width = 64, int height = 64)
    {
        string key = "plate:" + fill + edge + width + "x" + height;
        if (cache.ContainsKey(key)) return cache[key];

        Color32[] pixels = Fill(width, height, fill);
        DrawOutline(pixels, width, height, 0, 0, width, height, 3, edge);
        return Store(key, pixels, width, height);
    }

    /// <summary>
    /// The board surface: bog oak with a faint grain and a centre rule dividing the
    /// two lines of battle.
    /// </summary>
    public static Sprite BoardSurface()
    {
        const string key = "board";
        if (cache.ContainsKey(key)) return cache[key];

        const int w = 512;
        const int h = 288;
        Color32[] pixels = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Two offset sine bands read as grain without looking like a pattern.
                float grain = Mathf.Sin(y * 0.35f) * 0.012f + Mathf.Sin((x * 0.07f) + (y * 0.9f)) * 0.008f;
                Color c = Palette.BogOak + new Color(grain, grain * 0.8f, grain * 0.5f, 0f);

                // A warm bloom toward the middle, so the centre of play is the brightest
                // part of the table.
                float dy = Mathf.Abs(y - (h / 2f)) / (h / 2f);
                c += new Color(0.05f, 0.035f, 0.018f, 0f) * (1f - dy);

                if (Mathf.Abs(y - (h / 2f)) < 1.2f)
                {
                    c = Color.Lerp(c, Palette.Orpiment, 0.35f);
                }

                c.a = 1f;
                Set(pixels, w, x, y, c);
            }
        }

        return Store(key, pixels, w, h);
    }

    // ── Ornament ───────────────────────────────────────────────────────────────

    private static void DrawOrnament(Color32[] pixels, int w, int h, Faction faction, Color trim)
    {
        if (faction == Faction.Mercenary)
        {
            // Bare iron: rivets instead of ornament. Deliberately unadorned.
            for (int i = 0; i < 4; i++)
            {
                float fx = i < 2 ? 15f : w - 15f;
                float fy = (i % 2 == 0) ? 15f : h - 15f;
                DrawDisc(pixels, w, fx, fy, 6f, Palette.Vellum);
            }
            return;
        }

        Color ornament = Color.Lerp(trim, Palette.Vellum, 0.42f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Only inside the border band.
                bool inBand = x < 30 || x > w - 31 || y < 30 || y > h - 31;
                if (!inBand) continue;

                // Distance along the band, so the pattern runs continuously around it.
                float t = (x < 30 || x > w - 31) ? y : x;

                bool mark;
                if (faction == Faction.Celtic)
                {
                    // Woven: two out-of-phase waves crossing, so strokes pass over and
                    // under one another and the line never terminates.
                    float a = Mathf.Sin(t * 0.16f);
                    float b = Mathf.Sin((t * 0.16f) + Mathf.PI * 0.5f);
                    float band = (x < 30 || x > w - 31)
                        ? (x < 30 ? x : w - 1 - x)
                        : (y < 30 ? y : h - 1 - y);
                    float centred = (band - 15f) / 12f;
                    mark = Mathf.Abs(centred - (a * 0.55f)) < 0.30f
                           || Mathf.Abs(centred - (b * 0.55f)) < 0.30f;
                }
                else
                {
                    // Cut: hard diagonal chisel strokes with a grain line between them.
                    mark = ((x + y) % 26) < 3 || ((x - y + 2048) % 26) < 3;
                }

                if (mark)
                {
                    Set(pixels, w, x, y, ornament);
                }
            }
        }
    }

    // ── Primitives ─────────────────────────────────────────────────────────────

    private static Color32[] Fill(int w, int h, Color colour)
    {
        Color32[] pixels = new Color32[w * h];
        Color32 c = colour;
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = c;
        }
        return pixels;
    }

    private static void Set(Color32[] pixels, int w, int x, int y, Color colour)
    {
        int index = (y * w) + x;
        if (index >= 0 && index < pixels.Length)
        {
            pixels[index] = colour;
        }
    }

    private static void DrawRect(Color32[] pixels, int w, int x0, int y0, int x1, int y1, Color colour)
    {
        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                Set(pixels, w, x, y, colour);
            }
        }
    }

    private static void DrawBorder(Color32[] pixels, int w, int h, int inset, int thickness, Color colour)
    {
        DrawRect(pixels, w, inset, inset, w - inset, inset + thickness, colour);
        DrawRect(pixels, w, inset, h - inset - thickness, w - inset, h - inset, colour);
        DrawRect(pixels, w, inset, inset, inset + thickness, h - inset, colour);
        DrawRect(pixels, w, w - inset - thickness, inset, w - inset, h - inset, colour);
    }

    private static void DrawOutline(Color32[] pixels, int w, int h, int x0, int y0, int x1, int y1, int thickness, Color colour)
    {
        DrawRect(pixels, w, x0, y0, x1, y0 + thickness, colour);
        DrawRect(pixels, w, x0, y1 - thickness, x1, y1, colour);
        DrawRect(pixels, w, x0, y0, x0 + thickness, y1, colour);
        DrawRect(pixels, w, x1 - thickness, y0, x1, y1, colour);
    }

    private static void DrawDisc(Color32[] pixels, int w, float cx, float cy, float radius, Color colour)
    {
        int minX = Mathf.FloorToInt(cx - radius);
        int maxX = Mathf.CeilToInt(cx + radius);
        int minY = Mathf.FloorToInt(cy - radius);
        int maxY = Mathf.CeilToInt(cy + radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                if ((dx * dx) + (dy * dy) <= radius * radius)
                {
                    Set(pixels, w, x, y, colour);
                }
            }
        }
    }

    private static void DrawDiscOutline(Color32[] pixels, int w, float cx, float cy, float radius, float thickness, Color colour)
    {
        int minX = Mathf.FloorToInt(cx - radius);
        int maxX = Mathf.CeilToInt(cx + radius);
        int minY = Mathf.FloorToInt(cy - radius);
        int maxY = Mathf.CeilToInt(cy + radius);
        float inner = radius - thickness;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d2 = (dx * dx) + (dy * dy);
                if (d2 <= radius * radius && d2 >= inner * inner)
                {
                    Set(pixels, w, x, y, colour);
                }
            }
        }
    }

    private static Sprite Store(string key, Color32[] pixels, int w, int h)
    {
        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixels32(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;

        cache[key] = sprite;
        return sprite;
    }
}
