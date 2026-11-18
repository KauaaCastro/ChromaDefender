using UnityEngine;

/// <summary>Gera por código os sprites pixel art do HUD (chama, cristal de gelo, moldura).</summary>
public static class HudSprites
{
    private const int Size = 32;
    private static Sprite flame, ice, disc, ring;

    public static Sprite Flame => flame != null ? flame : (flame = BuildFlame());
    public static Sprite Ice => ice != null ? ice : (ice = BuildIce());
    public static Sprite Disc => disc != null ? disc : (disc = BuildCircle(64, 0f));
    public static Sprite Ring => ring != null ? ring : (ring = BuildCircle(64, 0.82f));

    private static Sprite Make(Color[] px, int size, FilterMode mode)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = mode, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Color[] Clear(int size)
    {
        var px = new Color[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        return px;
    }

    // Aplica contorno escuro em volta de pixels opacos.
    private static void Outline(Color[] px, int size, Color outline)
    {
        var copy = (Color[])px.Clone();
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (copy[y * size + x].a > 0f) continue;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= size || ny >= size) continue;
                        if (copy[ny * size + nx].a > 0f) { px[y * size + x] = outline; dx = dy = 2; }
                    }
            }
    }

    private static Sprite BuildFlame()
    {
        var px = Clear(Size);
        Color red = new Color(0.85f, 0.12f, 0.05f), orange = new Color(1f, 0.5f, 0.05f),
              yellow = new Color(1f, 0.85f, 0.2f), pale = new Color(1f, 0.97f, 0.75f);
        for (int y = 2; y < Size - 2; y++)
        {
            float v = (y - 2) / (float)(Size - 5);                 // 0 base, 1 ponta
            float center = 0.5f + 0.12f * Mathf.Sin(v * 6f) * v;   // ponta ondulada
            float hw = 0.42f * Mathf.Sin(Mathf.PI * Mathf.Pow(v, 0.6f)) * (1f - 0.25f * v);
            for (int x = 0; x < Size; x++)
            {
                float u = x / (float)(Size - 1);
                float d = Mathf.Abs(u - center);
                if (hw <= 0.01f || d > hw) continue;
                float r = d / hw;
                Color c = red;
                if (r < 0.8f) c = orange;
                if (r < 0.55f && v < 0.8f) c = yellow;
                if (r < 0.3f && v < 0.5f) c = pale;
                px[y * Size + x] = c;
            }
        }
        // Chama lateral pequena
        for (int y = 6; y < 16; y++)
            for (int x = 22; x < 27; x++)
            {
                float t = (y - 6) / 9f;
                if (Mathf.Abs(x - 24f) < 2.4f * (1f - t) + 0.4f) px[y * Size + x] = (x == 24 && y < 11) ? yellow : orange;
            }
        Outline(px, Size, new Color(0.35f, 0.04f, 0.02f));
        return Make(px, Size, FilterMode.Point);
    }

    private static Sprite BuildIce()
    {
        var px = Clear(Size);
        Color deep = new Color(0.12f, 0.45f, 0.95f), mid = new Color(0.35f, 0.82f, 1f),
              light = new Color(0.72f, 0.95f, 1f), white = new Color(0.95f, 1f, 1f);
        DrawShard(px, 0.5f, 0.5f, 0.17f, 0.46f, deep, mid, light, white);   // cristal central
        DrawShard(px, 0.20f, 0.36f, 0.09f, 0.22f, deep, mid, light, white); // lateral esquerdo
        DrawShard(px, 0.80f, 0.36f, 0.09f, 0.22f, deep, mid, light, white); // lateral direito
        Outline(px, Size, new Color(0.04f, 0.18f, 0.5f));
        return Make(px, Size, FilterMode.Point);
    }

    private static void DrawShard(Color[] px, float cx, float cy, float halfW, float halfH, Color deep, Color mid, Color light, Color white)
    {
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (x / (float)(Size - 1) - cx) / halfW;
                float v = (y / (float)(Size - 1) - cy) / halfH;
                if (Mathf.Abs(u) + Mathf.Abs(v) > 1f) continue;
                Color c = u < 0f ? mid : light;                    // facetas
                if (Mathf.Abs(u) < 0.12f) c = white;                // aresta central brilhante
                if (Mathf.Abs(u) + Mathf.Abs(v) > 0.82f) c = deep;  // borda interna
                px[y * Size + x] = c;
            }
    }

    private static Sprite BuildCircle(int size, float inner)
    {
        var px = Clear(size);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                if (d > 1f || d < inner) continue;
                float edge = Mathf.Clamp01((1f - d) * r);           // borda suave
                px[y * size + x] = new Color(1f, 1f, 1f, edge);
            }
        return Make(px, size, FilterMode.Bilinear);
    }
}
