#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// LABIRINTO DO CASTELO — fundos básicos das áreas (prefabs), para o labirinto levar o jogador a vários cenários.
///
/// Cada prefab fica em Assets/Prefabs/LabirintoCastelo/Fundos e tem:
///   <Nome>
///     Fundo/      → camadas com parallax e movimento (ParallaxBackground); acompanha a câmera
///     Grid/Chao/  → (só os de chão) tilemap de terra com colisão na layer Ground, linha de chão em y = 0
///     Decoracao/  → árvores, arbustos, lanternas, cercas etc., com a base em y = 0
///
/// Fundos:
///   Fundo_Floresta_Escura_HQ   → Parallax Forest HQ (10 camadas, sem chão)
///   Fundo_Ceu_Noturno_01/02    → Pixel Skies (céu noturno com nuvens, sem chão)
///   Fundo_Silhueta_Floresta    → Dark Forest (silhuetas de montanha/árvore, sem chão)
///   Fundo_Floresta_Chao        → Dark Forest + tilemap de terra + árvores e lanternas
///   Fundo_Acampamento_Chao     → Pixel Skies 02 + tilemap de terra + tenda e fogueira
/// Básico de propósito: é o ponto de partida para as áreas do labirinto.
/// </summary>
public static class LabirintoFundosCreator
{
    public const string Dir = LabirintoCasteloPresets.Dir + "/Fundos";
    private const string TileDir = Dir + "/Tiles";
    private const string DarkDir = "Assets/BSGameStudio/Dark Forest Pixel Pack – 2D Platformer Starter Pack/Art/";
    private const string SkyDir = "Assets/Pixel Skies DEMO/Parallax Pixel Skies 240x135px/";
    private const string HqForestPrefab = "Assets/Prefabs/Backgrounds/ParallaxForest.prefab";
    private const int HalfWidth = 30;   // chão de -30 a +30 unidades
    private const int GroundDepth = 3;  // 3 linhas de terra

    [MenuItem("Chroma Defender/Labirinto do Castelo/5. Criar Fundos das Áreas", priority = 5)]
    public static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder(LabirintoCasteloPresets.Dir)) AssetDatabase.CreateFolder("Assets/Prefabs", "LabirintoCastelo");
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder(LabirintoCasteloPresets.Dir, "Fundos");
        if (!AssetDatabase.IsValidFolder(TileDir)) AssetDatabase.CreateFolder(Dir, "Tiles");

        // Floresta HQ: reaproveita o prefab de fundo que já existe (cria se faltar)
        var hq = AssetDatabase.LoadAssetAtPath<GameObject>(HqForestPrefab) ?? ParallaxForestBuilder.CreatePrefab();

        // Tiles de terra (Dark Forest)
        var earth = Sprites(DarkDir + "Tiles/Earth.png");
        TileBase top = null, fill = null;
        if (earth.Length > 0)
        {
            top = MakeTile("Terra_Topo", earth[0]);
            fill = MakeTile("Terra_Miolo", earth.Length > 1 ? earth[1] : earth[0]);
        }
        else Debug.LogWarning("[Fundos] Tile Earth.png não encontrado: os fundos de chão ficarão sem tilemap.");

        var bgs = DarkDir + "Backgrounds/background_";
        Save(Forest(hq));
        Save(Sky("Fundo_Ceu_Noturno_01", SkyDir + "Demo 01 Parallax Pixel Sky/demo01_PixelSky_layer0"));
        Save(Sky("Fundo_Ceu_Noturno_02", SkyDir + "Demo 02 Parallax Pixel Sky/demo02_PixelSky_layer0"));
        Save(Silhouette(bgs));
        Save(GroundArea("Fundo_Floresta_Chao", top, fill, bgs, ForestDecor()));
        Save(GroundArea("Fundo_Acampamento_Chao", top, fill, null, CampDecor(), skyPrefix: SkyDir + "Demo 02 Parallax Pixel Sky/demo02_PixelSky_layer0"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=lime>[Labirinto do Castelo]</color> Fundos criados em {Dir}.");
    }

    // =====================================================================
    // FUNDOS
    // =====================================================================

    /// <summary>Floresta HQ: a instância do prefab de parallax, sem chão.</summary>
    private static GameObject Forest(GameObject hq)
    {
        var root = new GameObject("Fundo_Floresta_Escura_HQ");
        if (hq != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(hq, root.transform);
            inst.name = "Fundo";
        }
        return root;
    }

    /// <summary>Céu noturno: 3 camadas do Pixel Skies (céu fixo, nuvens lentas, nuvens mais perto).</summary>
    private static GameObject Sky(string name, string prefix)
    {
        var root = new GameObject(name);
        var layers = new[]
        {
            (Spr(prefix + "1.png"), 0f, 0f),
            (Spr(prefix + "2.png"), 0.08f, 0.15f),
            (Spr(prefix + "3.png"), 0.2f, 0.3f),
        };
        BuildParallax(root.transform, layers);
        return root;
    }

    /// <summary>Silhuetas da Dark Forest: céu chapado + 4 camadas de montanhas/árvores.</summary>
    private static GameObject Silhouette(string bgs)
    {
        var root = new GameObject("Fundo_Silhueta_Floresta");
        BuildParallax(root.transform, new[]
        {
            (Spr(bgs + "1.png"), 0f, 0f),
            (Spr(bgs + "5.png"), 0.04f, 0f),
            (Spr(bgs + "3.png"), 0.1f, 0f),
            (Spr(bgs + "2.png"), 0.18f, 0f),
            (Spr(bgs + "4.png"), 0.3f, 0f),
        });
        return root;
    }

    /// <summary>Área de chão: fundo parallax + tilemap de terra + decorações.</summary>
    private static GameObject GroundArea(string name, TileBase top, TileBase fill, string darkBgs,
        (string path, float x, float h, bool flip)[] decor, string skyPrefix = null)
    {
        var root = new GameObject(name);

        if (darkBgs != null)
            BuildParallax(root.transform, new[]
            {
                (Spr(darkBgs + "1.png"), 0f, 0f),
                (Spr(darkBgs + "3.png"), 0.1f, 0f),
                (Spr(darkBgs + "4.png"), 0.25f, 0f),
            });
        else if (skyPrefix != null)
            BuildParallax(root.transform, new[]
            {
                (Spr(skyPrefix + "1.png"), 0f, 0f),
                (Spr(skyPrefix + "2.png"), 0.08f, 0.15f),
                (Spr(skyPrefix + "3.png"), 0.2f, 0.3f),
            });

        if (top != null && fill != null) AddGround(root.transform, top, fill);

        var deco = new GameObject("Decoracao").transform;
        deco.SetParent(root.transform, false);
        foreach (var d in decor) Deco(deco, Spr(d.path), d.x, d.h, d.flip);
        return root;
    }

    // =====================================================================
    // DECORAÇÕES (base em y = 0, altura em unidades)
    // =====================================================================

    private static (string, float, float, bool)[] ForestDecor() => new[]
    {
        (DarkDir + "Nature/tree.png", -14f, 6.5f, false),
        (DarkDir + "Nature/tree_2.png", 9f, 5.2f, true),
        (DarkDir + "Nature/tree.png", 24f, 6f, true),
        (DarkDir + "Nature/bush.png", -3f, 1.2f, false),
        (DarkDir + "Nature/bush_2.png", 5f, 1.1f, true),
        (DarkDir + "Nature/bush_3.png", 19f, 1.3f, false),
        (DarkDir + "Nature/grass_1.png", 2f, 0.8f, false),
        (DarkDir + "Nature/grass_3.png", 14f, 0.8f, true),
        (DarkDir + "Nature/mushroom.png", 7f, 0.5f, false),
        (DarkDir + "Nature/mushroom_3.png", 22f, 0.5f, false),
        (DarkDir + "Nature/large_stone.png", 11f, 0.9f, false),
        (DarkDir + "Nature/small_stone.png", -7f, 0.4f, true),
        (DarkDir + "Decorations/post_with_lantern.png", -1f, 2.4f, false),
        (DarkDir + "Decorations/post_with_lantern.png", 16f, 2.4f, false),
        (DarkDir + "Decorations/fence.png", 12f, 1.0f, false),
    };

    private static (string, float, float, bool)[] CampDecor() => new[]
    {
        (DarkDir + "Decorations/tent.png", 0f, 2.6f, false),
        (DarkDir + "Decorations/bonfire.png", 4f, 1.4f, false),
        (DarkDir + "Decorations/box.png", -5f, 0.8f, false),
        (DarkDir + "Decorations/fence.png", -10f, 1.0f, false),
        (DarkDir + "Decorations/fence_2.png", 10f, 1.0f, true),
        (DarkDir + "Decorations/plaque_1.png", 7f, 0.9f, false),
        (DarkDir + "Decorations/post_with_lantern.png", -14f, 2.4f, false),
        (DarkDir + "Decorations/post_with_lantern.png", 16f, 2.4f, false),
        (DarkDir + "Nature/grass_2.png", 2f, 0.8f, false),
        (DarkDir + "Nature/bush_4.png", 21f, 1.2f, false),
    };

    // =====================================================================
    // HELPERS
    // =====================================================================

    /// <summary>Cria o filho "Fundo" com ParallaxBackground e as camadas (parallax 0 = parada, 1 = anda com o mundo).</summary>
    private static void BuildParallax(Transform root, (Sprite s, float p, float scroll)[] layers)
    {
        var go = new GameObject("Fundo");
        go.transform.SetParent(root, false);
        var bg = go.AddComponent<ParallaxBackground>();
        bg.Layers.Clear();
        foreach (var l in layers)
        {
            if (l.s == null) continue;
            bg.Layers.Add(new ParallaxBackground.Layer { name = l.s.name, sprite = l.s, parallax = l.p, autoScrollSpeed = l.scroll });
        }
        bg.Build();
    }

    /// <summary>Tilemap de terra: 3 linhas abaixo de y = 0, com colisão na layer Ground.</summary>
    private static void AddGround(Transform root, TileBase top, TileBase fill)
    {
        var grid = new GameObject("Grid", typeof(Grid)).transform;
        grid.SetParent(root, false);
        var chao = new GameObject("Chao", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        chao.transform.SetParent(grid, false);
        int ground = LayerMask.NameToLayer("Ground");
        chao.layer = ground >= 0 ? ground : 6;
        chao.GetComponent<TilemapRenderer>().sortingOrder = 0;

        var tm = chao.GetComponent<Tilemap>();
        for (int x = -HalfWidth; x <= HalfWidth; x++)
            for (int y = -GroundDepth; y <= -1; y++)
                tm.SetTile(new Vector3Int(x, y, 0), y == -1 ? top : fill);
        tm.CompressBounds();
    }

    private static void Deco(Transform parent, Sprite s, float x, float h, bool flip)
    {
        if (s == null) return;
        var go = new GameObject(s.name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 3;
        float k = h / s.bounds.size.y;
        go.transform.localScale = new Vector3(flip ? -k : k, k, 1f);
        // base do desenho encostada em y = 0, centro horizontal em x
        go.transform.localPosition = new Vector3(x - s.bounds.center.x * k, -s.bounds.min.y * k, 0f);
    }

    private static TileBase MakeTile(string name, Sprite sprite)
    {
        string path = $"{TileDir}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static Sprite Spr(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();

    private static Sprite[] Sprites(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();

    private static void Save(GameObject go)
    {
        PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{go.name}.prefab");
        Object.DestroyImmediate(go);
    }
}
#endif
