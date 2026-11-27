#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Criador passo a passo das Salas do Labirinto da Sofia.
/// Cria o prefab da sala em Assets/Prefabs/Fases/SalasSofia/ e uma cena isolada
/// em Assets/Scenes/ para testar sem conflito com a Fase2_Castelo.
/// </summary>
public static class SalasLabirintoSofiaCreator
{
    public const string PrefabDir = "Assets/Prefabs/Fases/SalasSofia";
    public const string TileDir = PrefabDir + "/Tiles";
    public const string TemplateScenePath = "Assets/Scenes/Fase3_AreaMagica.unity";

    private const string MedievalTiles = "Assets/Medieval_pixel_art_asset_FREE/Textures/Medieval_tiles_free2.png";
    private const string PixelSkyDemo01 = "Assets/Pixel Skies DEMO/Parallax Pixel Skies 240x135px/Demo 01 Parallax Pixel Sky/demo01_PixelSky_layer0";

    private static readonly string[] KeepRoots =
        { "Main Camera", "EventSystem", "Global Light 2D", "GameManager", "GameOver", "HUD", "TelaVitoria" };

    // =====================================================================
    // SALA 1 — SALA GRANDE (Pixel Skies Demo 01 + Chão de Pedra Medieval)
    // =====================================================================

    [MenuItem("Chroma Defender/Labirinto (Sofia)/1. Criar Sala 01 Grande (Pixel Skies 01 + Pedra Medieval)", priority = 700)]
    public static void CreateSala01Grande()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder("Assets/Prefabs/Fases");
        EnsureFolder(PrefabDir);
        EnsureFolder(TileDir);

        TileBase stoneTL = MakeMedievalTile("Pedra_TopoEsq", 0, 8);
        TileBase stoneT  = MakeMedievalTile("Pedra_Topo",    1, 8);
        TileBase stoneTR = MakeMedievalTile("Pedra_TopoDir", 2, 8);
        TileBase stoneL  = MakeMedievalTile("Pedra_Esq",     0, 9);
        TileBase stoneM  = MakeMedievalTile("Pedra_Meio",    1, 9);
        TileBase stoneR  = MakeMedievalTile("Pedra_Dir",     2, 9);

        if (stoneT == null || stoneM == null)
        {
            Debug.LogError("[Labirinto Sofia] Não foi possível carregar os tiles de pedra medieval.");
            return;
        }

        const int length = 140;
        const string roomKey = "Sala_01_Grande";
        const string roomDisplayName = "Sala Grande 01 — Passarela Noturna";

        var root = new GameObject(roomKey);

        // 1. Área da Sala (CastleMazeRoom) para o sistema do labirinto reconhecer a sala
        var areaGo = new GameObject("Area_Sala");
        areaGo.transform.SetParent(root.transform, false);
        areaGo.transform.localPosition = new Vector3(length * 0.5f, 5f, 0f);
        var roomComp = areaGo.AddComponent<CastleMazeRoom>();
        roomComp.Configure(roomDisplayName, new Vector2(length, 16f), start: false, message: "");

        // 2. Background: Pixel Skies Demo 01 (3 camadas de Parallax)
        var fundoGo = new GameObject("Fundo");
        fundoGo.transform.SetParent(root.transform, false);
        ApplyDarkForestBackground(fundoGo);

        // 3. Chão e Plataformas de Pedra Medieval (básico, sem mobs e sem enfeites)
        BuildStoneGround(root.transform, length, stoneTL, stoneT, stoneTR, stoneL, stoneM, stoneR);

        // 4. Paredes invisíveis nas bordas para o jogador não cair para fora da sala
        BuildBorderWall(root.transform, "Parede_Esquerda", -0.5f, 4f, 1f, 20f);
        BuildBorderWall(root.transform, "Parede_Direita", length + 0.5f, 4f, 1f, 20f);

        // 5. Portas Básicas (Entrada, Meio e Saída)
        var portasHolder = new GameObject("Portas").transform;
        portasHolder.SetParent(root.transform, false);
        var dEntrada = PlaceDoor(portasHolder, 3.5f,  0f, "sala01_entrada", "Porta de Entrada");
        var dMeio    = PlaceDoor(portasHolder, 70.0f, 0f, "sala01_meio",    "Porta Central");
        var dSaida   = PlaceDoor(portasHolder, 135.5f, 0f, "sala01_saida",  "Porta de Saída");
        // Liga Entrada <-> Saída provisoriamente para teste até o Kauã conectar às outras salas
        LabirintoCasteloPresets.Link(dEntrada, dSaida);
        LabirintoCasteloPresets.Link(dSaida, dEntrada);

        // 6. Ponto de Entrada (Spawn_Entrada)
        var spawnGo = new GameObject("Spawn_Entrada");
        spawnGo.transform.SetParent(root.transform, false);
        spawnGo.transform.localPosition = new Vector3(7f, 0.2f, 0f);
        spawnGo.AddComponent<SpawnPoint>().Configure("sala_01_grande_entrada", isDefault: true);

        // Salva o Prefab da Sala
        string prefabPath = $"{PrefabDir}/{roomKey}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        // Cria e abre a Cena isolada com a Sala 01 instanciada
        string scenePath = $"Assets/Scenes/{roomKey}.unity";
        CreateIsolatedRoomScene(scenePath, prefabPath, new Vector3(7f, 0.5f, 0f));

        Debug.Log($"<color=lime>[Labirinto Sofia]</color> {roomKey} criada com sucesso!\nPrefab: {prefabPath}\nCena aberta: {scenePath}");
    }

    // =====================================================================
    // FUNDO: Floresta Escura (Dark Forest, 5 camadas de parallax)
    // =====================================================================

    private const string DarkForestBg = "Assets/BSGameStudio/Dark Forest Pixel Pack – 2D Platformer Starter Pack/Art/Backgrounds/background_";

    /// <summary>Coloca (ou recoloca) as camadas da Floresta Escura no objeto "Fundo".</summary>
    private static void ApplyDarkForestBackground(GameObject fundoGo)
    {
        var bg = fundoGo.GetComponent<ParallaxBackground>();
        if (bg == null) bg = fundoGo.AddComponent<ParallaxBackground>();
        bg.Layers.Clear();
        AddLayer(bg, "Fundo_Ceu",       Spr(DarkForestBg + "1.png"), 0.00f, 0.00f);
        AddLayer(bg, "Montanhas",       Spr(DarkForestBg + "5.png"), 0.04f, 0.00f);
        AddLayer(bg, "Floresta_Longe",  Spr(DarkForestBg + "3.png"), 0.10f, 0.00f);
        AddLayer(bg, "Floresta_Meio",   Spr(DarkForestBg + "2.png"), 0.18f, 0.00f);
        AddLayer(bg, "Floresta_Perto",  Spr(DarkForestBg + "4.png"), 0.30f, 0.00f);
        bg.Build();
    }

    [MenuItem("Chroma Defender/Labirinto (Sofia)/Trocar fundo da Sala 01 (Floresta Escura)", priority = 710)]
    public static void SwapSala01Background()
    {
        string path = $"{PrefabDir}/Sala_01_Grande.prefab";
        if (!File.Exists(path))
        {
            Debug.LogWarning("[Labirinto Sofia] Prefab da Sala 01 não existe. Crie a sala primeiro (menu 1).");
            return;
        }
        var contents = PrefabUtility.LoadPrefabContents(path);
        var fundo = contents.transform.Find("Fundo");
        if (fundo == null)
        {
            fundo = new GameObject("Fundo").transform;
            fundo.SetParent(contents.transform, false);
        }
        ApplyDarkForestBackground(fundo.gameObject);
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        AssetDatabase.SaveAssets();
        Debug.Log("<color=lime>[Labirinto Sofia]</color> Fundo da Sala 01 trocado para a Floresta Escura (prefab atualizado).");
    }

    private const string ForestHqPrefab = "Assets/Prefabs/Backgrounds/ParallaxForest.prefab";

    [MenuItem("Chroma Defender/Labirinto (Sofia)/Trocar fundo da Sala 01 (Floresta HQ)", priority = 711)]
    public static void SwapSala01BackgroundForestHq()
    {
        string path = $"{PrefabDir}/Sala_01_Grande.prefab";
        if (!File.Exists(path))
        {
            Debug.LogWarning("[Labirinto Sofia] Prefab da Sala 01 não existe. Crie a sala primeiro (menu 1).");
            return;
        }
        var forest = AssetDatabase.LoadAssetAtPath<GameObject>(ForestHqPrefab);
        if (forest == null)
        {
            Debug.LogError("[Labirinto Sofia] Prefab não encontrado: " + ForestHqPrefab);
            return;
        }

        var contents = PrefabUtility.LoadPrefabContents(path);
        var fundo = contents.transform.Find("Fundo");
        if (fundo == null)
        {
            fundo = new GameObject("Fundo").transform;
            fundo.SetParent(contents.transform, false);
        }

        // Remove o parallax antigo (o componente apagaria os filhos novos ao iniciar) e as camadas antigas
        var oldBg = fundo.GetComponent<ParallaxBackground>();
        if (oldBg != null) Object.DestroyImmediate(oldBg);
        for (int i = fundo.childCount - 1; i >= 0; i--) Object.DestroyImmediate(fundo.GetChild(i).gameObject);

        PrefabUtility.InstantiatePrefab(forest, fundo);

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        AssetDatabase.SaveAssets();
        Debug.Log("<color=lime>[Labirinto Sofia]</color> Fundo da Sala 01 trocado para a Floresta HQ (prefab atualizado).");
    }

    // =====================================================================
    // CONSTRUÇÃO DO TERRENO DE PEDRA MEDIEVAL
    // =====================================================================

    private static void BuildStoneGround(Transform root, int length,
        TileBase tl, TileBase top, TileBase tr, TileBase left, TileBase mid, TileBase right)
    {
        var gridGo = new GameObject("Grid", typeof(Grid));
        gridGo.transform.SetParent(root, false);

        var chaoGo = new GameObject("Chao", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        chaoGo.transform.SetParent(gridGo.transform, false);
        int groundLayer = LayerMask.NameToLayer("Ground");
        chaoGo.layer = groundLayer >= 0 ? groundLayer : 6;
        chaoGo.GetComponent<TilemapRenderer>().sortingOrder = 0;

        // Buracos (vãos no chão principal): sempre longe das portas (x=3.5, x=70, x=135.5) e do spawn (x=7)
        var pits = new HashSet<int>();
        (int start, int width)[] pitRanges =
        {
            (24, 3),
            (48, 3),
            (92, 4),
            (116, 3)
        };
        foreach (var (s, w) in pitRanges)
            for (int x = s; x < s + w; x++) pits.Add(x);

        // Células sólidas do chão e das plataformas
        var cells = new HashSet<Vector2Int>();

        // Chão principal (superfície em y = 0 -> tiles em y = -1..-4)
        for (int x = 0; x < length; x++)
        {
            if (pits.Contains(x)) continue;
            for (int y = -4; y <= -1; y++)
                cells.Add(new Vector2Int(x, y));
        }

        // Plataformas básicas de pedra (sobre os buracos e alguns degraus)
        (int x0, int len, int topY)[] platforms =
        {
            (23, 5, 3),   // ponte sobre o 1º buraco
            (35, 6, 3),   // plataforma intermediária
            (47, 5, 3),   // ponte sobre o 2º buraco
            (60, 5, 2),   // degrau antes da porta central
            (78, 6, 3),   // plataforma após a porta central
            (91, 6, 3),   // ponte sobre o 3º buraco
            (104, 5, 4),  // plataforma alta
            (115, 5, 3),  // ponte sobre o 4º buraco
        };

        foreach (var (x0, len, topY) in platforms)
        {
            for (int x = x0; x < x0 + len; x++)
            {
                cells.Add(new Vector2Int(x, topY - 1));
                cells.Add(new Vector2Int(x, topY - 2));
            }
        }

        // Pinta usando bordas e miolo de pedra medieval
        var tm = chaoGo.GetComponent<Tilemap>();
        foreach (var c in cells)
        {
            bool up = cells.Contains(c + Vector2Int.up);
            bool l  = cells.Contains(c + Vector2Int.left);
            bool r  = cells.Contains(c + Vector2Int.right);

            TileBase tile;
            if (!up)
                tile = (!l && r && tl != null) ? tl : (l && !r && tr != null) ? tr : top;
            else
                tile = (!l && r && left != null) ? left : (l && !r && right != null) ? right : mid;

            tm.SetTile(new Vector3Int(c.x, c.y, 0), tile);
        }
        tm.CompressBounds();
    }

    private static void BuildBorderWall(Transform root, string name, float x, float y, float w, float h)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(root, false);
        wall.transform.localPosition = new Vector3(x, y, 0f);
        int groundLayer = LayerMask.NameToLayer("Ground");
        wall.layer = groundLayer >= 0 ? groundLayer : 6;
        var bc = wall.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(w, h);
    }

    private static CastleDoor PlaceDoor(Transform parent, float x, float y, string id, string title)
    {
        var prefab = LabirintoCasteloPresets.Load(LabirintoCasteloPresets.PortaComum);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = new Vector3(x, y, 0f);
        go.name = $"Porta_{id}";
        var door = go.GetComponent<CastleDoor>();
        var so = new SerializedObject(door);
        LabirintoCasteloPresets.Set(so, "doorId", id);
        LabirintoCasteloPresets.Set(so, "doorTitle", title);
        so.ApplyModifiedPropertiesWithoutUndo();
        return door;
    }

    // =====================================================================
    // CRIAÇÃO DA CENA ISOLADA PARA TESTE E EDIÇÃO
    // =====================================================================

    private static void CreateIsolatedRoomScene(string scenePath, string roomPrefabPath, Vector3 playerStartPos)
    {
        string sourceScene = File.Exists(TemplateScenePath) ? TemplateScenePath : "Assets/Scenes/SampleScene.unity";
        if (File.Exists(scenePath))
        {
            AssetDatabase.DeleteAsset(scenePath);
        }
        AssetDatabase.CopyAsset(sourceScene, scenePath);

        var scene = EditorSceneManager.OpenScene(scenePath);
        foreach (var go in scene.GetRootGameObjects())
        {
            if (!KeepRoots.Contains(go.name))
            {
                Object.DestroyImmediate(go);
            }
        }

        // Instancia o prefab da sala na raiz da cena
        var roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(roomPrefabPath);
        GameObject roomInst = null;
        if (roomPrefab != null)
        {
            roomInst = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
            roomInst.transform.position = Vector3.zero;
        }

        // Garante o gerenciador do labirinto na cena para portas e HUD de sala funcionarem
        if (Object.FindAnyObjectByType<CastleMaze>() == null)
        {
            var mgrPrefab = LabirintoCasteloPresets.Load(LabirintoCasteloPresets.Gerenciador);
            if (mgrPrefab != null) PrefabUtility.InstantiatePrefab(mgrPrefab);
        }

        // Posiciona o Player e a Main Camera no início da sala
        var player = Object.FindAnyObjectByType<PlayerBehavior>();
        if (player != null)
        {
            player.transform.position = playerStartPos;
        }
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(playerStartPos.x, playerStartPos.y + 1.2f, -10f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (roomInst != null) Selection.activeGameObject = roomInst;
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private static void AddLayer(ParallaxBackground bg, string name, Sprite sprite, float parallax, float scroll)
    {
        if (sprite == null) return;
        bg.Layers.Add(new ParallaxBackground.Layer
        {
            name = name,
            sprite = sprite,
            parallax = parallax,
            autoScrollSpeed = scroll
        });
    }

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

    private static Sprite Spr(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
