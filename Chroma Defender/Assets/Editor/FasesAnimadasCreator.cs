#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// FUNDOS ANIMADOS (FeonY) — duas etapas, na mesma página de menus "Chroma Defender > Fundos Animados":
///
///  1) ISOLADOS: 4 prefabs Fundo_Animado_01..04 em Assets/Prefabs/Fundos/Animados (só o fundo, com FundoAnimado).
///  2) FASES: 6 prefabs Fase_Animada_01..06 em Assets/Prefabs/Fases/Animadas, cada uma com um fundo animado,
///     tilemap de chão (grama ou pedra medieval), plataformas, buracos, muitas decorações do Cainos,
///     2 pares de portas, 1 baú e o ponto de entrada. SEM inimigos.
/// </summary>
public static class FasesAnimadasCreator
{
    public const string BgDir = "Assets/Prefabs/Fundos/Animados";
    public const string FaseDir = "Assets/Prefabs/Fases/Animadas";
    private const string AnimRoot = "Assets/FeonY/Animated Pixel-Art Backgrounds/Art/Animations/";
    private const string CainosTiles = "Assets/Cainos/Pixel Art Platformer - Village Props/Tileset Palette/TP Ground/TX Tileset Ground_";
    private const string CainosProp = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - ";
    private const string MedievalTiles = "Assets/Medieval_pixel_art_asset_FREE/Textures/Medieval_tiles_free2.png";
    private const string TileDir = FaseDir + "/Tiles";
    private const int Length = 160;

    // Pasta, arquivo do controller e primeiro frame de cada fundo animado
    private static readonly (string folder, string controller)[] BgSources =
    {
        ("Pixel-Art Background 1", "frame0000.controller"),
        ("Pixel-Art Background 2", "frame0000 (1).controller"),
        ("Pixel-Art Background 3", "frame0000 (2).controller"),
        ("Pixel-Art Background 4", "frame0000 (3).controller"),
    };

    // =====================================================================
    // MENU
    // =====================================================================

    [MenuItem("Chroma Defender/Fundos Animados/1. Criar 4 fundos animados isolados", priority = 600)]
    public static void CreateBackgrounds()
    {
        EnsureFolder("Assets/Prefabs/Fundos");
        EnsureFolder(BgDir);
        for (int i = 0; i < BgSources.Length; i++) SaveBackground(i);
        AssetDatabase.SaveAssets();
        Debug.Log($"<color=lime>[Fundos Animados]</color> 4 fundos isolados criados em {BgDir}.");
    }

    [MenuItem("Chroma Defender/Fundos Animados/2. Criar 6 fases com fundos animados", priority = 601)]
    public static void CreatePhases()
    {
        EnsureFolder("Assets/Prefabs/Fases");
        EnsureFolder(FaseDir);
        EnsureFolder(TileDir);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BgPath(0)) == null) CreateBackgrounds();

        TileBase grass = LoadTile(CainosTiles + "1.asset");
        TileBase soil = LoadTile(CainosTiles + "4.asset");
        TileBase rock = LoadTile(CainosTiles + "7.asset");
        TileBase stoneTop = MakeMedievalTile("Pedra_Topo", 1, 8);
        TileBase stoneBody = MakeMedievalTile("Pedra_Meio", 1, 9);
        if (grass == null || soil == null || rock == null || stoneTop == null || stoneBody == null)
        {
            Debug.LogError("[Fundos Animados] Tiles não encontrados. Nenhuma fase criada.");
            return;
        }

        var themes = Themes();
        for (int n = 1; n <= themes.Length; n++)
        {
            var t = themes[n - 1];
            var L = Make(n, t);
            var root = new GameObject(L.Name);
            var fundo = new GameObject("Fundo").transform;
            fundo.SetParent(root.transform, false);
            var bg = AssetDatabase.LoadAssetAtPath<GameObject>(BgPath(t.bg));
            if (bg != null) PrefabUtility.InstantiatePrefab(bg, fundo);

            bool stone = t.stone;
            BuildGround(root.transform, L, stone ? stoneTop : grass, stone ? stoneBody : soil, stone ? stoneBody : rock);
            Decorate(root.transform, L, t.props);
            BuildDoors(root.transform, L);
            BuildChest(root.transform, L);
            var spawn = new GameObject("Spawn_Entrada");
            spawn.transform.SetParent(root.transform, false);
            spawn.transform.position = new Vector3(L.spawnX, 0.1f, 0f);
            spawn.AddComponent<SpawnPoint>().Configure($"fase_anim_{n:00}_entrada", isDefault: true);

            PrefabUtility.SaveAsPrefabAsset(root, $"{FaseDir}/{L.Name}.prefab");
            Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=lime>[Fundos Animados]</color> {themes.Length} fases criadas em {FaseDir} (sem inimigos).");
    }

    // =====================================================================
    // FUNDOS ISOLADOS
    // =====================================================================

    private static string BgPath(int i) => $"{BgDir}/Fundo_Animado_{i + 1:00}.prefab";

    private static void SaveBackground(int i)
    {
        var (folder, controllerFile) = BgSources[i];
        var go = new GameObject($"Fundo_Animado_{i + 1:00}");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr(AnimRoot + folder + "/frame0000.png");
        sr.sortingOrder = -200;
        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimRoot + folder + "/" + controllerFile);
        go.AddComponent<FundoAnimado>();
        PrefabUtility.SaveAsPrefabAsset(go, BgPath(i));
        Object.DestroyImmediate(go);
    }

    // =====================================================================
    // TEMAS DAS 6 FASES
    // =====================================================================

    private class Theme
    {
        public string name; public int bg; public bool stone; public string[] props;
    }

    private static Theme[] Themes() => new[]
    {
        new Theme { name = "Campo_Animado",    bg = 0, stone = false, props = new[] { "Tree 01", "Tree 02", "Bush 01", "Bush 02", "Fence A", "Road Lamp", "Grass 01", "Flower 01", "Rock 01" } },
        new Theme { name = "Ceu_Animado",      bg = 1, stone = false, props = new[] { "Sunflower 01", "Flower 01", "Grass 01", "Bush 02", "Sign 01", "Well", "Stump", "Rock 02" } },
        new Theme { name = "Pantano_Animado",  bg = 2, stone = false, props = new[] { "Stump", "Bush 01", "Grass 01", "Gravestone 01", "Tree 02", "Rock 01", "Flower 01", "Fence A" } },
        new Theme { name = "Campo_Alto",       bg = 3, stone = false, props = new[] { "Tree 01", "Tree 02", "Bush 02", "Sunflower 01", "Well", "Sign 01", "Rock 02", "Grass 01" } },
        new Theme { name = "Ruinas_Animadas",  bg = 0, stone = true,  props = new[] { "Statue", "Weapon Rack", "Barrel", "Crate Small", "Gravestone 02", "Gravestone 01", "Rock 02", "Road Lamp" } },
        new Theme { name = "Torre_Animada",    bg = 2, stone = true,  props = new[] { "Statue", "Gravestone 02", "Weapon Rack", "Barrel", "Crate Small", "Rock 01", "Road Lamp", "Stump" } },
    };

    private class Layout
    {
        public int n; public string name; public Theme theme;
        public string Name => $"Fase_Animada_{n:00}_{theme.name}";
        public HashSet<int> pits = new HashSet<int>();
        public List<(int x, int top, int len)> plats = new List<(int, int, int)>();
        public List<float> decor = new List<float>();
        public float doorA = 3f, doorB = Length - 5f, doorC = 70f, doorD = 88f;
        public float chestX = 46f;
        public string chestPrefab;
        public float spawnX = 6f;
    }

    private static readonly (int a, int b)[] Protected =
    {
        (0, 10), (40, 54), (64, 76), (82, 94), (150, Length - 1)
    };

    private static bool IsProtected(int x) => Protected.Any(z => x >= z.a && x <= z.b);

    private static Layout Make(int n, Theme t)
    {
        var rng = new System.Random(7300 + n * 31);
        var L = new Layout { n = n, theme = t };
        L.chestPrefab = n <= 2 ? BausEscalaveisCreator.Madeira : n <= 4 ? BausEscalaveisCreator.Prata : BausEscalaveisCreator.Ouro;

        // Buracos com plataforma por cima (altura 4)
        int width = n >= 5 ? 4 : 3;
        for (int x = 22 + rng.Next(0, 4); x < Length - 14; x += 19 + rng.Next(0, 8))
        {
            bool ok = true;
            for (int c = x; c < x + width; c++) if (IsProtected(c)) ok = false;
            if (!ok) continue;
            for (int c = x; c < x + width; c++) L.pits.Add(c);
            L.plats.Add((x - 1, 4, width + 2));
        }

        // Degraus extras (5 por fase)
        int placed = 0;
        for (int tries = 0; tries < 300 && placed < 5; tries++)
        {
            int len = rng.Next(4, 7), x = rng.Next(12, Length - 14), h = rng.Next(2, 7);
            bool ok = true;
            for (int c = x - 2; c < x + len + 2; c++) if (IsProtected(c)) { ok = false; break; }
            foreach (var p in L.plats)
                if (x < p.x + p.len + 2 && p.x < x + len + 2 && Mathf.Abs(p.top - h) < 3) { ok = false; break; }
            if (!ok) continue;
            L.plats.Add((x, h, len));
            placed++;
        }

        for (int i = 0; i < 14; i++) L.decor.Add(PickGround(rng, L));
        return L;
    }

    private static float PickGround(System.Random rng, Layout L)
    {
        for (int t = 0; t < 500; t++)
        {
            int x = rng.Next(12, Length - 12);
            if (!L.pits.Contains(x) && !IsProtected(x)) return x + 0.5f;
        }
        return 12f;
    }

    // =====================================================================
    // CONSTRUÇÃO
    // =====================================================================

    private static void BuildGround(Transform root, Layout L, TileBase top, TileBase soil, TileBase body)
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
            tm.SetTile(new Vector3Int(x, -1, 0), top);
            tm.SetTile(new Vector3Int(x, -2, 0), soil);
            tm.SetTile(new Vector3Int(x, -3, 0), body);
            tm.SetTile(new Vector3Int(x, -4, 0), body);
        }
        foreach (var p in L.plats)
            for (int x = p.x; x < p.x + p.len; x++)
            {
                tm.SetTile(new Vector3Int(x, p.top - 1, 0), top);
                tm.SetTile(new Vector3Int(x, p.top - 2, 0), soil);
            }
        tm.CompressBounds();
    }

    /// <summary>Props do Cainos como decoração: sem colisão, com a base no chão.</summary>
    private static void Decorate(Transform root, Layout L, string[] props)
    {
        var holder = new GameObject("Decoracao").transform;
        holder.SetParent(root, false);
        for (int i = 0; i < L.decor.Count; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CainosProp + props[i % props.Length] + ".prefab");
            if (prefab == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
            foreach (var c in go.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
            foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>(true)) { rb.bodyType = RigidbodyType2D.Static; rb.simulated = false; }
            PlaceOnGround(go, L.decor[i]);
        }
    }

    /// <summary>Centraliza o prop em x e encosta a base dele no chão (y = 0).</summary>
    private static void PlaceOnGround(GameObject go, float x)
    {
        var rs = go.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.sprite != null).ToArray();
        go.transform.position = new Vector3(x, 0f, 0f);
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.position += new Vector3(x - b.center.x, -b.min.y, 0f);
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
        LabirintoCasteloPresets.Set(so, "doorId", $"fase_anim_{L.n:00}_{id}");
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
        var chest = inst.GetComponentInChildren<UpgradeChest>();
        if (chest != null)
        {
            var so = new SerializedObject(chest);
            LabirintoCasteloPresets.Set(so, "chestId", $"fase_anim_{L.n:00}_bau");
            so.ApplyModifiedProperties();
        }
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    /// <summary>Cria (uma vez) um Tile da folha medieval, usando o mesmo mapeamento de grade do labirinto.</summary>
    private static TileBase MakeMedievalTile(string name, int col, int row)
    {
        string path = $"{TileDir}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            Sprite sprite = null;
            foreach (var s in AssetDatabase.LoadAllAssetsAtPath(MedievalTiles).OfType<Sprite>())
            {
                int c = Mathf.RoundToInt((s.rect.x - 16f) / 48f);
                int r = Mathf.RoundToInt((760f - s.rect.y) / 48f);
                if (c == col && r == row) { sprite = s; break; }
            }
            if (sprite == null) return null;
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, path);
        }
        return tile;
    }

    private static TileBase LoadTile(string path) => AssetDatabase.LoadAssetAtPath<TileBase>(path);

    private static Sprite Spr(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
