#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// FASES VISUAIS — 10 áreas grandes (160 unidades) com fundo, chão em tilemap, buracos, plataformas,
/// decorações, 2 pares de portas, 1 baú e o ponto de entrada. SEM inimigos (eles entram pelo menu separado).
///
/// Prefabs em Assets/Prefabs/Fases/Visuais:  Fase_Visual_01 ... Fase_Visual_10
///   Fundo/      → camadas de parallax (ParallaxBackground)
///   Grid/Chao   → tilemap do chão e das plataformas (colisão na layer Ground). Superfície do chão = y 0.
///   Decoracao/  → árvores, lanternas, cercas, pedras
///   Portas/     → Porta_Labirinto ligadas em pares (ida e volta)
///   Baus/       → um baú escalável por fase
///   Spawn_Entrada → SpawnPoint "fase_NN_entrada" (padrão)
///
/// Layout é determinístico (semente por fase): cada buraco tem plataforma por cima para o pulo.
/// Menu "Adicionar inimigos de teste" acrescenta 2 esqueletos por fase, se você quiser.
/// </summary>
public static class FasesVisuaisCreator
{
    public const string Dir = "Assets/Prefabs/Fases/Visuais";
    private const int Length = 160;
    private const string CainosTiles = "Assets/Cainos/Pixel Art Platformer - Village Props/Tileset Palette/TP Ground/TX Tileset Ground_";
    private const string DarkBg = "Assets/BSGameStudio/Dark Forest Pixel Pack – 2D Platformer Starter Pack/Art/";
    private const string Pix = "Assets/Pixel Skies DEMO/";
    private const string HqPrefab = "Assets/Prefabs/Backgrounds/ParallaxForest.prefab";

    private enum Bg { HqFloresta, CeuLayer01, CeuLayer02, Silhueta, Ceu03, Ceu04, Ceu05, Ceu06, Ceu01Full, Ceu02Full }

    private static readonly string[] Names =
    {
        "Floresta_Escura", "Ceu_Noturno_Camadas", "Ceu_Estrelado", "Silhueta_Montanhas",
        "Ceu_Pixel_03", "Ceu_Pixel_04", "Ceu_Pixel_05", "Ceu_Pixel_06", "Ceu_Pixel_Full_01", "Ceu_Pixel_Full_02"
    };

    private class Layout
    {
        public int n;                      // 1..10
        public string Name => $"Fase_Visual_{n:00}_{Names[n - 1]}";
        public Bg bg;
        public HashSet<int> pits = new HashSet<int>();
        public List<(int x, int top, int len)> plats = new List<(int, int, int)>(); // top = superfície
        public List<float> decor = new List<float>();
        public List<float> enemies = new List<float>();
        public float doorA = 3f, doorB = Length - 5f, doorC = 70f, doorD = 88f;
        public float chestX = 46f;
        public string chestPrefab;
        public float spawnX = 6f;
    }

    // Zonas que nunca recebem buraco, plataforma ou decoração (portas, baú, spawn)
    private static readonly (int a, int b)[] Protected =
    {
        (0, 10), (40, 54), (64, 76), (82, 94), (150, Length - 1)
    };

    private static bool IsProtected(int x) => Protected.Any(z => x >= z.a && x <= z.b);

    private static Layout Make(int n)
    {
        var rng = new System.Random(4200 + n * 17);
        var L = new Layout { n = n, bg = (Bg)(n - 1) };
        L.chestPrefab = n <= 5 ? BausEscalaveisCreator.Madeira : n <= 8 ? BausEscalaveisCreator.Prata : BausEscalaveisCreator.Ouro;

        // Buracos: a cada ~20-26 unidades, com plataforma por cima (altura 4)
        int width = n >= 6 ? 4 : 3;
        for (int x = 22 + rng.Next(0, 4); x < Length - 14; x += 20 + rng.Next(0, 7))
        {
            bool ok = true;
            for (int c = x; c < x + width; c++) if (IsProtected(c)) ok = false;
            if (!ok) continue;
            for (int c = x; c < x + width; c++) L.pits.Add(c);
            L.plats.Add((x - 1, 4, width + 2));
        }

        // Plataformas de degrau (4 por fase), fora das zonas protegidas e sem sobrepor outras
        int placed = 0;
        for (int tries = 0; tries < 200 && placed < 4; tries++)
        {
            int len = rng.Next(4, 7), x = rng.Next(12, Length - 14), h = rng.Next(2, 6);
            bool ok = true;
            for (int c = x - 2; c < x + len + 2; c++) if (IsProtected(c)) { ok = false; break; }
            foreach (var p in L.plats)
                if (x < p.x + p.len + 2 && p.x < x + len + 2 && Mathf.Abs(p.top - h) < 3) { ok = false; break; }
            if (!ok) continue;
            L.plats.Add((x, h, len));
            placed++;
        }

        // Decorações e inimigos (somente sobre chão sólido, fora das zonas protegidas)
        for (int i = 0; i < 8; i++) L.decor.Add(PickGround(rng, L));
        L.enemies.Add(PickGround(rng, L, 20, 40));
        L.enemies.Add(PickGround(rng, L, 110, 138));
        return L;
    }

    private static float PickGround(System.Random rng, Layout L, int from = 12, int to = Length - 12)
    {
        for (int t = 0; t < 500; t++)
        {
            int x = rng.Next(from, to);
            if (!L.pits.Contains(x) && !IsProtected(x)) return x + 0.5f;
        }
        return from;
    }

    // =====================================================================
    // MENUS
    // =====================================================================

    [MenuItem("Chroma Defender/Fases/Criar 10 Fases Visuais", priority = 500)]
    public static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Fases")) AssetDatabase.CreateFolder("Assets/Prefabs", "Fases");
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Prefabs/Fases", "Visuais");

        var grass = Tile("1");
        var soil = Tile("4");
        var rock = Tile("7");
        if (grass == null || soil == null || rock == null)
        {
            Debug.LogError("[Fases Visuais] Tiles Cainos (TX Tileset Ground_1/4/7) não encontrados. Nada foi criado.");
            return;
        }

        var hq = AssetDatabase.LoadAssetAtPath<GameObject>(HqPrefab) ?? ParallaxForestBuilder.CreatePrefab();
        int ok = 0;
        for (int n = 1; n <= 10; n++)
        {
            var L = Make(n);
            var root = new GameObject(L.Name);
            var fundo = new GameObject("Fundo").transform;
            fundo.SetParent(root.transform, false);
            BuildBackground(fundo, L.bg, hq);
            BuildGround(root.transform, L, grass, soil, rock);
            Decorate(root.transform, L);
            BuildDoors(root.transform, L);
            BuildChest(root.transform, L);
            AddSpawn(root.transform, L);

            PrefabUtility.SaveAsPrefabAsset(root, $"{Dir}/{L.Name}.prefab");
            Object.DestroyImmediate(root);
            ok++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=lime>[Fases Visuais]</color> {ok} fases criadas em {Dir} (sem inimigos).");
    }

    [MenuItem("Chroma Defender/Fases/Adicionar inimigos de teste nas Fases Visuais", priority = 501)]
    public static void AddEnemies()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) { Debug.LogWarning("[Fases Visuais] Crie as fases antes (menu Criar 10 Fases Visuais)."); return; }
        var enemy = Fase2Fase3PrefabCreator.LoadEnemy(Fase2Fase3PrefabCreator.SkeletonSword);
        if (enemy == null) { Debug.LogWarning("[Fases Visuais] Prefab do inimigo não encontrado."); return; }

        for (int n = 1; n <= 10; n++)
        {
            var L = Make(n);
            string path = $"{Dir}/{L.Name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var old = root.transform.Find("Inimigos");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var holder = new GameObject("Inimigos").transform;
            holder.SetParent(root.transform, false);
            foreach (float x in L.enemies)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(enemy, holder);
                inst.transform.position = new Vector3(x, 0.05f, 0f);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("<color=lime>[Fases Visuais]</color> 2 inimigos de teste adicionados em cada fase.");
    }

    // =====================================================================
    // CONSTRUÇÃO
    // =====================================================================

    private static void BuildBackground(Transform parent, Bg bg, GameObject hq)
    {
        if (bg == Bg.HqFloresta)
        {
            if (hq != null) PrefabUtility.InstantiatePrefab(hq, parent);
            return;
        }
        var bgc = parent.gameObject.AddComponent<ParallaxBackground>();
        bgc.Layers.Clear();
        foreach (var l in LayersFor(bg))
            if (l.s != null) bgc.Layers.Add(new ParallaxBackground.Layer { name = l.s.name, sprite = l.s, parallax = l.p, autoScrollSpeed = l.scroll });
        bgc.Build();
    }

    private static (Sprite s, float p, float scroll)[] LayersFor(Bg bg)
    {
        string ds = DarkBg + "Backgrounds/background_";
        string l1 = Pix + "Parallax Pixel Skies 240x135px/Demo 01 Parallax Pixel Sky/demo01_PixelSky_layer0";
        string l2 = Pix + "Parallax Pixel Skies 240x135px/Demo 02 Parallax Pixel Sky/demo02_PixelSky_layer0";
        string full = Pix + "Pixel Skies 1920x1080px (Full HD)/demo0";
        switch (bg)
        {
            case Bg.CeuLayer01: return new[] { (Spr(l1 + "1.png"), 0f, 0f), (Spr(l1 + "2.png"), 0.08f, 0.15f), (Spr(l1 + "3.png"), 0.2f, 0.3f) };
            case Bg.CeuLayer02: return new[] { (Spr(l2 + "1.png"), 0f, 0f), (Spr(l2 + "2.png"), 0.08f, 0.15f), (Spr(l2 + "3.png"), 0.2f, 0.3f) };
            case Bg.Silhueta: return new[] { (Spr(ds + "1.png"), 0f, 0f), (Spr(ds + "5.png"), 0.04f, 0f), (Spr(ds + "3.png"), 0.1f, 0f), (Spr(ds + "2.png"), 0.18f, 0f), (Spr(ds + "4.png"), 0.3f, 0f) };
            case Bg.Ceu03: return new[] { (Spr(full + "03_PixelSky_1920x1080.png"), 0f, 0f) };
            case Bg.Ceu04: return new[] { (Spr(full + "04_PixelSky_1920x1080.png"), 0f, 0f) };
            case Bg.Ceu05: return new[] { (Spr(full + "05_PixelSky_1920x1080.png"), 0f, 0f) };
            case Bg.Ceu06: return new[] { (Spr(full + "06_PixelSky_1920x1080.png"), 0f, 0f) };
            case Bg.Ceu01Full: return new[] { (Spr(full + "01_PixelSky_1920x1080.png"), 0f, 0f) };
            case Bg.Ceu02Full: return new[] { (Spr(full + "02_PixelSky_1920x1080.png"), 0f, 0f) };
            default: return new (Sprite, float, float)[0];
        }
    }

    private static void BuildGround(Transform root, Layout L, TileBase grass, TileBase soil, TileBase rock)
    {
        var grid = new GameObject("Grid", typeof(Grid)).transform;
        grid.SetParent(root, false);
        var chao = new GameObject("Chao", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        chao.transform.SetParent(grid, false);
        int ground = LayerMask.NameToLayer("Ground");
        chao.layer = ground >= 0 ? ground : 6;
        chao.GetComponent<TilemapRenderer>().sortingOrder = 0;

        var tm = chao.GetComponent<Tilemap>();
        for (int x = 0; x < Length; x++)
        {
            if (L.pits.Contains(x)) continue;
            tm.SetTile(new Vector3Int(x, -1, 0), grass);
            tm.SetTile(new Vector3Int(x, -2, 0), soil);
            tm.SetTile(new Vector3Int(x, -3, 0), rock);
            tm.SetTile(new Vector3Int(x, -4, 0), rock);
        }
        foreach (var p in L.plats)
            for (int x = p.x; x < p.x + p.len; x++)
            {
                tm.SetTile(new Vector3Int(x, p.top - 1, 0), grass);
                tm.SetTile(new Vector3Int(x, p.top - 2, 0), soil);
            }
        tm.CompressBounds();
    }

    private static void Decorate(Transform root, Layout L)
    {
        var deco = new GameObject("Decoracao").transform;
        deco.SetParent(root, false);
        string nat = DarkBg + "Nature/";
        string dec = DarkBg + "Decorations/";
        var set = new[]
        {
            (nat + "tree.png", 6.5f, false), (nat + "tree_2.png", 5.2f, true), (nat + "bush.png", 1.2f, false),
            (nat + "bush_2.png", 1.1f, true), (nat + "mushroom.png", 0.5f, false), (nat + "large_stone.png", 0.9f, false),
            (dec + "post_with_lantern.png", 2.4f, false), (dec + "fence.png", 1.0f, true),
        };
        for (int i = 0; i < L.decor.Count; i++)
        {
            var d = set[i % set.Length];
            var s = Spr(d.Item1);
            if (s == null) continue;
            var go = new GameObject(s.name);
            go.transform.SetParent(deco, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sortingOrder = 3;
            float k = d.Item2 / s.bounds.size.y;
            go.transform.localScale = new Vector3(d.Item3 ? -k : k, k, 1f);
            go.transform.localPosition = new Vector3(L.decor[i] - s.bounds.center.x * k, -s.bounds.min.y * k, 0f);
        }
    }

    private static void BuildDoors(Transform root, Layout L)
    {
        var holder = new GameObject("Portas").transform;
        holder.SetParent(root, false);
        var a = Door(holder, L, L.doorA, "entrada", "Entrada");
        var b = Door(holder, L, L.doorB, "saida", "Saída");
        var c = Door(holder, L, L.doorC, "atalho_a", "Passagem");
        var d = Door(holder, L, L.doorD, "atalho_b", "Passagem");
        LabirintoCasteloPresets.Link(a, b); LabirintoCasteloPresets.Link(b, a);
        LabirintoCasteloPresets.Link(c, d); LabirintoCasteloPresets.Link(d, c);
    }

    private static CastleDoor Door(Transform parent, Layout L, float x, string id, string title)
    {
        var prefab = LabirintoCasteloPresets.Load(LabirintoCasteloPresets.PortaComum);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = new Vector3(x, 0f, 0f);
        go.name = $"Porta_{id}";
        var door = go.GetComponent<CastleDoor>();
        var so = new SerializedObject(door);
        LabirintoCasteloPresets.Set(so, "doorId", $"fase_{L.n:00}_{id}");
        LabirintoCasteloPresets.Set(so, "doorTitle", title);
        so.ApplyModifiedProperties();
        return door;
    }

    private static void BuildChest(Transform root, Layout L)
    {
        var prefab = BausEscalaveisCreator.Load(L.chestPrefab);
        if (prefab == null) return;
        var holder = new GameObject("Baus").transform;
        holder.SetParent(root, false);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
        inst.transform.position = new Vector3(L.chestX, 0f, 0f);
        inst.name = "Bau_" + L.chestPrefab;
        var chest = inst.GetComponentInChildren<UpgradeChest>();
        if (chest != null)
        {
            var so = new SerializedObject(chest);
            LabirintoCasteloPresets.Set(so, "chestId", $"fase_{L.n:00}_bau");
            so.ApplyModifiedProperties();
        }
    }

    private static void AddSpawn(Transform root, Layout L)
    {
        var go = new GameObject("Spawn_Entrada");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(L.spawnX, 0.1f, 0f);
        go.AddComponent<SpawnPoint>().Configure($"fase_{L.n:00}_entrada", isDefault: true);
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private static TileBase Tile(string index) =>
        AssetDatabase.LoadAssetAtPath<TileBase>(CainosTiles + index + ".asset");

    private static Sprite Spr(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();
}
#endif
