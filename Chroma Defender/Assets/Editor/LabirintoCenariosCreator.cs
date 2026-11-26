#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// LABIRINTO DO CASTELO — cenários temáticos de cada sala (só visual, sem colisão).
///
/// Cada sala ganha um prefab "Cenario_*" em Assets/Prefabs/LabirintoCastelo/Cenarios com:
///   - luz ambiente própria (véu colorido sobre a parede do fundo),
///   - decoração de parede (estandartes, janelas, brasões, retratos, espelhos, caveiras),
///   - decoração de chão (estátuas, lápides, armas, caixotes, trono...).
/// A origem do prefab é o CANTO INFERIOR ESQUERDO do lado de dentro da sala (onde o chão encosta na parede),
/// então basta soltar o prefab nesse ponto. O LabirintoCasteloBuilder já faz isso sozinho.
///
/// Regra do labirinto: NENHUM cenário usa tochas perto das portas — tocha acesa é a pista da porta certa.
/// </summary>
public static class LabirintoCenariosCreator
{
    public const string Dir = LabirintoCasteloPresets.Dir + "/Cenarios";
    private const string WhitePath = Dir + "/Branco.png";
    private const string CainosDir = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - ";
    private const string SkullDir = "Assets/Screw And Tape/2D Pixel Art Skull Icons  Emblems/";
    private const float RoomHeight = 10f;

    // Camadas de desenho (fundo de tiles = -20, portas = 4/5, personagens >= 0)
    private const int OrderAmbient = -19;
    private const int OrderWall = -12;
    private const int OrderWallFront = -11;
    private const int OrderFloor = -6;
    private const int OrderRug = -5;

    private enum Src { Cainos, Medieval, Skull, Rect }
    private enum Anchor { Bottom, Center }

    private class Item
    {
        public Src src; public string id; public float x, y, h; public Anchor anchor;
        public int order; public Color tint = Color.white; public bool flip; public float w; public float rot;
    }

    private class Theme
    {
        public string key, title; public float width = 26f; public Color ambient;
        public readonly List<Item> items = new List<Item>();

        // Atalhos para montar os cenários de forma legível
        public Theme C(string prefab, float x, float h, float y = 0f, Color? tint = null, bool flip = false, int order = OrderFloor) =>
            Add(new Item { src = Src.Cainos, id = prefab, x = x, y = y, h = h, anchor = Anchor.Bottom, order = order, tint = tint ?? Color.white, flip = flip });
        public Theme WallC(string prefab, float x, float y, float h, Color? tint = null) =>
            Add(new Item { src = Src.Cainos, id = prefab, x = x, y = y, h = h, anchor = Anchor.Center, order = OrderWall, tint = tint ?? Color.white });
        public Theme M(int index, float x, float y, float h, Anchor a = Anchor.Center, Color? tint = null, int order = OrderWall) =>
            Add(new Item { src = Src.Medieval, id = index.ToString(), x = x, y = y, h = h, anchor = a, order = order, tint = tint ?? Color.white });
        public Theme Skull(string file, float x, float y, float h, Color? tint = null) =>
            Add(new Item { src = Src.Skull, id = file, x = x, y = y, h = h, anchor = Anchor.Center, order = OrderWallFront, tint = tint ?? Color.white });
        public Theme Rect(string name, float x, float y, float w, float h, Color c, int order, float rot = 0f) =>
            Add(new Item { src = Src.Rect, id = name, x = x, y = y, w = w, h = h, anchor = Anchor.Center, order = order, tint = c, rot = rot });

        private Theme Add(Item i) { items.Add(i); return this; }
    }

    // =====================================================================
    // TEMAS (posições em unidades a partir do canto inferior esquerdo interno da sala)
    // As portas do LabirintoCasteloBuilder ficam livres: nada de chão em cima delas.
    // =====================================================================

    private static readonly Color Gold = new Color(0.85f, 0.68f, 0.3f);
    private static readonly Color Silver = new Color(0.72f, 0.78f, 0.86f);
    private static readonly Color Rug = new Color(0.45f, 0.08f, 0.1f);

    private static IEnumerable<Theme> Themes()
    {
        // 0 — Primeira Sala: salão de entrada, acolhedor (portas em 13 e 22; placa de dica em 7.5)
        yield return new Theme { key = "0_PrimeiraSala", title = "Salão de Entrada", ambient = new Color(1f, 0.8f, 0.5f, 0.07f) }
            .WallC("Banner", 10f, 6.3f, 3f)
            .WallC("Banner", 17.5f, 6.3f, 3f)
            .M(0, 4.5f, 6.4f, 1.9f).M(0, 24.5f, 6.4f, 1.9f)
            .M(20, 13f, 7.4f, 1.1f).M(20, 22f, 7.4f, 1.1f)
            .C("Statue", 17.5f, 2.6f)
            .C("Pot 01", 10.2f, 0.7f).C("Pot 02", 24.6f, 0.8f);

        // 1 — Corredor das Armaduras: fileira de estátuas de cavaleiros (portas em 2.5, 13 e 22)
        yield return new Theme { key = "1_Corredor", title = "Corredor das Armaduras", ambient = new Color(0.65f, 0.78f, 1f, 0.07f) }
            .C("Statue", 8.6f, 2.6f).C("Statue", 16.3f, 2.6f).C("Statue", 18.8f, 2.6f, flip: true)
            .M(20, 8.6f, 6.6f, 1.1f).M(20, 16.3f, 6.6f, 1.1f).M(20, 18.8f, 6.6f, 1.1f)
            .WallC("Banner", 10.6f, 6.4f, 2.8f).WallC("Banner", 24.4f, 6.4f, 2.8f)
            .WallC("Sword", 5.5f, 5.6f, 1.6f).WallC("Spear", 6.4f, 5.9f, 2.2f);

        // 2 — Cripta Esquecida: lápides, caveiras, ar esverdeado (portas em 2.5, 13 e 22; baú de prata em 24.6)
        yield return new Theme { key = "2_Cripta", title = "Cripta Esquecida", ambient = new Color(0.35f, 1f, 0.45f, 0.11f) }
            .C("Gravestone 01", 8.2f, 1.2f).C("Gravestone 02", 9.8f, 1.0f)
            .C("Gravestone 03", 15.6f, 1.3f).C("Gravestone 04", 19.6f, 1.1f)
            .C("Pot 03", 16.8f, 0.6f, tint: new Color(0.75f, 0.9f, 0.75f))
            .M(18, 11.0f, 0f, 2.4f, Anchor.Bottom, new Color(0.7f, 0.85f, 0.7f), OrderFloor)
            .M(19, 14.9f, 0f, 2.0f, Anchor.Bottom, new Color(0.7f, 0.85f, 0.7f), OrderFloor)
            .Skull("skull_brown_0-export.png", 7.0f, 6.4f, 0.8f, new Color(0.8f, 1f, 0.8f))
            .Skull("skull_brown_2-export.png", 9.4f, 7.2f, 0.7f, new Color(0.8f, 1f, 0.8f))
            .Skull("skull_brown_3-export.png", 16.4f, 6.6f, 0.8f, new Color(0.8f, 1f, 0.8f))
            .Skull("skull_brown_4-export.png", 19.8f, 7.1f, 0.7f, new Color(0.8f, 1f, 0.8f))
            .Skull("skull_brown_5-export.png", 24.6f, 6.4f, 0.8f, new Color(0.8f, 1f, 0.8f));

        // 3 — Galeria dos Retratos: quadros na parede, vasos em pedestais (degrau em 7..10; portas em 2.5, 14 e 22)
        var galeria = new Theme { key = "3_Galeria", title = "Galeria dos Retratos", ambient = new Color(1f, 0.7f, 0.5f, 0.07f) };
        Portrait(galeria, 5.5f, 6.2f, new Color(0.35f, 0.12f, 0.1f), "skull_brown_6-export.png");
        Portrait(galeria, 9.2f, 6.8f, new Color(0.12f, 0.16f, 0.32f), "skull_Bblue_0-export.png");
        Portrait(galeria, 18.2f, 6.2f, new Color(0.18f, 0.25f, 0.14f), "skull_brown_7-export.png");
        Portrait(galeria, 24.4f, 6.8f, new Color(0.3f, 0.1f, 0.25f), "skull_Bblue_2-export.png");
        galeria.WallC("Banner", 11.8f, 6.6f, 2.8f).WallC("Banner", 16.2f, 6.6f, 2.8f)
            .M(13, 17.4f, 0f, 1.3f, Anchor.Bottom, order: OrderFloor).M(8, 17.4f, 1.3f, 0.6f, Anchor.Bottom, order: OrderFloor)
            .M(13, 19.6f, 0f, 1.3f, Anchor.Bottom, order: OrderFloor).M(8, 19.6f, 1.3f, 0.6f, Anchor.Bottom, order: OrderFloor);
        yield return galeria;

        // 4 — Arsenal Esquecido: armas, caixotes, barris (baú em 10, alavanca em 16; portas em 2.5 e 22.5)
        yield return new Theme { key = "4_Arsenal", title = "Arsenal Esquecido", ambient = new Color(1f, 0.65f, 0.35f, 0.08f) }
            .C("Weapon Rack", 7.6f, 1.8f)
            .C("Crate Large", 12.6f, 1.1f).C("Crate Small", 13.8f, 0.7f)
            .C("Barrel", 18.6f, 1.1f).C("Heavy Sword", 19.9f, 1.5f)
            .WallC("Spear", 12.2f, 5.8f, 2.4f).WallC("Sword", 13.2f, 5.6f, 1.7f).WallC("Spear", 14.2f, 5.8f, 2.4f)
            .M(20, 7.6f, 6.3f, 1.1f).M(20, 19.2f, 6.3f, 1.1f)
            .C("Archery Target 01", 5.3f, 1.5f, order: OrderFloor - 1);

        // 5 — Antessala do Trono: estátuas guardando a porta do trono (porta em 15 com tochas; checkpoint em 7)
        yield return new Theme { key = "5_Antessala", title = "Antessala do Trono", ambient = new Color(0.75f, 0.55f, 1f, 0.08f) }
            .Rect("Tapete", 15f, 0.09f, 11f, 0.18f, Rug, OrderRug)
            .C("Statue", 11.6f, 2.6f).C("Statue", 18.4f, 2.6f, flip: true)
            .WallC("Banner", 11.6f, 6.4f, 2.8f, new Color(0.85f, 0.7f, 1f))
            .WallC("Banner", 18.4f, 6.4f, 2.8f, new Color(0.85f, 0.7f, 1f))
            .M(20, 15f, 6.9f, 1.5f)
            .M(0, 9.2f, 6.4f, 1.9f).M(0, 24.4f, 6.4f, 1.9f);

        // 6 — Salão dos Espelhos: espelhos de prata com brilho (chave em 7.5; portas em 2.5, 12, 17.5 e 23)
        var espelhos = new Theme { key = "6_Espelhos", title = "Salão dos Espelhos", ambient = new Color(0.5f, 0.85f, 1f, 0.12f) };
        foreach (float x in new[] { 7.5f, 9.8f, 14.75f, 20.25f, 25.0f }) Mirror(espelhos, x, 6.1f);
        espelhos.M(0, 12f, 8.3f, 1.3f, tint: new Color(0.7f, 0.95f, 1f)).M(0, 17.5f, 8.3f, 1.3f, tint: new Color(0.7f, 0.95f, 1f));
        yield return espelhos;

        // 7 — Sala do Trono (34 de largura): tapete, trono dourado, estátuas, braseiros (portas em 2.5 e 6.5)
        yield return new Theme { key = "7_Trono", title = "Sala do Trono", width = 34f, ambient = new Color(1f, 0.4f, 0.3f, 0.09f) }
            .Rect("Tapete", 20.5f, 0.09f, 25f, 0.18f, Rug, OrderRug)
            .Rect("Degrau_Trono", 31.2f, 0.25f, 3.4f, 0.5f, new Color(0.22f, 0.2f, 0.28f), OrderFloor - 1)
            .Rect("Degrau_Trono_Borda", 31.2f, 0.52f, 3.4f, 0.06f, Gold, OrderFloor)
            .C("Chair", 31.2f, 2.4f, y: 0.5f, tint: new Color(1f, 0.82f, 0.4f), flip: true)
            .C("Statue", 28.6f, 2.6f).C("Statue", 33.4f, 2.6f, flip: true)
            .C("Fire Bowl", 11f, 1.3f).C("Fire Bowl", 22f, 1.3f)
            .WallC("Banner", 13f, 6.4f, 3f, new Color(1f, 0.6f, 0.6f)).WallC("Banner", 19f, 6.4f, 3f, new Color(1f, 0.6f, 0.6f))
            .WallC("Banner", 25f, 6.4f, 3f, new Color(1f, 0.6f, 0.6f)).WallC("Banner", 31.2f, 7f, 3.4f, new Color(1f, 0.75f, 0.5f))
            .M(0, 16f, 6.6f, 1.9f).M(0, 22f, 6.6f, 1.9f)
            .M(20, 31.2f, 4.4f, 1.2f, tint: Gold, order: OrderWallFront);
    }

    /// <summary>Retrato: moldura dourada + tela colorida + figura (caveira) — "retratos amaldiçoados".</summary>
    private static void Portrait(Theme t, float x, float y, Color canvas, string skull)
    {
        t.Rect("Retrato_Moldura", x, y, 1.7f, 2.1f, Gold, OrderWall)
         .Rect("Retrato_Tela", x, y, 1.4f, 1.8f, canvas, OrderWall + 1)
         .Skull(skull, x, y - 0.05f, 1.1f, new Color(1f, 1f, 1f, 0.85f));
    }

    /// <summary>Espelho: moldura de prata, vidro azulado e um reflexo diagonal.</summary>
    private static void Mirror(Theme t, float x, float y)
    {
        t.Rect("Espelho_Moldura", x, y, 1.5f, 2.6f, Silver, OrderWall)
         .Rect("Espelho_Vidro", x, y, 1.25f, 2.35f, new Color(0.55f, 0.85f, 1f, 0.75f), OrderWall + 1)
         .Rect("Espelho_Reflexo", x - 0.15f, y + 0.2f, 0.18f, 1.7f, new Color(1f, 1f, 1f, 0.45f), OrderWallFront, -25f);
    }

    // =====================================================================
    // MENU / API
    // =====================================================================

    [MenuItem("Chroma Defender/Labirinto do Castelo/4. Criar ou Atualizar Cenários das Salas", priority = 4)]
    public static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder(LabirintoCasteloPresets.Dir)) { Directory.CreateDirectory(LabirintoCasteloPresets.Dir); AssetDatabase.Refresh(); }
        if (!AssetDatabase.IsValidFolder(Dir)) { Directory.CreateDirectory(Dir); AssetDatabase.Refresh(); }
        var white = EnsureWhiteSprite();
        int n = 0;
        foreach (var theme in Themes())
        {
            var go = Build(theme, white);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath(theme.key));
            Object.DestroyImmediate(go);
            n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"<color=lime>[Labirinto do Castelo]</color> {n} cenários criados em {Dir}.");
    }

    /// <summary>Prefab do cenário de uma sala (cria todos se ainda não existirem). Null se a sala não tem tema.</summary>
    public static GameObject Load(string roomKey)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(roomKey));
        if (p == null && Themes().Any(t => t.key == roomKey))
        {
            CreateAll();
            p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(roomKey));
        }
        return p;
    }

    private static string PrefabPath(string key) => $"{Dir}/Cenario_{key}.prefab";

    // =====================================================================
    // CONSTRUÇÃO
    // =====================================================================

    private static GameObject Build(Theme theme, Sprite white)
    {
        var root = new GameObject("Cenario_" + theme.key);

        // Luz ambiente da sala
        if (theme.ambient.a > 0f && white != null)
            MakeRect(root.transform, "LuzAmbiente", white, theme.width * 0.5f, RoomHeight * 0.5f, theme.width, RoomHeight, theme.ambient, OrderAmbient, 0f);

        var props = LabirintoCasteloPresets.Props();
        var wall = new GameObject("Parede").transform; wall.SetParent(root.transform, false);
        var floor = new GameObject("Chao").transform; floor.SetParent(root.transform, false);

        foreach (var it in theme.items)
        {
            Transform parent = (it.anchor == Anchor.Bottom || (it.src == Src.Rect && it.order >= OrderFloor - 1)) ? floor : wall;
            GameObject go = null;
            switch (it.src)
            {
                case Src.Rect:
                    if (white != null) MakeRect(parent, it.id, white, it.x, it.y, it.w, it.h, it.tint, it.order, it.rot);
                    continue;
                case Src.Medieval:
                    go = SpriteObject(parent, "Medieval_" + it.id, LabirintoCasteloPresets.P(props, int.Parse(it.id)));
                    break;
                case Src.Skull:
                    go = SpriteObject(parent, "Caveira", AssetDatabase.LoadAssetAtPath<Sprite>(SkullDir + it.id));
                    break;
                case Src.Cainos:
                    go = CainosObject(parent, it.id);
                    break;
            }
            if (go == null) { Debug.LogWarning($"[Labirinto do Castelo] Cenário {theme.key}: arte não encontrada ({it.src} {it.id})."); continue; }

            Finish(go, it);
        }
        return root;
    }

    private static GameObject SpriteObject(Transform parent, string name, Sprite sprite)
    {
        if (sprite == null) return null;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<SpriteRenderer>().sprite = sprite;
        return go;
    }

    /// <summary>Prop do Cainos como prefab aninhado, só decorativo: sem colisão e sem física.</summary>
    private static GameObject CainosObject(Transform parent, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CainosDir + name + ".prefab");
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        foreach (var c in go.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>(true)) { rb.bodyType = RigidbodyType2D.Static; rb.simulated = false; }
        return go;
    }

    /// <summary>Ajusta tamanho, cor, espelhamento, ordem e posição (base no chão ou centro na parede).</summary>
    private static void Finish(GameObject go, Item it)
    {
        var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) return;

        go.transform.localPosition = Vector3.zero;
        Bounds b = Combined(renderers);
        if (b.size.y > 0.0001f)
        {
            float k = it.h / b.size.y;
            go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(it.flip ? -k : k, k, 1f));
        }

        int baseOrder = renderers.Min(r => r.sortingOrder);
        foreach (var r in renderers)
        {
            r.sortingOrder = it.order + (r.sortingOrder - baseOrder);
            if (it.tint != Color.white) r.color = r.color * it.tint;
        }
        foreach (var ps in go.GetComponentsInChildren<ParticleSystemRenderer>(true)) ps.sortingOrder = it.order + 2;

        // Posiciona pelo retângulo final do desenho
        b = Combined(renderers);
        Vector3 parentPos = go.transform.parent.position;
        float targetX = parentPos.x + it.x;
        float targetY = parentPos.y + it.y;
        Vector3 delta = new Vector3(targetX - b.center.x, it.anchor == Anchor.Bottom ? targetY - b.min.y : targetY - b.center.y, 0f);
        go.transform.position += delta;
    }

    private static Bounds Combined(SpriteRenderer[] renderers)
    {
        Bounds b = default; bool has = false;
        foreach (var r in renderers)
        {
            if (r.sprite == null) continue;
            if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    private static void MakeRect(Transform parent, string name, Sprite white, float x, float y, float w, float h, Color c, int order, float rot)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        go.transform.localScale = new Vector3(w, h, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = white;
        sr.color = c;
        sr.sortingOrder = order;
    }

    /// <summary>Sprite branco de 1x1 unidade (base do véu de luz, tapetes, molduras e espelhos).</summary>
    private static Sprite EnsureWhiteSprite()
    {
        if (!File.Exists(WhitePath))
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            File.WriteAllBytes(WhitePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(WhitePath);
        }
        var imp = AssetImporter.GetAtPath(WhitePath) as TextureImporter;
        if (imp != null && (imp.textureType != TextureImporterType.Sprite || !Mathf.Approximately(imp.spritePixelsPerUnit, 4f)))
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 4f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
    }
}
#endif
